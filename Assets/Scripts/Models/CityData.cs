using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 城市数据模型
    /// </summary>
    [System.Serializable]
    public class CityData
    {
        public int cityId;
        public string cityName;

        public int cityGoldProduced => (cityKindsLevel.cityLevel + 1) * 10;
        public int cityIndustryProduced => (cityKindsLevel.industryLevel) * 5;
        public int cityScienceProduced => (cityKindsLevel.scienceLevel) * 2;

        public Tilemap cityTiles;
        public Vector3Int cityLocation;

        public int ownerNationId; 
        public CityKindsLevel cityKindsLevel;

        public CityData(int id, string name, Tilemap tiles, Vector3Int location, int ownerId, CityKindsLevel level)
        {
            cityId = id;
            cityName = name;
            cityTiles = tiles;
            cityLocation = location;
            ownerNationId = ownerId;
            cityKindsLevel = level;
        }

        public override string ToString()
        {
            int level = cityKindsLevel != null ? cityKindsLevel.cityLevel : 1;
            return $"城市ID: {cityId} | 名称: {cityName} | 等级: {level} | 所属国家: {ownerNationId}\n" +
                   $"资源产出: 金币{cityGoldProduced} | 工业{cityIndustryProduced} | 科技{cityScienceProduced}\n" +
                   $"位置: {cityLocation}";
        }
    }

    /// <summary>
    /// 城市等级
    /// </summary>
    [System.Serializable]
    public class CityKindsLevel
    {
        public int cityLevel = 1;
        public int industryLevel = 1;
        public int airportLevel = 1;
        public int scienceLevel = 1;
        public int supplyLevel = 1;

        public CityKindsLevel(int level, int industry, int airport, int science, int supply)
        {
            cityLevel = level;
            industryLevel = industry;
            airportLevel = airport;
            scienceLevel = science;
            supplyLevel = supply;
        }

        public string GetCityLevelString()
        {
            string result = "";
            switch (cityLevel)
            {
                case 1:
                    result = "I";
                    break;
                case 2:
                    result = "II";
                    break;
                case 3:
                    result = "III";
                    break;
                case 4:
                    result = "IV";
                    break;
                case 5:
                    result = "V";
                    break;
                default:
                    Debug.LogWarning("CityView: 城市等级错误！");
                    result = "";
                    break;
            }
            return result;
        }
    }
}
