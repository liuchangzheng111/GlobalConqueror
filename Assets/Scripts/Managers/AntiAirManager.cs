using System.Collections;
using GlobalConqueror.Controllers;
using GlobalConqueror.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Managers
{
    public class AntiAirManager : MonoBehaviour
    {
        public static AntiAirManager instance;

        [Header("防空配置")]
        public List<AntiAirConfig> antiAir = new();

        [Header("开局防空（地图设计）")]
        [Tooltip("其子物体挂 InitialAntiAirSpawn，地图与城市初始化后写入地块，不消耗资源")]
        [SerializeField] private GameObject initialAntiAirContainer;

        [HideInInspector]
        public bool initialAntiAirManagerSpawned = false;

        /// <summary>场景与城市上的开局防空已全部写入地图。</summary>
        public bool IsInitialPlacementCompleted { get; private set; }


        /// <summary>防空建造成功并已扣费、地块已写入后触发，供 UI 刷新资源/列表。</summary>
        public Action<int, Vector3Int, AntiAirConfig> OnAntiAirBuilt;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            while (MapManager.instance == null || !MapManager.instance.InitializeMapCompleted)
                yield return null;

            PlaceInitialAntiAirFromSceneMarkers();

            while (CityManager.instance == null || !CityManager.instance.IsCityTilemapInitialized)
                yield return null;

            IsInitialPlacementCompleted = true;
            initialAntiAirManagerSpawned = true;
        }

        /// <summary>
        /// 从场景标记中放置开局防空
        /// </summary>
        private void PlaceInitialAntiAirFromSceneMarkers()
        {
            if (initialAntiAirContainer == null) return;

            Tilemap tilemap = MapManager.instance.Tilemap;
            if (tilemap == null) return;

            var spawns = initialAntiAirContainer.GetComponentsInChildren<InitialAntiAirSpawn>(true);
            foreach (InitialAntiAirSpawn spawn in spawns)
            {
                AntiAirConfig config = spawn.antiAirConfig != null
                    ? spawn.antiAirConfig
                    : ResolveConfigByLevelIndex(spawn.antiAirLevelIndex);
                if (config == null)
                {
                    Debug.LogWarning($"AntiAirManager: {spawn.gameObject.name} 未指定防空配置，已跳过");
                    continue;
                }

                Vector3Int cell = tilemap.WorldToCell(spawn.transform.position);
                if (!MapManager.instance.IsCoordinateValid(cell))
                {
                    Debug.LogWarning($"AntiAirManager: {spawn.gameObject.name} 位置 {cell} 不在有效地图内，已跳过");
                    continue;
                }

                if (!PlaceInitialAntiAir(cell, config))
                    Debug.LogWarning($"AntiAirManager: 无法在 {cell} 放置开局防空（格无效或已有防空）");
            }
        }

        /// <summary>
        /// 写入开局防空（不扣资源、不触发 OnAntiAirBuilt）。
        /// </summary>
        public bool PlaceInitialAntiAir(Vector3Int cell, AntiAirConfig config)
        {
            if (config == null || MapManager.instance == null) return false;
            if (!MapManager.instance.IsCoordinateValid(cell)) return false;

            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null || tile.antiAir != null) return false;

            return MapManager.instance.SetAntiAirLevel(cell, config);
        }

        /// <summary>
        /// 按 AntiAirManager.antiAir 列表索引解析配置
        /// </summary>
        public AntiAirConfig ResolveConfigByLevelIndex(int levelIndex)
        {
            if (levelIndex <= 0 || antiAir == null || antiAir.Count == 0) return null;
            int i = levelIndex - 1;
            if (i < 0 || i >= antiAir.Count) return null;
            return antiAir[i];
        }

        /// <summary>
        /// 判断是否可以建造防空
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

