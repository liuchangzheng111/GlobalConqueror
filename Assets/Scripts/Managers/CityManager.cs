using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using System.Linq;
using GlobalConqueror.Controllers;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 城市管理器 - 管理所有城市数据
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        public static CityManager instance;

        [Header("城市地块簇")]
        public GameObject cities;

        [Header("城市详情图标预制体")]
        public GameObject cityView;

        [Header("城市各类等级列表图标(按从低到高顺序)")]
        public List<Sprite> cityLevels;
        public List<Sprite> industry;
        public List<Sprite> airport;
        public List<Sprite> science;
        public List<Sprite> supply;

        private readonly Dictionary<string, CityData> citiesDic = new();

        // 索引即城市ID，一个场景的整局游戏中初始化后不会改动
        private List<CityData> allCities;

        public bool IsCityTilemapInitialized { get; private set; } = false;
        public List<CityData> AllCities => allCities;
        public Dictionary<string, CityData> CitiesDic => citiesDic;

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
            StartCoroutine(InitializeWhenMapReady());
        }

        /// <summary>
        /// 等待地图初始化完成后再初始化城市，避免脚本执行顺序导致的空引用
        /// </summary>
        private System.Collections.IEnumerator InitializeWhenMapReady()
        {
            // 等待 MapManager 单例与地图初始化完成
            while (MapManager.instance == null || !MapManager.instance.InitializeMapCompleted)
            {
                yield return null;
            }

            InitializeCityMaps();
        }

        /// <summary>
        /// 初始化城市映射
        /// </summary>
        private void InitializeCityMaps()
        {
            citiesDic.Clear();
            IsCityTilemapInitialized = false;

            if (cities == null)
            {
                Debug.LogError("CityManager: 城市簇对象cities未赋值！");
                return;
            }

            // 初始化城市地块和等级
            Dictionary<string, Tilemap> cityTilemap = new();
            Dictionary<string, CityLevelMapping> cityLevels = new();

            Tilemap[] childTilemaps = cities.GetComponentsInChildren<Tilemap>(includeInactive: false);

            foreach (Tilemap tilemap in childTilemaps)
            {
                if (!cityTilemap.ContainsKey(tilemap.name))
                {
                    cityTilemap.Add(tilemap.name, tilemap);
                    Debug.Log($"CityManager: 加载城市地块 - {tilemap.name}");
                }

                CityLevelMapping levelMapping = tilemap.GetComponent<CityLevelMapping>();
                if (!cityLevels.ContainsKey(tilemap.name) && levelMapping != null)
                {
                    cityLevels.Add(tilemap.name, levelMapping);
                    Debug.Log($"CityManager: 加载城市等级 - {tilemap.name}");
                }
            }


            // 初始化城市（在编辑器中已绑定，确保每一个城市都有国家归属且不要有不存在的城市，否则会出现问题）
            int countIndex = 0;
            foreach (var item in MapManager.instance.CitiesTile)
            {
                foreach (var keyValuePair in cityTilemap)
                {
                    if (keyValuePair.Value.GetTile(item) && cityLevels.TryGetValue(keyValuePair.Key, out CityLevelMapping cityLevel))
                    {
                        citiesDic.Add(keyValuePair.Key, new CityData(
                            countIndex,
                            keyValuePair.Key,
                            keyValuePair.Value,
                            item,
                            -1,
                            cityLevel.CreateCityLevel()
                            ));
                        break;
                    }
                }
            }
            allCities = citiesDic.Values.ToList();

            // 初始化CityView
            foreach (var city in allCities)
            {
                Vector3 location = MapManager.instance.Tilemap.CellToWorld(city.cityLocation);
                GameObject cityGo = Instantiate(cityView, location, Quaternion.identity, this.transform);
                if (cityGo.TryGetComponent<CityView>(out var view))
                {
                    view.Setup(city);
                }
                else
                {
                    Debug.LogWarning("CityManager: 城市详情预制体无CityView组件！");
                }
            }

            IsCityTilemapInitialized = true;
            Debug.Log($"CityManager: 城市初始化完成，共加载 {cityTilemap.Count} 个城市Tilemap");
        }

        /// <summary>
        /// 获取指定国家的所有城市
        /// </summary>
        public List<CityData> GetCitiesByNation(string nation)
        {
            List<CityData> result = new();
            foreach (var city in NationManager.instance.NationsDic[nation].ownedCitiesNames)
            {
                result.Add(citiesDic[city]);
            }
            return result;
        }

        /// <summary>
        /// 根据坐标获取该位置的城市（若有）
        /// </summary>
        public CityData GetCityAtPosition(Vector3Int position)
        {
            if (allCities == null) return null;
            foreach (var city in allCities)
            {
                if (city.cityLocation == position)
                    return city;
            }
            return null;
        }

        /// <summary>
        /// 转移城市所有权
        /// </summary>
        public void TransferCityOwnership(CityData city, string newOwnerName)
        {
            if (city == null) return;

            int oldOwnerId = city.ownerNationId;

            NationData oldOwner = NationManager.instance.GetNation(oldOwnerId);
            NationData newOwner = NationManager.instance.NationsDic[newOwnerName];

            oldOwner?.RemoveCity(city);

            newOwner?.AddCity(city);
        }
    }
}
