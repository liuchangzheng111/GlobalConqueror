using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using GlobalConqueror.Controllers;
using DG.Tweening;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 军队管理器 - 管理单位生成、移动、攻击、占领城市
    /// </summary>
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager instance;

        [Header("兵种预制体列表")]
        [SerializeField] private List<GameObject> availableUnitTypes = new List<GameObject>();

        [Header("初始地图上兵的父容器")]
        [Tooltip("其子物体需挂 InitialUnitSpawn，世界坐标会转为格子坐标作为出生点")]
        [SerializeField] private GameObject unitsContainer;

        private List<UnitData> allUnits = new List<UnitData>();
        private int nextUnitId = 1;

        [HideInInspector]
        public bool initialUnitsSpawned = false;

        public List<UnitData> AllUnits => allUnits;
        public List<GameObject> AvailableUnitTypes => availableUnitTypes;

        public System.Action<UnitData, GameObject> OnUnitSpawned;
        public System.Action<UnitData, UnitData> OnUnitAttacked;
        public System.Action<UnitData, CityData> OnCityCaptured;
        public System.Action<UnitData> OnUnitDestroyed;

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
            while (NationManager.instance == null || !NationManager.instance.isNationsInitialized)
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
            if (unitsContainer == null || MapManager.instance?.Tilemap == null)
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

                int ownerId = ResolveOwnerNationId(cell, spawn.ownerNationId);
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
        /// 解析单位所属国家：若 ownerNationId >= 0 直接返回；为 -1 时根据所在地块的所属国家（MapTileData.ownerId）自动识别。
        /// 城市格在国家初始化时已设好地块归属；非城市格需由地图或领土逻辑设置 ownerId 后才会被识别。
        /// </summary>
        /// <returns>国家 ID，无法识别时返回 -1</returns>
        private int ResolveOwnerNationId(Vector3Int cell, int ownerNationId)
        {
            if (ownerNationId >= 0)
                return ownerNationId;

            MapTileData tile = MapManager.instance?.GetTileData(cell);
            if (tile != null && tile.ownerId >= 0)
                return tile.ownerId;

            return -1;
        }

        private void OnNationTurnStart(NationData nation)
        {
            ResetUnitActionsForNation(nation.nationId);
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
                    unit.hasAttackedThisTurn = false;
                    unit.hasMovedThisTurn = false;
                }
            }
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
                Debug.Log("城市格子上已有单位，无法在此购买");
                return false;
            }

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

            UnitData unit = new UnitData(nextUnitId++, unitType, position, ownerNationId);

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
        /// 生成部队的动画
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        private IEnumerator AnimateSpawnUnit(GameObject unit, Vector3 targetPosition)
        {
            if (unit == null)
            {
                yield break;
            }         

            float spawnDuration = 0.3f; 

            Tween spawnTween = unit.transform.DOMove(targetPosition, spawnDuration).SetUpdate(true);

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
            List<UnitData> result = new List<UnitData>();
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
            HashSet<Vector3Int> reachable = new HashSet<Vector3Int>();

            if (unit == null || unit.unitType == null) return reachable;

            int maxCost = unit.MovementRange;

            PriorityQueue<Vector3Int, int> priorityQueue = new PriorityQueue<Vector3Int, int>();
            Dictionary<Vector3Int, int> _minCostToReach = new Dictionary<Vector3Int, int>();

            // 初始化：当前位置成本为0
            Vector3Int startPos = unit.position;
            priorityQueue.Enqueue(startPos, 0);
            _minCostToReach[startPos] = 0;
            reachable.Add(startPos); // 加入自身位置

            while (priorityQueue.Count > 0)
            {
                var (currentPos, currentCost) = priorityQueue.DequeueWithPriority();

                if (currentCost > maxCost) continue;

                List<Vector3Int> neighbors = HexGridUtils.GetPointyTopNeighbors(currentPos);
                if (neighbors == null || neighbors.Count == 0) continue;

                foreach (Vector3Int nextPos in neighbors)
                {
                    if (!MapManager.instance.IsCoordinateValid(nextPos)) continue;

                    MapTileData tile = MapManager.instance.GetTileData(nextPos);
                    if (tile == null) continue;
                    int moveCost = GetMoveCost(unit, tile.tileType);
                    if (moveCost <= 0) continue;

                    int newCost = currentCost + moveCost;
                    if (newCost > maxCost) continue;

                    UnitData blockingUnit = GetUnitAtPosition(nextPos);
                    if (blockingUnit != null && blockingUnit.ownerNationId != unit.ownerNationId)
                    {
                        // 敌军阻挡
                        continue;
                    }

                    // 检查是否有更优路径
                    if (_minCostToReach.TryGetValue(nextPos, out int existingCost))
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

            int maxCost = unit.MovementRange;

            PriorityQueue<Vector3Int, int> priorityQueue = new PriorityQueue<Vector3Int, int>();
            Dictionary<Vector3Int, int> _minCostToReach = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

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

                List<Vector3Int> neighbors = HexGridUtils.GetPointyTopNeighbors(currentPos);
                if (neighbors == null || neighbors.Count == 0) continue;

                foreach (Vector3Int nextPos in neighbors)
                {
                    if (!MapManager.instance.IsCoordinateValid(nextPos)) continue;

                    MapTileData tile = MapManager.instance.GetTileData(nextPos);
                    if (tile == null) continue;
                    int moveCost = GetMoveCost(unit, tile.tileType);
                    if (moveCost <= 0) continue;

                    int newCost = currentCost + moveCost;
                    if (newCost > maxCost) continue;

                    UnitData blockingUnit = GetUnitAtPosition(nextPos);
                    if (blockingUnit != null && blockingUnit.ownerNationId != unit.ownerNationId)
                    {
                        // 敌军阻挡
                        continue;
                    }

                    if (_minCostToReach.TryGetValue(nextPos, out int existingCost) && existingCost <= newCost)
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
            List<Vector3Int> path = new List<Vector3Int>();
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
        /// 获取单位的可攻击范围（尖顶六边形距离）
        /// </summary>
        public HashSet<Vector3Int> GetAttackablePositions(UnitData unit)
        {
            HashSet<Vector3Int> attackable = new HashSet<Vector3Int>();
            if (unit == null || unit.unitType == null) return attackable;

            int range = unit.AttackRange;
            HashSet<Vector3Int> inRange = HexGridUtils.GetCellsWithinHexDistance(unit.position, range);

            foreach (Vector3Int target in inRange)
            {
                if (target == unit.position) continue;
                if (!MapManager.instance.IsCoordinateValid(target)) continue;

                UnitData targetUnit = GetUnitAtPosition(target);
                if (targetUnit != null && targetUnit.ownerNationId != unit.ownerNationId)
                {
                    attackable.Add(target);
                }
            }
            return attackable;
        }

        /// <summary>
        /// 获取单位进入某地形的移动消耗，-1 表示不可通行
        /// </summary>
        private int GetMoveCost(UnitData unit, TileType tileType)
        {
            if (unit?.unitType == null) return 1;

            switch (tileType)
            {
                case TileType.Water:
                    return unit.unitType.waterMoveCost <= 0 ? -1 : unit.unitType.waterMoveCost;
                case TileType.Plain:
                case TileType.City:
                case TileType.Port:
                    return unit.unitType.plainAndCityMoveCost <= 0 ? -1 : unit.unitType.plainAndCityMoveCost;
                case TileType.Forest:
                    return unit.unitType.forestMoveCost <= 0 ? -1 : unit.unitType.forestMoveCost;
                case TileType.Mountain:
                    return unit.unitType.mountainMoveCost <= 0 ? -1 : unit.unitType.mountainMoveCost;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// 更改单位位置到目标位置
        /// </summary>
        /// <returns>是否移动成功（含占领城市）</returns>
        public bool TryMoveUnit(UnitData unit, Vector3Int targetPosition)
        {
            if (unit == null || unit.hasAttackedThisTurn || unit.hasMovedThisTurn) return false;

            UnitData targetUnit = GetUnitAtPosition(targetPosition);
            if (targetUnit != null)
            {
                Debug.Log("目标格有单位，无法移动（请使用攻击）");
                return false;
            }

            unit.position = targetPosition;
            unit.hasMovedThisTurn = true;

            // 检查是否占领城市
            CityData city = CityManager.instance?.GetCityAtPosition(targetPosition);
            if (city != null && city.ownerNationId != unit.ownerNationId)
            {
                CaptureCity(unit, city);
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

            int attackerStrength = Mathf.CeilToInt(attacker.unitType.attackStrength * Random.Range(0.8f, 1.2f) * attacker.HealthRate);
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

            int defenderStrength = Mathf.CeilToInt(defender.unitType.attackStrength * Random.Range(0.8f, 1.2f) * defenderHealthRate); 
            attacker.currentHealth = Mathf.Max(0, attacker.currentHealth - defenderStrength);

            if (FloatingDamageManager.instance != null && defenderStrength > 0)
                FloatingDamageManager.instance.ShowAttackerCounterDamage(attacker.position, defenderStrength);

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
        private void CaptureCity(UnitData unit, CityData city)
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
            if (oldOwner.ownedCitiesNames.Count == 0)
            {
                oldOwner.isDefeated = true;
                NationManager.instance.OnNationDefeated?.Invoke(oldOwner);
            }
        }
    }
}
