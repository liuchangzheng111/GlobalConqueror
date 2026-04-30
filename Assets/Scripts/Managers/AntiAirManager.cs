using GlobalConqueror.Models;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    public class AntiAirManager : MonoBehaviour
    {
        public static AntiAirManager instance;

        [Header("建造消耗（按等级 0~3）")]
        public int[] goldCostByLevel = { 0, 80, 160, 250 };
        public int[] industryCostByLevel = { 0, 30, 50, 75 };
        public int[] scienceCostByLevel = { 0, 0, 5, 10 };

        [Header("对空袭减伤倍率（按等级 0~3）")]
        public float[] airStrikeDamageMultiplierByLevel = { 1f, 0.8f, 0.6f, 0.4f };

        [Header("对空投兵伤害（按等级 0~3）")]
        public int[] paradropDamageByLevel = { 0, 15, 35, 55 };

        [Header("地块显示图标（按等级 0~3）")]
        public Sprite[] antiAirIconByLevel = { null, null, null, null };

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// 检查是否可以建造防空设施
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool CanBuildAntiAir(Vector3Int cell)
        {
            if (MapManager.instance == null || NationManager.instance == null || UnitManager.instance == null) return false;
            if (!MapManager.instance.IsCoordinateValid(cell)) return false;

            NationData nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null) return false;
            if (tile.ownerId != nation.nationId) return false;

            if (tile.tileType != TileType.Plain && tile.tileType != TileType.Forest && tile.tileType != TileType.Mountain) return false;

            UnitData unit = UnitManager.instance.GetUnitAtPosition(cell);
            if (unit != null && unit.ownerNationId != nation.nationId) return false;

            return true;
        }

        /// <summary>
        /// 尝试建造防空设施
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool TryBuildAntiAir(Vector3Int cell, int level)
        {
            if (!CanBuildAntiAir(cell)) return false;
            level = Mathf.Clamp(level, 1, 3);

            NationData nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            int gold = GetCost(goldCostByLevel, level);
            int industry = GetCost(industryCostByLevel, level);
            int science = GetCost(scienceCostByLevel, level);

            if (nation.gold < gold || nation.industry < industry || nation.science < science) return false;

            nation.gold -= gold;
            nation.industry -= industry;
            nation.science -= science;

            return MapManager.instance.SetAntiAirLevel(cell, level);
        }

        /// <summary>
        /// 获取防空设施对空袭的减伤倍率
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public float GetAirStrikeMultiplier(int antiAirLevel)
        {
            int idx = Mathf.Clamp(antiAirLevel, 0, airStrikeDamageMultiplierByLevel.Length - 1);
            return airStrikeDamageMultiplierByLevel[idx];
        }

        /// <summary>
        /// 获取防空设施对空投兵的伤害
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public int GetParadropDamage(int antiAirLevel)
        {
            int idx = Mathf.Clamp(antiAirLevel, 0, paradropDamageByLevel.Length - 1);
            return paradropDamageByLevel[idx];
        }

        /// <summary>
        /// 获取防空设施的图标
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public Sprite GetAntiAirIcon(int antiAirLevel)
        {
            int idx = Mathf.Clamp(antiAirLevel, 0, antiAirIconByLevel.Length - 1);
            return antiAirIconByLevel[idx];
        }

        /// <summary>
        /// 获取指定等级的消耗
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static int GetCost(int[] arr, int level)
        {
            if (arr == null || arr.Length == 0) return 0;
            int idx = Mathf.Clamp(level, 0, arr.Length - 1);
            return arr[idx];
        }
    }
}

