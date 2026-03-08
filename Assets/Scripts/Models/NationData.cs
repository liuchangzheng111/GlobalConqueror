using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 国家数据模型
    /// </summary>
    #nullable enable
    [System.Serializable]
    public class NationData
    {
        public int nationId;
        public string nationName;
        public Color nationColor;
        
        public int gold;
        public int industry;
        public int science;
        
        public List<CityData> ownedCity;
        public CityData? capital;
        
        public bool isPlayer;
        public bool isDefeated;

        public NationData(
            int id, 
            string name, 
            Color color, 
            List<CityData> cities,
            int _gold = 1000,
            int _industry = 200,
            int _science = 50,
            CityData? cap = null, 
            bool player = false)
        {
            nationId = id;
            nationName = name;
            nationColor = color;
            ownedCity = cities;           
            
            gold = _gold;
            industry = _industry;
            science = _science;   
            
            capital = cap;
            isPlayer = player;
            isDefeated = false;
        }

        public void AddCity(CityData city)
        {
            if (city == null) return;
            
            if (!ownedCity.Contains(city))
            {
                ownedCity.Add(city);
                city.ownerNationId = nationId;
            }
        }

        public void RemoveCity(CityData city)
        {
            if (city == null) return;
            
            ownedCity.Remove(city);
            
            if (capital == city)
            {
                capital = null;
            }
        }

        public bool HasCity(CityData city)
        {
            return city != null && ownedCity.Contains(city);
        }

        public CityData GetCityAt(Vector3Int location)
        {
            return ownedCity.Find(c => c.cityLocation == location);
        }

        public bool HasCityAt(Vector3Int location)
        {
            return GetCityAt(location) != null;
        }
    }
    #nullable disable
}
