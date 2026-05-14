using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using System.Collections;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单位控制器 - 处理单位选中、移动、攻击的玩家交互
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        public static UnitController instance;

        /// <summary>
        /// 当前是否处于“单位操作中”（正在播放移动动画）。
        /// 用于屏蔽城市购买面板等 UI 的误触发。
        /// </summary>
        public static bool IsUnitCommandActive => instance != null && instance.isAnimating;

        [Header("高亮显示")]
        [SerializeField] private GameObject moveRangeHighlightPrefab;
        [SerializeField] private GameObject attackRangeHighlightPrefab;
        [SerializeField] private GameObject actionableHighlightPrefab;
        [SerializeField] private Color moveHighlightColor = new(0, 1, 0, 0.4f);
        [SerializeField] private Color attackHighlightColor = new(1, 0, 0, 0.4f);
        [SerializeField] private Color actionableHighlightColor = new(0, 0, 0, 0.4f);

        [Header("单位详情面板")]
        [SerializeField] private UnitDetailsPanelController unitDetailsPanel;

        private UnitData selectedUnit;
        private UnitData currentUnit;
        private readonly List<GameObject> moveHighlightObjects = new();
        private readonly List<GameObject> attackHighlightObjects = new();
        private readonly Dictionary<Vector3Int, GameObject> actionableHighlightObjects = new();

        private readonly Dictionary<UnitData, GameObject> unitVisuals = new();
        private readonly Dictionary<UnitData, Sprite> _originalMainSprites = new();
        private HashSet<Vector3Int> reachable = new();
        private HashSet<Vector3Int> attackable = new();

        private bool isAnimating = false;
        private Camera mainCamera;
        private Action<NationData> _onNationTurnEndHandler;
        private Action<UnitData> _onUnitConstructionCompletedHandler;
        private Action<UnitData> _onUnitConstructionUpdatedHandler;
        private Action<NationData> _onNationTurnPreparedHandler;

        private void Awake()
        {
            mainCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected += OnTileSelected;
            }

            StartCoroutine(BindNationManagerWhenReady());
        }

        private IEnumerator BindNationManagerWhenReady()
        {
            while (NationManager.instance == null)
            {
                yield return null;
            }
            while (UnitManager.instance == null)
            {
                yield return null;
            }

            _onNationTurnEndHandler ??= (_) => ClearSelection();
            NationManager.instance.OnNationTurnEnd += _onNationTurnEndHandler;
            _onNationTurnPreparedHandler ??= ResetActionableHighlightObjects;
            UnitManager.instance.OnNationTurnPrepared += _onNationTurnPreparedHandler;

            UnitManager.instance.OnUnitSpawned += OnUnitSpawned;
            UnitManager.instance.OnUnitDestroyed += OnUnitDestroyed;
            UnitManager.instance.OnUnitAttacked += OnUnitAttack;
            UnitManager.instance.OnUnitBarged += ChangeUnitVisual;
            UnitManager.instance.OnUnitLanded += ChangeUnitVisual;

            _onUnitConstructionCompletedHandler ??= OnUnitConstructionCompleted;
            UnitManager.instance.OnUnitConstructionCompleted += _onUnitConstructionCompletedHandler;

            _onUnitConstructionUpdatedHandler ??= OnUnitConstructionUpdated;
            UnitManager.instance.OnUnitConstructionUpdated += _onUnitConstructionUpdatedHandler;
        }

        private void OnDisable()
        {
            if (UnitManager.instance != null)
            {
                UnitManager.instance.OnUnitSpawned -= OnUnitSpawned;
                UnitManager.instance.OnUnitDestroyed -= OnUnitDestroyed;
                UnitManager.instance.OnUnitAttacked -= OnUnitAttack;
                UnitManager.instance.OnUnitBarged -= ChangeUnitVisual;
                UnitManager.instance.OnUnitLanded -= ChangeUnitVisual;
                if (_onUnitConstructionCompletedHandler != null)
                {
                    UnitManager.instance.OnUnitConstructionCompleted -= _onUnitConstructionCompletedHandler;
                }
                if (_onUnitConstructionUpdatedHandler != null)
                {
                    UnitManager.instance.OnUnitConstructionUpdated -= _onUnitConstructionUpdatedHandler;
                }
                if (_onNationTurnPreparedHandler != null)
                {
                    UnitManager.instance.OnNationTurnPrepared -= _onNationTurnPreparedHandler;
                }
            }

            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnTileSelected;
            }

            if (NationManager.instance != null)
            {
                if (_onNationTurnEndHandler != null)
                {
                    NationManager.instance.OnNationTurnEnd -= _onNationTurnEndHandler;
                }
            }
        }

        private void Update()
        {
            HandleRightClickForDetails();
        }

        /// <summary>
        /// 右键点击获取单位详情
        /// </summary>
        private void HandleRightClickForDetails()
        {
            if (isAnimating) return;
            if (NationManager.instance == null || !NationManager.instance.IsLocalHumanTurn()) return;
            if (!Input.GetMouseButtonDown(1)) return;

            // 点击到 UI 上不触发
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (MapManager.instance.Tilemap == null || mainCamera == null)
                return;

            Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            Vector3Int cell = MapManager.instance.Tilemap.WorldToCell(world);

            UnitData unitUnderCursor = UnitManager.instance != null ? UnitManager.instance.GetUnitAtPosition(cell) : null;
            if (unitUnderCursor == null) return;

            if (unitDetailsPanel != null && unitUnderCursor == currentUnit)
            {
                if (unitVisuals[unitUnderCursor].TryGetComponent<BargeUnitMapping>(out var bargeUnit))
                {
                    unitDetailsPanel.Show(unitUnderCursor, bargeUnit.landUnitType);
                }
                else
                {
                    unitDetailsPanel.Show(unitUnderCursor);
                }
            }
        }

        /// <summary>
        /// 单位被创建时的回调
        /// </summary>
        private void OnUnitSpawned(UnitData unit, GameObject gameObject)
        {
            BindUnitVisual(unit, gameObject);
        }

        private void OnUnitConstructionCompleted(UnitData unit)
        {
            if (unit == null) return;
            if (unitVisuals.TryGetValue(unit, out var go) && go != null)
            {
                ApplyConstructionVisual(unit, go);
            }
        }

        private void OnUnitConstructionUpdated(UnitData unit)
        {
            if (unit == null) return;
            if (unitVisuals.TryGetValue(unit, out var go) && go != null)
            {
                ApplyConstructionVisual(unit, go);
            }
        }

        /// <summary>
        /// 单位被销毁时的回调
        /// </summary>
        private void OnUnitDestroyed(UnitData unit)
        {
            if (unitVisuals.TryGetValue(unit, out var go) && go != null)
            {
                go.transform.DOKill();
                Destroy(go);
                unitVisuals.Remove(unit);
            }
            _originalMainSprites.Remove(unit);
            if (selectedUnit == unit)
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// 单位攻击时的回调
        /// </summary>
        private void OnUnitAttack(UnitData attacker, UnitData defender)
        {
            if (unitVisuals.TryGetValue(attacker, out GameObject attackerGo) && attackerGo != null)
            {
                attackerGo.GetComponent<UnitView>().RefreshHealthBar();
            }
            if (unitVisuals.TryGetValue(defender, out GameObject defenderGo) && defenderGo != null)
            {
                defenderGo.GetComponent<UnitView>().RefreshHealthBar();
            }
        }

        /// <summary>
        /// 绑定单位视觉
        /// </summary>
        private void BindUnitVisual(UnitData unit, GameObject gameObject)
        {
            if (unit == null || gameObject == null) return;

            if (gameObject.TryGetComponent<UnitView>(out var unitView))
                unitView.Setup(unit);
            unitVisuals[unit] = gameObject;

            CacheOriginalMainSprite(unit, gameObject);
            ApplyConstructionVisual(unit, gameObject);
        }

        /// <summary>
        /// 地块被选中时的回调
        /// </summary>
        private void OnTileSelected(Vector3Int coordinate)
        {
            if (isAnimating) return;

            if (NationManager.instance == null || NationManager.instance.CurrentNation == null || UnitManager.instance == null) return;
            if (!NationManager.instance.IsLocalHumanTurn()) return;

            UnitData unitAtTile = UnitManager.instance.GetUnitAtPosition(coordinate);

            // 移动/攻击仍仅对己方（CurrentNation）且未行动单位生效
            if (unitAtTile != null)
            {
                currentUnit = unitAtTile;
                bool isOwnUnit = unitAtTile.ownerNationId == NationManager.instance.CurrentNation.nationId;

                if (isOwnUnit)
                {
                    SelectUnit(unitAtTile, unitAtTile.hasAttackedThisTurn, unitAtTile.hasMovedThisTurn);
                    return;
                }
            }

            // 若已有选中单位，尝试移动或攻击
            if (selectedUnit != null)
            {
                if (reachable.Contains(coordinate) && !selectedUnit.hasMovedThisTurn)
                {
                    UnitData targetUnit = UnitManager.instance.GetUnitAtPosition(coordinate);
                    if (targetUnit == null)
                    {
                        StartCoroutine(CoAnimateMoveAlongPath(selectedUnit, coordinate));
                        return;
                    }
                }

                if (attackable.Contains(coordinate) && !selectedUnit.hasAttackedThisTurn)
                {
                    ClearActionableSelection(selectedUnit.position);
                    UnitManager.instance.TryAttack(selectedUnit, coordinate);
                    ClearSelection();
                    return;
                }
            }

            // 点击空白处，取消选中
            ClearSelection();
        }

        /// <summary>
        /// 选中单位
        /// </summary>
        private void SelectUnit(UnitData unit, bool hasAttacked, bool hasMoved)
        {
            ClearSelection();
            selectedUnit = unit;

            if (hasAttacked)
                return;

            attackable = UnitManager.instance.GetAttackablePositions(unit);
            ShowRangeHighlights(attackable, attackHighlightObjects, attackRangeHighlightPrefab, attackHighlightColor);

            if (!hasMoved)
            {
                reachable = UnitManager.instance.GetReachablePositions(unit);
                ShowRangeHighlights(reachable, moveHighlightObjects, moveRangeHighlightPrefab, moveHighlightColor);
            }
        }

        /// <summary>
        /// 清除选中
        /// </summary>
        private void ClearSelection()
        {
            selectedUnit = null;
            attackable.Clear();
            reachable.Clear();
            ClearHighlightObjects(moveHighlightObjects);
            ClearHighlightObjects(attackHighlightObjects);

            if (unitDetailsPanel != null)
                unitDetailsPanel.Hide();
        }

        /// <summary>
        /// AI 与脚本调用的移动：与玩家相同的 DOTween 逐格动画，结束后不刷新玩家选中/范围高亮。
        /// </summary>
        public Coroutine StartAnimatedMoveForAi(UnitData unit, Vector3Int targetCell)
        {
            return StartCoroutine(CoAnimateMoveAlongPath(unit, targetCell));
        }

        /// <summary>
        /// 沿六边形路径播放单位移动动画，并在结束时更新逻辑位置（与玩家点击移动共用）。
        /// </summary>
        private IEnumerator CoAnimateMoveAlongPath(UnitData unit, Vector3Int targetCell)
        {
            if (unit == null || UnitManager.instance == null)
            {
                yield break;
            }

            List<Vector3Int> path = UnitManager.instance.FindPath(unit, targetCell);
            if (path == null || path.Count == 0)
            {
                yield break;
            }

            if (!unitVisuals.TryGetValue(unit, out GameObject go) || go == null)
            {
                yield break;
            }

            ClearActionableSelection(unit.position);

            ClearSelection();

            isAnimating = true;

            float stepDuration = 0.15f;

            for (int i = 1; i < path.Count; i++)
            {
                if (!unitVisuals.TryGetValue(unit, out GameObject stepGo) || stepGo == null)
                {
                    isAnimating = false;
                    yield break;
                }

                Vector3 nextWorld = MapManager.instance.Tilemap.GetCellCenterWorld(path[i]);
                Tween t = stepGo.transform.DOMove(nextWorld, stepDuration).SetEase(Ease.Linear).SetLink(stepGo);
                yield return t.WaitForCompletion();
            }

            UnitManager.instance.TryMoveUnit(unit, targetCell);

            isAnimating = false;

            SelectUnit(unit, unit.hasAttackedThisTurn, unit.hasMovedThisTurn);

            if (attackable.Count > 0 && selectedUnit != null && !selectedUnit.hasAttackedThisTurn)
            {
                ShowActionableSelection(targetCell);
            }
        }

        /// <summary>
        /// 显示范围高亮
        /// </summary>
        private void ShowRangeHighlights(HashSet<Vector3Int> positions, List<GameObject> objectList, GameObject prefab, Color color)
        {
            ClearHighlightObjects(objectList);
            if (prefab == null || MapManager.instance == null) return;

            foreach (var pos in positions)
            {
                Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(pos);
                var go = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);
                if (go != null)
                {
                    var sr = go.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = color;
                    objectList.Add(go);
                }
            }
        }

        /// <summary>
        /// 清除范围高亮
        /// </summary>
        private void ClearHighlightObjects(List<GameObject> list)
        {
            foreach (var go in list)
            {
                if (go != null) Destroy(go);
            }
            list.Clear();
        }

        /// <summary>
        /// 显示攻击高亮（供外部调用）
        /// </summary>
        public void ShowAttackRangeHighlights(HashSet<Vector3Int> positions)
        {
            ClearHighlightObjects(attackHighlightObjects);
            if (attackRangeHighlightPrefab == null || MapManager.instance == null) return;

            foreach (var pos in positions)
            {
                Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(pos);
                var go = Instantiate(attackRangeHighlightPrefab, worldPos, Quaternion.identity, this.transform);
                if (go != null)
                {
                    var sr = go.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = attackHighlightColor;
                    attackHighlightObjects.Add(go);
                }
            }
        }

        /// <summary>
        /// 显示移动高亮（供外部调用）
        /// </summary>
        public void ShowMoveRangeHighlights(HashSet<Vector3Int> positions)
        {
            ClearHighlightObjects(moveHighlightObjects);
            if (moveRangeHighlightPrefab == null || MapManager.instance == null) return;

            foreach (var pos in positions)
            {
                Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(pos);
                var go = Instantiate(moveRangeHighlightPrefab, worldPos, Quaternion.identity, this.transform);
                if (go != null)
                {
                    var sr = go.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = moveHighlightColor;
                    moveHighlightObjects.Add(go);
                }
            }
        }

        /// <summary>
        /// 清除所有攻击、移动范围高亮（供外部调用）
        /// </summary>
        public void ClearHighlightObjects()
        {
            ClearHighlightObjects(moveHighlightObjects);
            ClearHighlightObjects(attackHighlightObjects);
        }

        /// <summary>
        /// 重置可行动高亮显示
        /// </summary>
        /// <param name="nationData"></param>
        private void ResetActionableHighlightObjects(NationData nationData)
        {
            List<GameObject> Objects = actionableHighlightObjects.Values.ToList();
            ClearHighlightObjects(Objects);
            actionableHighlightObjects.Clear();

            if (nationData == null || UnitManager.instance == null || actionableHighlightPrefab == null) return;

            foreach (var unit in UnitManager.instance.GetUnitsByNation(nationData.nationId))
            {
                if (unit.unitType.unitProperty != UnitProperty.Fort || UnitManager.instance.GetAttackablePositions(unit).Count != 0)
                {
                    Vector3Int pos = unit.position;
                    Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(pos);
                    var go = Instantiate(actionableHighlightPrefab, worldPos, Quaternion.identity, this.transform);
                    if (go != null)
                    {
                        var sr = go.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null) sr.color = actionableHighlightColor;
                        actionableHighlightObjects.Add(pos, go);
                    }
                }
            }
        }

        /// <summary>
        /// 指定位置生成可行动高亮显示
        /// </summary>
        /// <param name="position"></param>
        private void ShowActionableSelection(Vector3Int position)
        {
            Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(position);
            var go = Instantiate(actionableHighlightPrefab, worldPos, Quaternion.identity, this.transform);
            if (go != null)
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = actionableHighlightColor;
                actionableHighlightObjects.Add(position, go);
            }
        }

        /// <summary>
        /// 指定位置删除可行动高亮显示
        /// </summary>
        /// <param name="vector3Int"></param>
        private void ClearActionableSelection(Vector3Int position)
        {
            if (actionableHighlightObjects.TryGetValue(position, out GameObject Highlight))
            {
                Destroy(Highlight);
            }
            actionableHighlightObjects.Remove(position);
        }

        /// <summary>
        /// 移除某一格上的可行动高亮（供 AI 等在攻击结束后与玩家表现一致）。
        /// </summary>
        public void ClearActionableHighlightAt(Vector3Int cell)
        {
            ClearActionableSelection(cell);
        }

        /// <summary>
        /// AI 攻击结束后清除移动动画末尾 <see cref="SelectUnit"/> 留下的移动/攻击范围圈及可行动高亮。
        /// </summary>
        public void ClearHighlightsAfterAiUnitAttack(UnitData attacker)
        {
            ClearSelection();
            if (attacker != null)
                ClearActionableHighlightAt(attacker.position);
        }

        /// <summary>
        /// 根据单位数据获取单位模型
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public GameObject GetUnitGameObject(UnitData unit)
        {
            if (unitVisuals.TryGetValue(unit, out GameObject unitGo))
            {
                return unitGo;
            }
            return null;
        }

        /// <summary>
        ///  改变单位视觉模型（返回旧模型）
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="gameObject"></param>
        public void ChangeUnitVisual(UnitData unit, GameObject newGo)
        {
            if (unit == null || newGo == null) return;

            if (newGo.TryGetComponent<UnitView>(out var unitView))
                unitView.Setup(unit);
            unitVisuals.Remove(unit, out GameObject oldGo);
            if (oldGo != null)
                oldGo.transform.DOKill();
            Destroy(oldGo);

            unitVisuals.Add(unit, newGo);
            CacheOriginalMainSprite(unit, newGo);
            ApplyConstructionVisual(unit, newGo);
            return;
        }

        /// <summary>
        /// 缓存单位原始主精灵
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitGo"></param>
        private void CacheOriginalMainSprite(UnitData unit, GameObject unitGo)
        {
            if (unit == null || unitGo == null) return;
            var sr = GetMainSpriteRenderer(unitGo);
            if (sr == null) return;
            _originalMainSprites[unit] = sr.sprite;
        }

        /// <summary>
        /// 获取单位主精灵渲染器
        /// </summary>
        /// <param name="unitGo"></param>
        /// <returns></returns>
        private static SpriteRenderer GetMainSpriteRenderer(GameObject unitGo)
        {
            if (unitGo == null) return null;
            // 优先根节点，其次找子节点里第一个（用于兼容不同预制体结构）
            if (unitGo.TryGetComponent<SpriteRenderer>(out var root)) return root;
            return unitGo.GetComponentInChildren<SpriteRenderer>(true);
        }

        /// <summary>
        /// 应用建造视觉
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="unitGo"></param>
        private void ApplyConstructionVisual(UnitData unit, GameObject unitGo)
        {
            if (unit == null || unitGo == null) return;

            // 低成本做法：建造中灰化显示（含子节点）
            Color target = unit.isUnderConstruction ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
            var renderers = unitGo.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                sr.color = target;
            }

            // Fort 建造阶段图标切换（Turn3/2/1）
            var mainSr = GetMainSpriteRenderer(unitGo);
            if (mainSr == null) return;

            if (unit.isUnderConstruction && unit.unitType != null && unit.unitType.unitProperty == UnitProperty.Fort)
            {
                Sprite stageSprite = unit.constructionTurnsRemaining switch
                {
                    >= 3 => unit.unitType.constructionIconTurn3,
                    2 => unit.unitType.constructionIconTurn2,
                    1 => unit.unitType.constructionIconTurn1,
                    _ => null
                };

                if (stageSprite != null)
                {
                    mainSr.sprite = stageSprite;
                }
            }
            else
            {
                if (_originalMainSprites.TryGetValue(unit, out var original) && original != null)
                {
                    mainSr.sprite = original;
                }
            }
        }
    }
}
