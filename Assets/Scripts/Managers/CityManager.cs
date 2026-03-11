using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using System.Linq;

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

        private Dictionary<string, CityData> citiesDic = new Dictionary<string, CityData>();

        // 索引即城市ID，一个场景的整局游戏中初始化后不会改动
        private List<CityData> allCities;

        public bool IsCityTilemapInitialized { get; private set; } = false;
        public List<CityData> AllCities => allCities;
        public Dictionary<string, CityData> CitiesDic => citiesDic;
        
        public System.Action OnCitiesTilemapInitialized;

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
            if (MapManager.instance.InitializeMapCompleted)
            {
                InitializeCityMaps();
            }         
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

            // 初始化城市地块
            Dictionary<string, Tilemap> cityTilemap = new Dictionary<string, Tilemap>();
            Tilemap[] childTilemaps = cities.GetComponentsInChildren<Tilemap>(includeInactive: false);
            foreach (Tilemap tilemap in childTilemaps)
            {
                if (!cityTilemap.ContainsKey(tilemap.name))
                {
                    cityTilemap.Add(tilemap.name, tilemap);
                    Debug.Log($"CityManager: 加载城市Tilemap - {tilemap.name}");
                }
            }

            // 初始化城市（在编辑器中已绑定，确保每一个城市都有国家归属且不要有不存在的城市，否则会出现问题）
            int countIndex = 0;
            foreach (var item in MapManager.instance.CitiesTile)
            {
                foreach (var keyValuePair in cityTilemap)
                {
                    if (keyValuePair.Value.GetTile(item))
                    {
                        citiesDic.Add(keyValuePair.Key, new CityData(
                            countIndex,
                            keyValuePair.Key,
                            keyValuePair.Value,
                            item,
                            -1
                            ));
                        break;
                    }
                }
            }
            allCities = citiesDic.Values.ToList();

            IsCityTilemapInitialized = true;
            OnCitiesTilemapInitialized?.Invoke();
            Debug.Log($"CityManager: 城市初始化完成，共加载 {cityTilemap.Count} 个城市Tilemap");
        }

        /// <summary>
        /// 获取指定国家的所有城市
        /// </summary>
        public List<CityData> GetCitiesByNation(string nation)
        {
            List<CityData> result = new List<CityData>();
            foreach (var city in NationManager.instance.NationsDic[nation].ownedCitiesNames)
            {
                result.Add(citiesDic[city]);
            }
            return result;
        }

        /// <summary>
        /// 转移城市所有权
        /// </summary>
        public void TransferCityOwnership(CityData city, string newOwnerName)
        {
            if (city == null) return;

            int oldOwnerId = city.ownerNationId;

            NationData oldOwner = NationManager.instance?.GetNation(oldOwnerId);
            NationData newOwner = NationManager.instance?.NationsDic[newOwnerName];

            if (oldOwner != null)
            {
                oldOwner.RemoveCity(city);
            }

            if (newOwner != null)
            {
                newOwner.AddCity(city);
            }
        }
    }
}
