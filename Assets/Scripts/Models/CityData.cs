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

        public int cityGoldProduced = 50;
        public int cityIndustryProduced = 10;
        public int cityScienceProduced = 2;

        public Tilemap cityTiles;
        public Vector3Int cityLocation;

        public int ownerNationId; 
        public CityLevel cityLevel;

        public CityData(int id, string name, Tilemap tiles, Vector3Int location, int ownerId)
        {
            cityId = id;
            cityName = name;
            cityTiles = tiles;
            cityLocation = location;
            ownerNationId = ownerId;
            cityLevel = new CityLevel();
        }

        public override string ToString()
        {
            int level = cityLevel != null ? cityLevel.level : 1;
            return $"城市ID: {cityId} | 名称: {cityName} | 等级: {level} | 所属国家: {ownerNationId}\n" +
                   $"资源产出: 金币{cityGoldProduced} | 工业{cityIndustryProduced} | 科技{cityScienceProduced}\n" +
                   $"位置: {cityLocation}";
        }
    }

    /// <summary>
    /// 城市等级
    /// </summary>
    [System.Serializable]
    public class CityLevel 
    {
        public int level = 1;
    }
}
