using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using System.Collections;
using DG.Tweening;

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
        [SerializeField] private Color moveHighlightColor = new Color(0, 1, 0, 0.4f);
        [SerializeField] private Color attackHighlightColor = new Color(1, 0, 0, 0.4f);

        private UnitData selectedUnit;
        private List<GameObject> moveHighlightObjects = new List<GameObject>();
        private List<GameObject> attackHighlightObjects = new List<GameObject>();
        private Dictionary<UnitData, GameObject> unitVisuals = new Dictionary<UnitData, GameObject>();
        private HashSet<Vector3Int> reachable = new HashSet<Vector3Int>();
        private HashSet<Vector3Int> attackable = new HashSet<Vector3Int>();

        private bool isAnimating = false;

        private void Awake()
        {
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

            NationManager.instance.OnNationTurnEnd += (nationData) => ClearSelection();

            UnitManager.instance.OnUnitSpawned += OnUnitSpawned;
            UnitManager.instance.OnUnitDestroyed += OnUnitDestroyed;
            UnitManager.instance.OnUnitAttacked += OnUnitAttack;
        }

        private void OnDisable()
        {
            if (UnitManager.instance != null)
            {
                UnitManager.instance.OnUnitSpawned -= OnUnitSpawned;
                UnitManager.instance.OnUnitDestroyed -= OnUnitDestroyed;
                UnitManager.instance.OnUnitAttacked -= OnUnitAttack;
            }

            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnTileSelected;
            }

            if (NationManager.instance != null)
            {
                NationManager.instance.OnNationTurnEnd -= (nationData) => ClearSelection();
            }
        }

        private void Start()
        {
        }

        /// <summary>
        /// 单位被创建时的回调
        /// </summary>
        private void OnUnitSpawned(UnitData unit, GameObject gameObject)
        {
            BindUnitVisual(unit, gameObject);
        }

        /// <summary>
        /// 单位被销毁时的回调
        /// </summary>
        private void OnUnitDestroyed(UnitData unit)
        {
            if (unitVisuals.TryGetValue(unit, out var go) && go != null)
            {
                Destroy(go);
                unitVisuals.Remove(unit);
            }
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

            var unitView = gameObject.GetComponent<UnitView>();
            if (unitView != null)
                unitView.Setup(unit);
            unitVisuals[unit] = gameObject;
        }
        
        /// <summary>
        /// 地块被选中时的回调
        /// </summary>
        private void OnTileSelected(Vector3Int coordinate)
        {
            if (isAnimating) return;

            if (NationManager.instance?.CurrentNation == null) return;

            UnitData unitAtTile = UnitManager.instance?.GetUnitAtPosition(coordinate);

            // 若点击的是己方未行动单位，选中它
            if (unitAtTile != null &&
                unitAtTile.ownerNationId == NationManager.instance.CurrentNation.nationId)
            {
                SelectUnit(unitAtTile, unitAtTile.hasAttackedThisTurn, unitAtTile.hasMovedThisTurn);
                return;
            }

            // 若已有选中单位，尝试移动或攻击
            if (selectedUnit != null)
            {
                if (reachable.Contains(coordinate) && !selectedUnit.hasMovedThisTurn)
                {
                    UnitData targetUnit = UnitManager.instance.GetUnitAtPosition(coordinate);
                    if (targetUnit == null)
                    {
                        StartCoroutine(AnimateMoveAlongPath(selectedUnit, coordinate));
                        return;
                    }
                }

                if (attackable.Contains(coordinate) && !selectedUnit.hasAttackedThisTurn)
                {
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
        }

        /// <summary>
        /// 沿六边形路径播放单位移动动画，并在结束时更新逻辑位置
        /// </summary>
        private IEnumerator AnimateMoveAlongPath(UnitData unit, Vector3Int targetCell)
        {
            if (unit == null || UnitManager.instance == null)
            {
                yield break;
            }

            // 寻路
            List<Vector3Int> path = UnitManager.instance.FindPath(unit, targetCell);
            if (path == null || path.Count == 0)
            {
                yield break;
            }

            if (!unitVisuals.TryGetValue(unit, out GameObject go) || go == null)
            {
                yield break;
            }

            ClearSelection();
            isAnimating = true;

            // 简单匀速：每格固定时间
            float stepDuration = 0.15f;

            // 起点已经在第一格，逐格移动到后续格子
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 nextWorld = MapManager.instance.Tilemap.GetCellCenterWorld(path[i]);
                Tween t = go.transform.DOMove(nextWorld, stepDuration).SetEase(Ease.Linear);
                yield return t.WaitForCompletion();
            }

            // 动画完成后，更新逻辑位置并走一次正式移动逻辑（占城、标记已移动等）
            UnitManager.instance.TryMoveUnit(unit, targetCell);

            isAnimating = false;
            SelectUnit(unit, unit.hasAttackedThisTurn, unit.hasMovedThisTurn);
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
                var go = Instantiate(prefab, worldPos, Quaternion.identity);
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
    }
}
