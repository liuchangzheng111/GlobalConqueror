using GlobalConqueror.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    public class AntiAirManager : MonoBehaviour
    {
        public static AntiAirManager instance;

        [Header("防空配置")]
        public List<AntiAirConfig> antiAir = new();

        [HideInInspector]
        public bool initialAntiAirManagerSpawned = false;

        /// <summary>防空建造成功并已扣费、地块已写入后触发，供 UI 刷新资源/列表。</summary>
        public Action<int, Vector3Int, AntiAirConfig> OnAntiAirBuilt;

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

            if (tile.tileType != TileType.Plain && tile.tileType != TileType.Forest && tile.tileType != TileType.Mountain && tile.tileType != TileType.City) return false;

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
        public bool TryBuildAntiAir(Vector3Int cell, AntiAirConfig antiAir)
        {
            if (!CanBuildAntiAir(cell)) return false;

            NationData nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            int gold = antiAir.goldCost;
            int industry = antiAir.industryCost;
            int science = antiAir.scienceCost;

            if (nation.gold < gold || nation.industry < industry || nation.science < science) return false;

            nation.gold -= gold;
            nation.industry -= industry;
            nation.science -= science;

            bool placed = MapManager.instance.SetAntiAirLevel(cell, antiAir);
            if (placed)
            {
                OnAntiAirBuilt?.Invoke(nation.nationId, cell, antiAir);
            }

            return placed;
        }

        /// <summary>
        /// 获取防空设施对空袭的减伤倍率
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public float GetAirStrikeMultiplier(AntiAirConfig antiAir)
        {
            return antiAir == null ? 1 : antiAir.airStrikeDamageMultiplier;
        }

        /// <summary>
        /// 获取防空设施对空投兵的伤害
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public int GetParadropDamage(AntiAirConfig antiAir)
        {
            return antiAir == null ? 0 : antiAir.paradropDamage;
        }

        /// <summary>
        /// 获取防空设施的图标
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public Sprite GetAntiAirIcon(AntiAirConfig antiAir)
        {
            return antiAir == null ? null : antiAir.icon;
        }

        /// <summary>
        /// 获取防空设施的地块图标显示
        /// </summary>
        /// <param name="antiAirLevel"></param>
        /// <returns></returns>
        public Sprite GetAntiAirTileIcon(AntiAirConfig antiAir)
        {
            return antiAir.tileIcon;
        }
    }
}

