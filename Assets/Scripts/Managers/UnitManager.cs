using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using GlobalConqueror.Controllers;
using DG.Tweening;
using UnityEngine.UIElements;
using JetBrains.Annotations;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 军队管理器 - 管理单位生成、移动、攻击、占领城市
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager instance;

        [Header("兵种预制体列表")]
        [SerializeField] private List<GameObject> availableSoldier = new();
        [SerializeField] private List<GameObject> availableArmor = new();
        [SerializeField] private List<GameObject> availableArtillery = new();
        [SerializeField] private List<GameObject> availableShip = new();
        [SerializeField] private List<GameObject> availableFort = new();

        [Header("驳船预制体")]
        [SerializeField] private GameObject barge;

        [Header("初始地图上兵的父容器")]
        [Tooltip("其子物体需挂 InitialUnitSpawn，世界坐标会转为格子坐标作为出生点")]
        [SerializeField] private GameObject unitsContainer;

        private readonly List<UnitData> allUnits = new();
        private int nextUnitId = 1;

        [HideInInspector]
        public bool initialUnitsSpawned = false;

        public List<UnitData> AllUnits => allUnits;
        public List<GameObject> AvailableSoldier => availableSoldier;
        public List<GameObject> AvailableArmor => availableArmor;
        public List<GameObject> AvailableArtillery => availableArtillery;
        public List<GameObject> AvailableShip => availableShip;
        public List<GameObject> AvailableFort => availableFort;


        public System.Action<UnitData, GameObject> OnUnitSpawned;
        public System.Action<UnitData, UnitData> OnUnitAttacked;
        public System.Action<UnitData, CityData> OnCityCaptured;
        public System.Action<UnitData, PortData> OnPortCaptured;
        public System.Action<UnitData> OnUnitDestroyed;
        public System.Action<UnitData, GameObject> OnUnitBarged;
        public System.Action<UnitData, GameObject> OnUnitLanded;
        public System.Action<UnitData> OnUnitConstructionCompleted;
        public System.Action<UnitData> OnUnitConstructionUpdated;
        public System.Action<NationData> OnNationTurnPrepared;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeUnitsWhenMapReady());
        }

        /// <summary>
        /// 地图与城市均初始化完成后，从 UnitsContainer 子物体初始化开局单位
        /// </summary>
        private IEnumerator InitializeUnitsWhenMapReady()
        {
            while (MapManager.instance == null || !MapManager.instance.InitializeMapCompleted)
            {
                yield return null;
            }
            while (CityManager.instance == null || !CityManager.instance.IsCityTilemapInitialized)
            {
                yield return null;
            }
            while (NationManager.instance == null || !NationManager.instance.IsNationsInitialized)
            {
                yield return null;
            }

            NationManager.instance.OnNationTurnStart += OnNationTurnStart;
            NationManager.instance.OnNationDefeated += OnNationDefeated;

            if (!initialUnitsSpawned)
            {
                InitializeUnitsFromContainer();
                initialUnitsSpawned = true;
            }
        }

        private void OnDisable()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnNationTurnStart -= OnNationTurnStart;
                NationManager.instance.OnNationDefeated -= OnNationDefeated;
            }
        }

        /// <summary>
        /// 从父容器读取子物体，在对应格子生成单位。
        /// ownerNationId 为 -1 时自动识别：优先该格城市所属国家，否则取最近城市所属国家。
        /// </summary>
        private void InitializeUnitsFromContainer()
        {
            if (unitsContainer == null || MapManager.instance == null || MapManager.instance.Tilemap == null)
            {
                return;
            }

            Tilemap tilemap = MapManager.instance.Tilemap;
            var spawns = unitsContainer.GetComponentsInChildren<InitialUnitSpawn>(true);

            foreach (var spawn in spawns)
            {
                if (spawn.unitType == null)
                {
                    Debug.LogWarning($"UnitManager: InitialUnitSpawn 未指定 unitType，物体 {spawn.gameObject.name} 已跳过");
                    continue;
                }

                Vector3 worldPos = spawn.transform.position;
                Vector3Int cell = tilemap.WorldToCell(worldPos);

                if (!MapManager.instance.IsCoordinateValid(cell))
                {
                    Debug.LogWarning($"UnitManager: 开局单位 {spawn.gameObject.name} 位置 {cell} 不在有效地图内，已跳过");
                    continue;
                }

                if (GetUnitAtPosition(cell) != null)
                {
                    Debug.LogWarning($"UnitManager: 格子 {cell} 已有单位，跳过 {spawn.gameObject.name}");
                    continue;
                }

                int ownerId = ResolveOwnerNationId(cell, spawn.ownerNationName);
                if (ownerId < 0)
                {
                    Debug.LogWarning($"UnitManager: 无法为 {spawn.gameObject.name} 判定所属国家（位置 {cell}，请设置 ownerNationId 或确保附近有城市），已跳过");
                    continue;
                }

                SpawnUnit(cell, spawn.unitType, ownerId, spawn.gameObject, false);

                Debug.Log($"UnitManager: 初始化开局单位 {spawn.unitType.unitTypeName} 于 {cell}，国家 {ownerId}");
            }
        }

        /// <summary>
        /// 解析单位所属国家：若 ownerNationName 属实直接返回；为空字符串时根据所在地块的所属国家（MapTileData.ownerId）自动识别。
        /// </summary>
        /// <returns>国家 ID，无法识别时返回 -1</returns>
        private int ResolveOwnerNationId(Vector3Int cell, string ownerNationName)
        {
            if (ownerNationName == "")
            {
                MapTileData tile = MapManager.instance.GetTileData(cell);
                if (tile != null && tile.ownerId >= 0)
                    return tile.ownerId;
            }
            if (NationManager.instance != null && NationManager.instance.NationsDic.ContainsKey(ownerNationName))
                return NationManager.instance.NationsDic[ownerNationName].nationId;

            return -1;
        }

        private void OnNationTurnStart(NationData nation)
        {
            ProgressConstructionForNation(nation.nationId);
            ApplyCitySupplyHealForNation(nation);
            ResetUnitActionsForNation(nation.nationId);
            OnNationTurnPrepared?.Invoke(nation);
        }

        /// <summary>
        /// 该国回合开始时：该国拥有的、补给等级大于 0 的城市，为其所占格子上己方单位回复血量。
        /// </summary>
        private void ApplyCitySupplyHealForNation(NationData nation)
        {
            if (nation == null || CityManager.instance == null || MapManager.instance == null) return;

            foreach (var city in nation.ownedCities)
            {
                if (city == null) continue;
                if (CityManager.instance.CitiesDic.TryGetValue(city.name, out CityData cityData))
                {
                    UnitData unit = GetUnitAtPosition(cityData.cityLocation);
                    int heal = cityData.CitySupplyHealPerTurn;
                    if (unit == null || unit.ownerNationId != nation.nationId) continue;
                    if (unit.isUnderConstruction) continue;
                    if (unit.currentHealth >= unit.maxHealth || heal <= 0) continue;

                    unit.currentHealth = Mathf.Min(unit.maxHealth, unit.currentHealth + heal);

                    if (UnitController.instance != null)
                    {
                        GameObject go = UnitController.instance.GetUnitGameObject(unit);
                        if (go != null && go.TryGetComponent<UnitView>(out var view))
                            view.RefreshHealthBar();
                    }
                }
            }
        }

        private void OnNationDefeated(NationData nation)
        {
            RemoveAllUnitsOfNation(nation.nationId);
        }

        /// <summary>
        /// 重置指定国家所有单位的本回合行动状态
        /// </summary>
        private void ResetUnitActionsForNation(int nationId)
        {
            foreach (var unit in allUnits)
            {
                if (unit.ownerNationId == nationId)
                {
                    if (unit.isUnderConstruction)
                    {
                        unit.hasAttackedThisTurn = true;
                        unit.hasMovedThisTurn = true;
                        continue;
                    }

                    unit.hasAttackedThisTurn = false;
                    unit.hasMovedThisTurn = false;
                }
            }
        }

        /// <summary>
        /// 推进指定国家所有单位的建造进度
        /// </summary>
        /// <param name="nationId"></param>
        private void ProgressConstructionForNation(int nationId)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                UnitData unit = allUnits[i];
                if (unit == null) continue;
                if (unit.ownerNationId != nationId) continue;
                if (!unit.isUnderConstruction) continue;

                unit.constructionTurnsRemaining = Mathf.Max(0, unit.constructionTurnsRemaining - 1);
                OnUnitConstructionUpdated?.Invoke(unit);
                if (unit.constructionTurnsRemaining <= 0)
                {
                    unit.isUnderConstruction = false;
                    OnUnitConstructionUpdated?.Invoke(unit);
                    OnUnitConstructionCompleted?.Invoke(unit);
                }
            }
        }

        /// <summary>
        /// 尝试建造堡垒
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="fortType"></param>
        /// <returns></returns>
        public bool TryBuildFort(Vector3Int cell, UnitTypeConfig fortType)
        {
            if (fortType == null) return false;
            if (fortType.unitProperty != UnitProperty.Fort) return false;
            if (MapManager.instance == null || NationManager.instance == null) return false;
            if (MapManager.instance.Tilemap == null) return false;
            if (!MapManager.instance.IsCoordinateValid(cell)) return false;

            NationData nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null) return false;
            if (tile.ownerId != nation.nationId) return false;

            if (tile.tileType != TileType.Plain && tile.tileType != TileType.Forest && tile.tileType != TileType.Mountain)
            {
                return false;
            }

            if (GetUnitAtPosition(cell) != null) return false;

            if (nation.gold < fortType.goldCost || nation.industry < fortType.industryCost || nation.science < fortType.scienceCost)
            {
                return false;
            }

            GameObject fortPrefab = GetFortPrefab(fortType);
            if (fortPrefab == null) return false;

            nation.gold -= fortType.goldCost;
            nation.industry -= fortType.industryCost;
            nation.science -= fortType.scienceCost;

            Vector3 targetWorldPos = MapManager.instance.Tilemap.GetCellCenterWorld(cell);
            Vector3 spawnStartPos = targetWorldPos + new Vector3(0, 0.5f, 0);
            GameObject spawnedFort = Instantiate(fortPrefab, spawnStartPos, Quaternion.identity, unitsContainer != null ? unitsContainer.transform : null);
            StartCoroutine(AnimateSpawnUnit(spawnedFort, targetWorldPos));

            // 注意：需要在触发 OnUnitSpawned 之前就写入“建造中状态”，确保视图绑定时外观正确。
            if (GetUnitAtPosition(cell) != null)
            {
                Debug.LogWarning($"UnitManager: 位置 {cell} 已有单位");
                Destroy(spawnedFort);
                return false;
            }

            UnitData unit = new(nextUnitId++, fortType, cell, nation.nationId)
            {
                isUnderConstruction = true,
                constructionTurnsRemaining = 3,
                hasMovedThisTurn = true,
                hasAttackedThisTurn = true
            };

            allUnits.Add(unit);
            OnUnitSpawned?.Invoke(unit, spawnedFort);
            return true;
        }

        /// <summary>
        /// 获取堡垒预制体
        /// </summary>
        /// <param name="fortType"></param>
        /// <returns></returns>
        private GameObject GetFortPrefab(UnitTypeConfig fortType)
        {
            if (fortType == null) return null;
            if (availableFort == null) return null;

            foreach (var prefab in availableFort)
            {
                if (prefab == null) continue;
                var spawn = prefab.GetComponent<InitialUnitSpawn>();
                if (spawn != null && spawn.unitType == fortType)
                {
                    return prefab;
                }
            }
            return null;
        }

        /// <summary>
        /// 国家战败时移除该国所有军队
        /// </summary>
        private void RemoveAllUnitsOfNation(int nationId)
        {
            for (int i = allUnits.Count - 1; i >= 0; i--)
            {
                if (allUnits[i].ownerNationId == nationId)
                {
                    UnitData u = allUnits[i];
                    allUnits.RemoveAt(i);
                    OnUnitDestroyed?.Invoke(u);
                }
            }
            Debug.Log($"UnitManager: 国家 {nationId} 战败，已移除其所有军队");
        }

        /// <summary>
        /// 在指定城市购买单位（消耗资源）
        /// </summary>
        /// <returns>是否购买成功</returns>
        public bool TryPurchaseUnit(CityData city, GameObject unit)
        {
            var unitType = unit.GetComponent<InitialUnitSpawn>().unitType;
            if (city == null || unitType == null) return false;
            if (CityManager.instance == null || NationManager.instance == null) return false;

            NationData nation = NationManager.instance.GetNation(city.ownerNationId);
            if (nation == null) return false;

            nation.gold -= unitType.goldCost;
            nation.industry -= unitType.industryCost;
            nation.science -= unitType.scienceCost;

            Vector3 targetWorldPos = MapManager.instance.Tilemap.GetCellCenterWorld(city.cityLocation);
            Vector3 spawnStartPos = targetWorldPos + new Vector3(0, 0.5f, 0);
            GameObject spawnedUnit = Instantiate(unit, spawnStartPos, Quaternion.identity, unitsContainer.transform);

            StartCoroutine(AnimateSpawnUnit(spawnedUnit, targetWorldPos));
            SpawnUnit(city.cityLocation, unitType, city.ownerNationId, spawnedUnit, true);
            Debug.Log($"{nation.nationName} 在 {city.cityName} 购买了 {unitType.unitTypeName}");
            return true;
        }

        /// <summary>
        /// 在指定港口购买单位（消耗资源）
        /// </summary>
        /// <returns>是否购买成功</returns>
        public bool TryPurchaseUnit(PortData port, GameObject unit)
        {
            var unitType = unit.GetComponent<InitialUnitSpawn>().unitType;
            if (port == null || unitType == null) return false;
            if (PortManager.instance == null || NationManager.instance == null) return false;

            NationData nation = NationManager.instance.GetNation(port.ownerNationId);
            if (nation == null) return false;

            nation.gold -= unitType.goldCost;
            nation.industry -= unitType.industryCost;
            nation.science -= unitType.scienceCost;

            Vector3 targetWorldPos = MapManager.instance.Tilemap.GetCellCenterWorld(port.portLocation);
            Vector3 spawnStartPos = targetWorldPos + new Vector3(0, 0.5f, 0);
            GameObject spawnedUnit = Instantiate(unit, spawnStartPos, Quaternion.identity, unitsContainer.transform);

            StartCoroutine(AnimateSpawnUnit(spawnedUnit, targetWorldPos));
            SpawnUnit(port.portLocation, unitType, port.ownerNationId, spawnedUnit, true);
            Debug.Log($"{nation.nationName} 在 {port.portName} 购买了 {unitType.unitTypeName}");
            return true;
        }

        /// <summary>
        /// 在指定位置生成单位
        /// </summary>
        public UnitData SpawnUnit(Vector3Int position, UnitTypeConfig unitType, int ownerNationId, GameObject unitObject, bool isNewUnit)
        {
            if (unitType == null) return null;
            if (!MapManager.instance.IsCoordinateValid(position))
            {
                Debug.LogWarning($"UnitManager: 无效坐标 {position}");
                return null;
            }
            if (GetUnitAtPosition(position) != null)
            {
                Debug.LogWarning($"UnitManager: 位置 {position} 已有单位");
                return null;
            }

            UnitData unit = new(nextUnitId++, unitType, position, ownerNationId);

            // 判断是否为驳船
            if (unitObject.TryGetComponent<BargeUnitMapping>(out BargeUnitMapping bargeUnitMapping))
            {
                unit.unitType = bargeUnitMapping.bargeUnitType;
            }

            // 判断是否为新生成的单位
            if (isNewUnit)
            {
                unit.hasAttackedThisTurn = true;
                unit.hasMovedThisTurn = true;
            }
            else
            {
                unit.hasAttackedThisTurn = false;
                unit.hasMovedThisTurn = true;
            }
            allUnits.Add(unit);

            // 告诉UnitController绑定游戏对象
            OnUnitSpawned?.Invoke(unit, unitObject);
            return unit;
        }

        /// <summary>
        /// 在指定位置生成空投单位
        /// </summary>
        public UnitData SpawnParadropUnit(Vector3Int position, Vector3 targetWorldPos, UnitTypeConfig unitType, int ownerNationId, GameObject unitObject, bool isNewUnit)
        {
            if (unitType == null) return null;
            if (!MapManager.instance.IsCoordinateValid(position))
            {
                Debug.LogWarning($"UnitManager: 无效坐标 {position}");
                return null;
            }
            if (GetUnitAtPosition(position) != null)
            {
                Debug.LogWarning($"UnitManager: 位置 {position} 已有单位");
                return null;
            }

            UnitData unit = new(nextUnitId++, unitType, position, ownerNationId);

            // 判断是否为新生成的单位
            if (isNewUnit)
            {
                unit.hasAttackedThisTurn = true;
                unit.hasMovedThisTurn = true;
            }
            else
            {
                unit.hasAttackedThisTurn = false;
                unit.hasMovedThisTurn = true;
            }
            allUnits.Add(unit);

            StartCoroutine(AnimateSpawnUnit(unitObject, targetWorldPos));
            // 告诉UnitController绑定游戏对象
            OnUnitSpawned?.Invoke(unit, unitObject);
            return unit;
        }

        /// <summary>
        /// 生成部队的动画
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        private IEnumerator AnimateSpawnUnit(GameObject unitGo, Vector3 targetPosition)
        {
            if (unitGo == null)
                yield break;

            float spawnDuration = 0.3f;

            // 空投等场景下单位可能在动画中途被击毁并 Destroy，SetLink 会在 GameObject 销毁时自动杀掉 tween，避免 DOTween 报 NULL target
            Tween spawnTween = unitGo.transform
                .DOMove(targetPosition, spawnDuration)
                .SetUpdate(true)
                .SetLink(unitGo);

            yield return spawnTween.WaitForCompletion();
        }

        /// <summary>
        /// 获取指定坐标上的单位
        /// </summary>
        public UnitData GetUnitAtPosition(Vector3Int position)
        {
            foreach (var unit in allUnits)
            {
                if (unit.position == position)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 获取指定国家的所有单位
        /// </summary>
        public List<UnitData> GetUnitsByNation(int nationId)
        {
            List<UnitData> result = new();
            foreach (var unit in allUnits)
            {
                if (unit.ownerNationId == nationId)
                    result.Add(unit);
            }
            return result;
        }

        /// <summary>
        /// 获取单位的可移动范围
        /// </summary>
        public HashSet<Vector3Int> GetReachablePositions(UnitData unit)
        {
            HashSet<Vector3Int> reachable = new();

            if (unit == null || unit.unitType == null) return reachable;

            float maxCost = unit.MovementRange;

            PriorityQueue<Vector3Int, float> priorityQueue = new();
            Dictionary<Vector3Int, float> _minCostToReach = new();

            // 初始化：当前位置成本为0
            Vector3Int startPos = unit.position;
            priorityQueue.Enqueue(startPos, 0);
            _minCostToReach[startPos] = 0;
            reachable.Add(startPos); // 加入自身位置

            while (priorityQueue.Count > 0)
            {
                var (currentPos, currentCost) = priorityQueue.DequeueWithPriority();

                if (currentCost > maxCost) continue;

                List<Vector3Int> neighbors = HexGridUtils.GetPointNeighbors(currentPos);
                if (neighbors == null || neighbors.Count == 0) continue;

                foreach (Vector3Int nextPos in neighbors)
                {
                    if (!MapManager.instance.IsCoordinateValid(nextPos)) continue;

                    MapTileData tile = MapManager.instance.GetTileData(nextPos);
                    if (tile == null) continue;
                    float moveCost = GetMoveCost(unit, tile.tileType);
                    if (moveCost <= 0) continue;

                    float newCost = currentCost + moveCost;
                    if (newCost > maxCost) continue;

                    UnitData blockingUnit = GetUnitAtPosition(nextPos);
                    if (blockingUnit != null && blockingUnit.ownerNationId != unit.ownerNationId)
                    {
                        // 敌军阻挡
                        continue;
                    }

                    // 检查是否有更优路径
                    if (_minCostToReach.TryGetValue(nextPos, out float existingCost))
                    {
                        if (existingCost <= newCost) continue;
                    }

                    // 更新最小成本并加入队列
                    _minCostToReach[nextPos] = newCost;
                    priorityQueue.Enqueue(nextPos, newCost);
                    if (blockingUnit == null)
                    {
                        reachable.Add(nextPos);
                    }
                }
            }

            reachable.Remove(startPos);
            return reachable;
        }

        /// <summary>
        /// 在六边形网格上为单位寻找从当前位置到目标格的最短路径（按移动成本）
        /// 返回包含起点和终点的一串格子坐标；若无法到达，返回 null。
        /// </summary>
        public List<Vector3Int> FindPath(UnitData unit, Vector3Int targetPosition)
        {
            if (unit == null || unit.unitType == null) return null;
            if (!MapManager.instance.IsCoordinateValid(targetPosition)) return null;

            float maxCost = unit.MovementRange;

            PriorityQueue<Vector3Int, float> priorityQueue = new();
            Dictionary<Vector3Int, float> _minCostToReach = new();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new();

            Vector3Int startPos = unit.position;
            priorityQueue.Enqueue(startPos, 0);
            _minCostToReach[startPos] = 0;

            bool reached = false;

            while (priorityQueue.Count > 0)
            {
                var (currentPos, currentCost) = priorityQueue.DequeueWithPriority();
                if (currentCost > maxCost) continue;

                if (currentPos == targetPosition)
                {
                    reached = true;
                    break;
                }

                List<Vector3Int> neighbors = HexGridUtils.GetPointNeighbors(currentPos);
                if (neighbors == null || neighbors.Count == 0) continue;

                foreach (Vector3Int nextPos in neighbors)
                {
                    if (!MapManager.instance.IsCoordinateValid(nextPos)) continue;

                    MapTileData tile = MapManager.instance.GetTileData(nextPos);
                    if (tile == null) continue;
                    float moveCost = GetMoveCost(unit, tile.tileType);
                    if (moveCost <= 0) continue;

                    float newCost = currentCost + moveCost;
                    if (newCost > maxCost) continue;

                    UnitData blockingUnit = GetUnitAtPosition(nextPos);
                    if (blockingUnit != null && blockingUnit.ownerNationId != unit.ownerNationId)
                    {
                        // 敌军阻挡
                        continue;
                    }

                    if (_minCostToReach.TryGetValue(nextPos, out float existingCost) && existingCost <= newCost)
                    {
                        continue;
                    }

                    _minCostToReach[nextPos] = newCost;
                    cameFrom[nextPos] = currentPos;
                    priorityQueue.Enqueue(nextPos, newCost);
                }
            }

            if (!reached)
            {
                return null;
            }

            // 回溯路径：target -> start
            List<Vector3Int> path = new();
            Vector3Int current = targetPosition;
            path.Add(current);
            while (current != startPos)
            {
                if (!cameFrom.TryGetValue(current, out Vector3Int prev))
                {
                    // 理论上不应发生，防御性处理
                    return null;
                }
                current = prev;
                path.Add(current);
            }

            path.Reverse(); // 变为 start -> target
            return path;
        }

        /// <summary>
        /// 获取单位的可攻击范围内的可攻击目标（尖顶六边形距离）
        /// </summary>
        public HashSet<Vector3Int> GetAttackablePositions(UnitData unit)
        {
            HashSet<Vector3Int> attackable = new();
            if (unit == null || unit.unitType == null) return attackable;
            if (unit.isUnderConstruction) return attackable;

            int range = unit.AttackRange;
            HashSet<Vector3Int> inRange = HexGridUtils.GetCellsWithinHexDistance(unit.position, range);

            foreach (Vector3Int target in inRange)
            {
                if (target == unit.position) continue;
                if (!MapManager.instance.IsCoordinateValid(target)) continue;

                UnitData targetUnit = GetUnitAtPosition(target);
                if (targetUnit != null && targetUnit.ownerNationId != unit.ownerNationId)
                {
                    // 陆地单位和潜艇无法相互攻击
                    bool attackerIsLand = unit.unitType.unitProperty == UnitProperty.Soldier || unit.unitType.unitProperty == UnitProperty.Armor || unit.unitType.unitProperty == UnitProperty.Fort;
                    bool defenderIsLand = targetUnit.unitType.unitProperty == UnitProperty.Soldier || targetUnit.unitType.unitProperty == UnitProperty.Armor || targetUnit.unitType.unitProperty == UnitProperty.Fort;
                    bool attackerIsSubmarine = IsSubmarine(unit);
                    bool defenderIsSubmarine = IsSubmarine(targetUnit);
                    if ((attackerIsLand && defenderIsSubmarine) || (defenderIsLand && attackerIsSubmarine))
                    {
                        // 堡垒单位海岸炮除外
                        if (unit.unitType.unitTypeName == "海岸炮" && defenderIsSubmarine)
                        {
                            attackable.Add(target);
                        }
                        continue;
                    }

                    attackable.Add(target);
                }
            }
            return attackable;
        }

        /// <summary>
        /// 获取单位进入某地形的移动消耗，-1 表示不可通行
        /// </summary>
        private float GetMoveCost(UnitData unit, TileType tileType)
        {
            if (unit?.unitType == null) return 1;

            return tileType switch
            {
                TileType.Port or TileType.Water => unit.unitType.waterMoveCost <= 0 ? -1 : unit.unitType.waterMoveCost,
                TileType.Plain or TileType.City => unit.unitType.plainAndCityMoveCost <= 0 ? -1 : unit.unitType.plainAndCityMoveCost,
                TileType.Forest => unit.unitType.forestMoveCost <= 0 ? -1 : unit.unitType.forestMoveCost,
                TileType.Mountain => unit.unitType.mountainMoveCost <= 0 ? -1 : unit.unitType.mountainMoveCost,
                _ => 1,
            };
        }

        /// <summary>
        /// 更改单位位置到目标位置
        /// </summary>
        /// <returns>是否移动成功（含占领城市）</returns>
        public bool TryMoveUnit(UnitData unit, Vector3Int targetPosition)
        {
            if (unit == null || unit.hasAttackedThisTurn || unit.hasMovedThisTurn || CityManager.instance == null || PortManager.instance == null) return false;

            UnitData targetUnit = GetUnitAtPosition(targetPosition);
            if (targetUnit != null)
            {
                Debug.Log("目标格有单位，无法移动（请使用攻击）");
                return false;
            }

            if (!MapManager.instance.TileDataMap.TryGetValue(unit.position, out MapTileData oldTileData))
            {
                Debug.LogWarning("UnitManager: 当前格的地形无法识别！");
                return false;
            }

            unit.position = targetPosition;
            unit.hasMovedThisTurn = true;

            // 检查是否占领城市
            CityData city = CityManager.instance.GetCityAtPosition(targetPosition);
            if (city != null && city.ownerNationId != unit.ownerNationId)
            {
                CaptureCity(unit, city);
            }
            // 检查是否占领港口
            PortData port = PortManager.instance.GetPortAtPosition(targetPosition);
            if (port != null && port.ownerNationId != unit.ownerNationId)
            {
                CapturePort(unit, port);
            }

            // 检查是否入水
            if ((oldTileData.tileType != TileType.Water && oldTileData.tileType != TileType.Port) &&
                MapManager.instance.TileDataMap.TryGetValue(targetPosition, out MapTileData newTileData) &&
                (newTileData.tileType == TileType.Water || newTileData.tileType == TileType.Port))
            {
                ChangeLandUnitToBarge(unit);
            }

            // 检查是否上岸
            if ((oldTileData.tileType == TileType.Water || oldTileData.tileType == TileType.Port) &&
                MapManager.instance.TileDataMap.TryGetValue(targetPosition, out newTileData) &&
                (newTileData.tileType != TileType.Water && newTileData.tileType != TileType.Port))
            {
                ChangeBargeUnitToLand(unit);
            }

            return true;
        }

        /// <summary>
        /// 攻击目标位置上的敌方单位
        /// </summary>
        /// <returns>是否攻击成功</returns>
        public bool TryAttack(UnitData attacker, Vector3Int targetPosition)
        {
            if (attacker == null || attacker.hasAttackedThisTurn) return false;
            if (attacker.isUnderConstruction) return false;

            var attackable = GetAttackablePositions(attacker);
            if (!attackable.Contains(targetPosition))
            {
                Debug.Log("目标不在攻击范围内");
                return false;
            }

            UnitData defender = GetUnitAtPosition(targetPosition);
            if (defender == null || defender.ownerNationId == attacker.ownerNationId)
            {
                Debug.Log("目标格无敌方单位");
                return false;
            }

            // 获取单位的特攻
            int attackerStr = GetSpecialAttack(attacker, defender);
            int defenderStr = GetSpecialAttack(defender, attacker);

            int attackerStrength = Mathf.CeilToInt(attackerStr * Random.Range(0.8f, 1.2f) * attacker.HealthRate);
            float defenderHealthRate = defender.HealthRate;
            defender.currentHealth = Mathf.Max(0, defender.currentHealth - attackerStrength);

            if (FloatingDamageManager.instance != null && attackerStrength > 0)
                FloatingDamageManager.instance.ShowDefenderDamage(defender.position, attackerStrength);

            if (defender.currentHealth <= 0)
            {
                allUnits.Remove(defender);
                OnUnitDestroyed?.Invoke(defender);
                OnUnitAttacked?.Invoke(attacker, defender);
                attacker.hasAttackedThisTurn = true;
                Debug.Log($"{attacker.unitType.unitTypeName} 击败了 {defender.unitType.unitTypeName}");
                return true;
            }

            int defenderStrength = Mathf.CeilToInt(defenderStr * Random.Range(0.8f, 1.2f) * defenderHealthRate);

            // 如果攻击者为火炮单位、潜艇、航空母舰或者防守单位为无法反击的单位、建造中的堡垒或攻击距离不够则无法反击
            if (!IsUnitInAvailableList(attacker, availableArtillery) &&
                !IsSubmarine(attacker) &&
                !IsAircraftCarrier(attacker) &&
                !IsUnderConstructionFort(defender) &&
                defender.AttackRange >= HexGridUtils.GetHexDistance(attacker.position, targetPosition) &&
                !CannotBeReversed(defender))
            {
                attacker.currentHealth = Mathf.Max(0, attacker.currentHealth - defenderStrength);

                if (FloatingDamageManager.instance != null && defenderStrength > 0)
                    FloatingDamageManager.instance.ShowAttackerCounterDamage(attacker.position, defenderStrength);
            }

            if (attacker.currentHealth <= 0)
            {
                allUnits.Remove(attacker);
                OnUnitDestroyed?.Invoke(attacker);
                OnUnitAttacked?.Invoke(attacker, defender);
                Debug.Log($"{attacker.unitType.unitTypeName} 被 {defender.unitType.unitTypeName} 击败");
                return true;
            }
            else
            {
                OnUnitAttacked?.Invoke(attacker, defender);
                attacker.hasAttackedThisTurn = true;
                Debug.Log($"{attacker.unitType.unitTypeName} 对 {defender.unitType.unitTypeName} 造成 {attackerStrength}伤害\n" +
                    $"{defender.unitType.unitTypeName} 对 {defender.unitType.unitTypeName} 造成 {defenderStrength}伤害\n");
                return true;
            }
        }

        /// <summary>
        /// 占领城市
        /// </summary>
        public void CaptureCity(UnitData unit, CityData city)
        {
            if (city == null || unit == null) return;
            if (CityManager.instance == null || NationManager.instance == null) return;

            NationData oldOwner = NationManager.instance.GetNation(city.ownerNationId);
            NationData newOwner = NationManager.instance.GetNation(unit.ownerNationId);
            if (oldOwner == null || newOwner == null) return;

            CityManager.instance.TransferCityOwnership(city, newOwner.nationName);
            OnCityCaptured?.Invoke(unit, city);
            Debug.Log($"{newOwner.nationName} 的 {unit.unitType.unitTypeName} 占领了 {city.cityName}！");

            // 检查原国家是否战败（失去最后一座城市）
            if (oldOwner.ownedCities.Count == 0)
            {
                oldOwner.isDefeated = true;
                NationManager.instance.OnNationDefeated?.Invoke(oldOwner);
            }
        }

        /// <summary>
        /// 占领港口
        /// </summary>
        private void CapturePort(UnitData unit, PortData port)
        {
            if (port == null || unit == null) return;
            if (PortManager.instance == null || NationManager.instance == null) return;

            NationData oldOwner = NationManager.instance.GetNation(port.ownerNationId);
            NationData newOwner = NationManager.instance.GetNation(unit.ownerNationId);
            if (oldOwner == null || newOwner == null) return;

            PortManager.instance.TransferPortOwnership(port, newOwner);
            OnPortCaptured?.Invoke(unit, port);
            Debug.Log($"{newOwner.nationName} 的 {unit.unitType.unitTypeName} 占领了 {port.portName}！");
        }

        /// <summary>
        /// 获取对不同敌方单位种类的对应攻击数值
        /// </summary>
        /// <param name="Attacker"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private int GetSpecialAttack(UnitData Attacker, UnitData target)
        {
            int strength = 0;
            switch (target.unitType.unitProperty)
            {
                case UnitProperty.Soldier:
                    strength = Attacker.unitType.attackStrength_Soldier;
                    break;
                case UnitProperty.Armor:
                    strength = Attacker.unitType.attackStrength_Armor;
                    break;
                case UnitProperty.Fort:
                    strength = Attacker.unitType.attackStrength_Fort;
                    break;
                case UnitProperty.Warship:
                    strength = Attacker.unitType.attackStrength_Warship;
                    break;
                case UnitProperty.Battleship:
                    strength = Attacker.unitType.attackStrength_Battleship;
                    break;
            }
            return strength;
        }

        /// <summary>
        /// 查看是否符合满足生产条件（城市）
        /// </summary>
        /// <param name="city"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public bool CanSatisfyProduceCondition(CityData city, UnitTypeConfig unitType)
        {
            // 查看单位生产条件
            switch (unitType.unitProperty)
            {
                case UnitProperty.Soldier:
                    if (unitType.produceCondition > city.cityKindsLevel.cityLevel)
                    {
                        return false;
                    }
                    break;
                case UnitProperty.Armor:
                    if (unitType.produceCondition > city.cityKindsLevel.industryLevel)
                    {
                        return false;
                    }
                    break;
                default:
                    return false;
            }

            NationData nation = NationManager.instance.GetNation(city.ownerNationId);
            if (nation.gold < unitType.goldCost ||
                nation.industry < unitType.industryCost ||
                nation.science < unitType.scienceCost)
            {
                Debug.Log($"资源不足：需要 金币{unitType.goldCost} 工业{unitType.industryCost} 科技{unitType.scienceCost}");
                return false;
            }

            // 城市格子上不能已有己方单位
            if (GetUnitAtPosition(city.cityLocation) != null)
            {
                Debug.Log("城市格子上已有单位，无法在此生产");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查看是否符合满足生产条件（港口）
        /// </summary>
        /// <param name="city"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public bool CanSatisfyProduceCondition(PortData port, UnitTypeConfig unitType)
        {
            // 查看单位生产条件

            if (unitType.produceCondition > port.portLevel)
            {
                return false;
            }

            NationData nation = NationManager.instance.GetNation(port.ownerNationId);
            if (nation.gold < unitType.goldCost ||
                nation.industry < unitType.industryCost ||
                nation.science < unitType.scienceCost)
            {
                Debug.Log($"资源不足：需要 金币{unitType.goldCost} 工业{unitType.industryCost} 科技{unitType.scienceCost}");
                return false;
            }

            // 城市格子上不能已有己方单位
            if (GetUnitAtPosition(port.portLocation) != null)
            {
                Debug.Log("港口格子上已有单位，无法在此生产");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取购买条件的各类城市图标
        /// </summary>
        /// <param name="unitTypeConfig"></param>
        /// <returns></returns>
        public Sprite GetUnitProduceConditionSprite(UnitTypeConfig unitTypeConfig)
        {
            if (unitTypeConfig == null || unitTypeConfig.produceCondition <= 0) return null;
            if (CityManager.instance == null || PortManager.instance == null) return null;
            switch (unitTypeConfig.unitProperty)
            {
                case UnitProperty.Soldier:
                    if (unitTypeConfig.produceCondition > CityManager.instance.cityLevels.Count)
                        return null;
                    return CityManager.instance.cityLevels[unitTypeConfig.produceCondition - 1];

                case UnitProperty.Armor:
                    if (unitTypeConfig.produceCondition > CityManager.instance.industry.Count)
                        return null;
                    return CityManager.instance.industry[unitTypeConfig.produceCondition - 1];

                case UnitProperty.Warship:
                case UnitProperty.Battleship:
                    if (unitTypeConfig.produceCondition > PortManager.instance.portLevels.Count)
                        return null;
                    return PortManager.instance.portLevels[unitTypeConfig.produceCondition - 1];
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取购买条件的各类城市图标（战机）
        /// </summary>
        /// <param name="unitTypeConfig"></param>
        /// <returns></returns>
        public Sprite GetUnitProduceConditionSprite(AirMissionConfig airMissionConfig)
        {
            if (airMissionConfig == null || airMissionConfig.airportLevel <= 0) return null;
            if (CityManager.instance == null) return null;

            return CityManager.instance.airport[airMissionConfig.airportLevel - 1];
        }

        /// <summary>
        /// 目标单位是否是列表中的
        /// </summary>
        /// <returns></returns>
        public bool IsUnitInAvailableList(UnitData unit, List<GameObject> availableList)
        {
            if (unit == null || availableList == null || unit.unitType == null)
            {
                return false;
            }

            foreach (var unitItem in availableList)
            {
                var unitItemType = unitItem.GetComponent<InitialUnitSpawn>();
                if (unitItemType != null && unitItemType.unitType == unit.unitType)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSubmarine(UnitData unit)
        {
            if (unit?.unitType == null) return false;
            return unit.unitType.isSubmarine || unit.unitType.unitTypeName == "潜艇";
        }

        private static bool IsAircraftCarrier(UnitData unit)
        {
            if (unit?.unitType == null) return false;
            return unit.unitType.unitTypeName == "航空母舰";
        }

        private static bool IsUnderConstructionFort(UnitData unit)
        {
            if (unit?.unitType == null) return false;
            return unit.isUnderConstruction;
        }

        private static bool CannotBeReversed(UnitData unit)
        {
            if (unit?.unitType == null) return false;
            return unit.unitType.cannotBeReversed;
        }

        /// <summary>
        /// 将陆地单位变为驳船单位
        /// </summary>
        /// <param name="unit"></param>
        private void ChangeLandUnitToBarge(UnitData unit)
        {
            if (unit == null || barge == null) return;

            Vector3 position = MapManager.instance.Tilemap.CellToWorld(unit.position);
            GameObject bargeGo = Instantiate(barge, position, Quaternion.identity, unitsContainer.transform);

            if (bargeGo.TryGetComponent<BargeUnitMapping>(out var bargeUnitMapping) && UnitController.instance != null)
            {
                bargeUnitMapping.landUnitType = unit.unitType;
                bargeUnitMapping.landUnitSprite.sprite = unit.unitType.unitIcon;
                unit.unitType = bargeUnitMapping.bargeUnitType;
                OnUnitBarged?.Invoke(unit, bargeGo);
                return;
            }
            else
            {
                Debug.LogWarning("UnitManager: 将陆地单位变为驳船单位失败！");
                return;
            }
        }

        /// <summary>
        /// 将驳船单位变为陆地单位
        /// </summary>
        /// <param name="unit"></param>
        private void ChangeBargeUnitToLand(UnitData unit)
        {
            if (unit == null || UnitController.instance == null) return;

            Vector3 position = MapManager.instance.Tilemap.CellToWorld(unit.position);
            GameObject bargeGo = UnitController.instance.GetUnitGameObject(unit);

            if (bargeGo.TryGetComponent<BargeUnitMapping>(out var bargeUnitMapping))
            {
                GameObject unitGo = GetUnitPrefab(bargeUnitMapping.landUnitType);
                if (unitGo != null)
                {
                    unit.unitType = bargeUnitMapping.landUnitType;
                    GameObject unitGameObject = Instantiate(unitGo, position, Quaternion.identity, unitsContainer.transform);

                    OnUnitBarged?.Invoke(unit, unitGameObject);
                    return;
                }
            }
            Debug.LogWarning("UnitManager: 将驳船单位变为陆地单位失败！");
            return;
        }

        public GameObject GetUnitPrefab(UnitTypeConfig unitType)
        {
            switch (unitType.unitProperty)
            {
                case UnitProperty.Soldier:
                    return availableSoldier.Find(x => x.GetComponent<InitialUnitSpawn>().unitType == unitType);
                case UnitProperty.Armor:
                    GameObject Go = availableArmor.Find(x => x.GetComponent<InitialUnitSpawn>().unitType == unitType);
                    return Go != null ? Go : availableArtillery.Find(x => x.GetComponent<InitialUnitSpawn>().unitType == unitType);
                case UnitProperty.Warship:
                case UnitProperty.Battleship:
                    return availableShip.Find(x => x.GetComponent<InitialUnitSpawn>().unitType == unitType);
                default:
                    return null;
            }
        }
    }
}
