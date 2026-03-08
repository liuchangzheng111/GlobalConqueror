using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 城市管理器 - 管理所有城市数据
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        public static CityManager instance;

        [Header("城市设置")]
        [SerializeField] private List<CityData> allCities = new List<CityData>();

        private Dictionary<Vector3Int, CityData> cityLocationMap = new Dictionary<Vector3Int, CityData>();
        private Dictionary<int, CityData> cityIdMap = new Dictionary<int, CityData>();

        public List<CityData> AllCities => allCities;

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
            InitializeCityMaps();
        }

        /// <summary>
        /// 初始化城市映射
        /// </summary>
        private void InitializeCityMaps()
        {
            cityLocationMap.Clear();
            cityIdMap.Clear();

            foreach (CityData city in allCities)
            {
                if (city != null)
                {
                    cityLocationMap[city.cityLocation] = city;
                    cityIdMap[city.cityId] = city;
                }
            }
        }

        /// <summary>
        /// 添加城市
        /// </summary>
        public void AddCity(CityData city)
        {
            if (city == null) return;

            if (!allCities.Contains(city))
            {
                allCities.Add(city);
                cityLocationMap[city.cityLocation] = city;
                cityIdMap[city.cityId] = city;
            }
        }

        /// <summary>
        /// 移除城市
        /// </summary>
        public void RemoveCity(CityData city)
        {
            if (city == null) return;

            allCities.Remove(city);
            cityLocationMap.Remove(city.cityLocation);
            cityIdMap.Remove(city.cityId);
        }

        /// <summary>
        /// 根据位置获取城市
        /// </summary>
        public CityData GetCityAt(Vector3Int location)
        {
            cityLocationMap.TryGetValue(location, out CityData city);
            return city;
        }

        /// <summary>
        /// 根据ID获取城市
        /// </summary>
        public CityData GetCityById(int cityId)
        {
            cityIdMap.TryGetValue(cityId, out CityData city);
            return city;
        }

        /// <summary>
        /// 检查位置是否有城市
        /// </summary>
        public bool HasCityAt(Vector3Int location)
        {
            return cityLocationMap.ContainsKey(location);
        }

        /// <summary>
        /// 获取指定国家的所有城市
        /// </summary>
        public List<CityData> GetCitiesByNation(int nationId)
        {
            List<CityData> result = new List<CityData>();
            foreach (CityData city in allCities)
            {
                if (city != null && city.ownerNationId == nationId)
                {
                    result.Add(city);
                }
            }
            return result;
        }

        /// <summary>
        /// 转移城市所有权
        /// </summary>
        public void TransferCityOwnership(CityData city, int newOwnerId)
        {
            if (city == null) return;

            int oldOwnerId = city.ownerNationId;
            city.ownerNationId = newOwnerId;

            NationData oldOwner = TurnManager.instance?.GetNation(oldOwnerId);
            NationData newOwner = TurnManager.instance?.GetNation(newOwnerId);

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
