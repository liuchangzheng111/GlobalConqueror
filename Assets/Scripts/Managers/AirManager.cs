using System.Collections;
using System.Collections.Generic;
using GlobalConqueror.Controllers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 空军管理器 - 执行空军任务及其实施效果
    /// </summary>
    public class AirManager : MonoBehaviour
    {
        public static AirManager instance;

        [Header("空军列表")]
        [SerializeField] private List<AirMissionConfig> availableAircrafts = new();

        [HideInInspector]
        public bool initialAirManagerSpawned = false;

        public CityData currentCity; 

        public List<AirMissionConfig> AvailableAircrafts => availableAircrafts;

        public System.Action<AirMissionConfig, CityData, Vector3Int> OnAirAttackMissionExecuted;
        public System.Action<AirMissionConfig, CityData, Vector3Int> OnAirParadropMissionExecuted;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(InitializeAirManagerWhenReady());
        }

        private IEnumerator InitializeAirManagerWhenReady()
        {
            while (CityManager.instance == null || !CityManager.instance.IsCityTilemapInitialized)
            {
                yield return null;
            }
            while (UnitManager.instance == null || !UnitManager.instance.initialUnitsSpawned)
            {
                yield return null;
            }
            initialAirManagerSpawned = true;
        }

        /// <summary>
        /// 检查城市是否可以使用指定的空军任务
        /// </summary>
        /// <param name="city">城市</param>
        /// <param name="mission">空军任务</param>
        /// <returns>是否可以使用</returns>
        public bool CanUseMissionFromCity(CityData city, AirMissionConfig mission)
        {
            if (city == null || mission == null) return false;
            if (NationManager.instance == null || NationManager.instance.CurrentNation == null) return false;
            if (city.ownerNationId != NationManager.instance.CurrentNation.nationId) return false;
            if (city.cityKindsLevel == null) return false;
            return city.cityKindsLevel.airportLevel >= mission.airportLevel;
        }

        /// <summary>
        /// 尝试执行空军任务
        /// </summary>
        /// <param name="fromCity">出发城市</param>
        /// <param name="mission">空军任务</param>
        /// <param name="targetCell">目标格子</param>
        /// <returns>是否执行成功</returns>
        public bool TryExecuteMission(AirMissionConfig mission, Vector3Int targetCell)
        {
            if (currentCity == null || mission == null) return false;
            if (MapManager.instance == null || MapManager.instance.Tilemap == null) return false;
            if (NationManager.instance == null || NationManager.instance.CurrentNation == null) return false;
            if (UnitManager.instance == null) return false;

            if (!CanUseMissionFromCity(currentCity, mission)) return false;

            int distance = HexGridUtils.GetHexDistance(currentCity.cityLocation, targetCell);
            if (distance > mission.range) return false;

            NationData nation = NationManager.instance.CurrentNation;
            if (nation.gold < mission.goldCost || nation.industry < mission.industryCost || nation.science < mission.scienceCost) return false;

            bool ok = mission.type switch
            {
                AirMissionType.AttackTarget => ExecuteAttackTarget(mission, targetCell),
                AirMissionType.ParadropInfantry => ExecuteParadrop(mission, targetCell),
                _ => false
            };

            if (ok)
            {
                nation.gold -= mission.goldCost;
                nation.industry -= mission.industryCost;
                nation.science -= mission.scienceCost;

                // 在扣费之后通知 UI，否则 TurnUI 等读到的是执行前资源数，看起来像“没刷新”
                if (mission.type == AirMissionType.AttackTarget)
                {
                    OnAirAttackMissionExecuted?.Invoke(mission, currentCity, targetCell);
                }
                else if (mission.type == AirMissionType.ParadropInfantry)
                {
                    OnAirParadropMissionExecuted?.Invoke(mission, currentCity, targetCell);
                }
            }

            return ok;
        }

        /// <summary>
        /// 获取空军任务对目标单位的特殊伤害
        /// </summary>
        /// <param name="mission">空军任务</param>
        /// <param name="target">目标单位</param>
        /// <returns>特殊伤害</returns>
        private static int GetAirSpecialAttack(AirMissionConfig mission, UnitData target)
        {
            if (mission == null || target?.unitType == null) return 0;
            return target.unitType.unitProperty switch
            {
                UnitProperty.Soldier => mission.attackStrength_Soldier,
                UnitProperty.Armor => mission.attackStrength_Armor,
                UnitProperty.Fort => mission.attackStrength_Fort,
                UnitProperty.Warship => mission.attackStrength_Warship,
                UnitProperty.Battleship => mission.attackStrength_Battleship,
                _ => 0
            };
        }

        /// <summary>
        /// 获取目标格子的防空信息
        /// </summary>
        /// <param name="cell">目标格子</param>
        /// <returns>防空等级</returns>
        private static AntiAirConfig GetAntiAirLevel(Vector3Int cell)
        {
            if (MapManager.instance == null) return null;
            MapTileData tile = MapManager.instance.GetTileData(cell);
            return tile != null ? tile.antiAir : null;
        }

        /// <summary>
        /// 执行攻击目标任务
        /// </summary>
        /// <param name="mission">空军任务</param>
        /// <param name="targetCell">目标格子</param>
        /// <returns>是否执行成功</returns>
        private bool ExecuteAttackTarget(AirMissionConfig mission, Vector3Int targetCell)
        {
            UnitData targetUnit = UnitManager.instance.GetUnitAtPosition(targetCell);
            if (targetUnit == null) return false;

            if (NationManager.instance != null && NationManager.instance.CurrentNation != null &&
                targetUnit.ownerNationId == NationManager.instance.CurrentNation.nationId)
            {
                return false;
            }

            int baseDamage = GetAirSpecialAttack(mission, targetUnit);
            AntiAirConfig antiAir = GetAntiAirLevel(targetCell);
            float amendment = AntiAirManager.instance != null ? AntiAirManager.instance.GetAirStrikeMultiplier(antiAir) : 1f;

            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * amendment));
            int dmg = Mathf.CeilToInt(finalDamage * Random.Range(0.8f, 1.2f));
            if (dmg <= 0) dmg = 1;

            targetUnit.currentHealth = Mathf.Max(0, targetUnit.currentHealth - dmg);

            if (FloatingDamageManager.instance != null)
            {
                FloatingDamageManager.instance.ShowDefenderDamage(targetCell, dmg);
            }

            if (targetUnit.currentHealth <= 0)
            {
                UnitManager.instance.AllUnits.Remove(targetUnit);
                UnitManager.instance.OnUnitDestroyed?.Invoke(targetUnit);
                Debug.Log($"{targetUnit.unitType.unitTypeName} 被 {mission.missionName} 击败");
            }
            else
            {
                if (UnitController.instance != null)
                {
                    GameObject go = UnitController.instance.GetUnitGameObject(targetUnit);
                    if (go != null && go.TryGetComponent<UnitView>(out var view))
                    {
                        view.RefreshHealthBar();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 执行空投任务
        /// </summary>
        /// <param name="mission">空军任务</param>
        /// <param name="targetCell">目标格子</param>
        /// <returns>是否执行成功</returns>
        private bool ExecuteParadrop(AirMissionConfig mission, Vector3Int targetCell)
        {
            if (mission.paradropInfantryType == null) return false;
            if (!MapManager.instance.IsCoordinateValid(targetCell)) return false;

            MapTileData tile = MapManager.instance.GetTileData(targetCell);
            if (tile == null) return false;
            if (tile.tileType == TileType.Water || tile.tileType == TileType.Port) return false;

            if (UnitManager.instance.GetUnitAtPosition(targetCell) != null) return false;

            GameObject prefab = UnitManager.instance.GetUnitPrefab(mission.paradropInfantryType);
            if (prefab == null) return false;

            Vector3 targetWorldPos = MapManager.instance.Tilemap.GetCellCenterWorld(targetCell);
            Vector3 spawnStartPos = targetWorldPos + new Vector3(0, 0.5f, 0);
            GameObject spawned = Instantiate(prefab, spawnStartPos, Quaternion.identity, null);

            UnitData unit = UnitManager.instance.SpawnParadropUnit(targetCell, targetWorldPos, mission.paradropInfantryType, NationManager.instance.CurrentNation.nationId, spawned, true);
            if (unit == null)
            {
                Destroy(spawned);
                return false;
            }

            AntiAirConfig antiAir = GetAntiAirLevel(targetCell);
            int paradropDmg = AntiAirManager.instance != null ? AntiAirManager.instance.GetParadropDamage(antiAir) : 0;
            if (paradropDmg > 0)
            {
                unit.currentHealth = Mathf.Max(0, unit.currentHealth - paradropDmg);
                if (FloatingDamageManager.instance != null)
                {
                    FloatingDamageManager.instance.ShowDefenderDamage(targetCell, paradropDmg);
                }

                // 如果被防空设施击杀
                if (unit.currentHealth <= 0)
                {
                    UnitManager.instance.AllUnits.Remove(unit);
                    UnitManager.instance.OnUnitDestroyed?.Invoke(unit);
                    return true;
                }
                else
                {
                    if (UnitController.instance != null)
                    {
                        GameObject go = UnitController.instance.GetUnitGameObject(unit);
                        if (go != null && go.TryGetComponent<UnitView>(out var view))
                        {
                            view.RefreshHealthBar();
                        }
                    }
                }
            }

            // 如果空投目标为城市且无守军，则立即占领
            CityData city = CityManager.instance.GetCityAtPosition(targetCell);
            if (city != null && city.ownerNationId != currentCity.ownerNationId)
            {
                UnitManager.instance.CaptureCity(unit, city);
            }

            return true;
        }

        /// <summary>
        /// 获取空军的可空投范围内的目标（尖顶六边形距离）
        /// </summary>
        public HashSet<Vector3Int> GetParadropPositions(AirMissionConfig airMission, CityData cityData)
        {
            HashSet<Vector3Int> reachable = new();

            if (airMission == null || cityData == null) return reachable;
            if (airMission.type == AirMissionType.AttackTarget) return reachable;

            int maxRange = airMission.range;

            PriorityQueue<Vector3Int, int> priorityQueue = new();
            HashSet<Vector3Int> visited = new();

            // 初始化：当前位置成本为0
            Vector3Int startPos = cityData.cityLocation;
            priorityQueue.Enqueue(startPos, 0);
            visited.Add(startPos);
            reachable.Add(startPos); // 加入自身位置

            while (priorityQueue.Count > 0)
            {
                var (currentPos, currentDistance) = priorityQueue.DequeueWithPriority();

                // 超过最大格数就跳过
                if (currentDistance > maxRange)
                    continue;

                if (currentDistance > 0)
                {
                    UnitData blockingUnit = UnitManager.instance.GetUnitAtPosition(currentPos);
                    // 必须为陆地地块
                    MapTileData tile = MapManager.instance.GetTileData(currentPos);
                    if (tile != null &&
                        tile.tileType != TileType.Port &&
                        tile.tileType != TileType.Water &&
                        blockingUnit == null)
                    {
                        reachable.Add(currentPos);
                    }
                }

                // 没到最大距离才继续扩散
                if (currentDistance >= maxRange)
                    continue;

                List<Vector3Int> neighbors = HexGridUtils.GetPointNeighbors(currentPos);
                if (neighbors == null || neighbors.Count == 0)
                    continue;

                foreach (Vector3Int nextPos in neighbors)
                {
                    // 地图必须有效
                    if (!MapManager.instance.IsCoordinateValid(nextPos))
                        continue;

                    // 已经遍历过就跳过
                    if (visited.Contains(nextPos))
                        continue;
                    visited.Add(nextPos);

                    // 下一格距离 = 当前+1
                    int nextDistance = currentDistance + 1;

                    priorityQueue.Enqueue(nextPos, nextDistance);
                }
            }

            return reachable;
        }

        /// <summary>
        /// 获取空军的可攻击范围内的可攻击目标（尖顶六边形距离）
        /// </summary>
        public HashSet<Vector3Int> GetAttackablePositions(AirMissionConfig airMission, CityData cityData)
        {
            HashSet<Vector3Int> attackable = new();
            if (airMission == null) return attackable;
            if (airMission.type == AirMissionType.ParadropInfantry) return attackable;
            if (cityData == null) return attackable;

            int range = airMission.range;
            HashSet<Vector3Int> inRange = HexGridUtils.GetCellsWithinHexDistance(cityData.cityLocation, range);

            foreach (Vector3Int target in inRange)
            {
                if (target == cityData.cityLocation) continue;
                if (!MapManager.instance.IsCoordinateValid(target)) continue;

                UnitData targetUnit = UnitManager.instance.GetUnitAtPosition(target);
                if (targetUnit != null && targetUnit.ownerNationId != cityData.ownerNationId)
                {
                    attackable.Add(target);
                }
            }
            return attackable;
        }
    }
}