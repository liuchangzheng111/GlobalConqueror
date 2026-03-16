using GlobalConqueror.Managers;
using System.Collections.Generic;
using System.Linq;
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
        public Sprite nationFlag;
        public Color nationColor;
        
        public int gold = 500;
        public int industry = 100;
        public int science = 25;
        
        public List<string> ownedCitiesNames;
        public string? capital;      // TOEXTEND:
        
        public bool isPlayer = false;
        public bool isDefeated = false;

        public NationData(
            int id, 
            string name, 
            Sprite flag,
            Color color, 
            List<string> cities,
            int _gold = 1000,
            int _industry = 200,
            int _science = 50,
            string? cap = null, 
            bool player = false)
        {
            nationId = id;
            nationName = name;
            nationFlag = flag;
            nationColor = color;
            ownedCitiesNames = cities;           
            
            gold = _gold;
            industry = _industry;
            science = _science;   
            
            capital = cap;
            isPlayer = player;
            isDefeated = false;
        }

        /// <summary>
        /// 添加城市
        /// </summary>
        /// <param name="city"></param>
        public void AddCity(CityData city)
        {
            if (city == null)
            {
                Debug.LogError("NationData: 不存在此城市！");
                return;
            }

            if (!ownedCitiesNames.Contains(city.cityName))
            {
                ownedCitiesNames.Add(city.cityName);
                city.cityTiles.color = nationColor;
                city.ownerNationId = nationId;
            }
        }

        /// <summary>
        /// 移除城市
        /// </summary>
        /// <param name="city"></param>
        public void RemoveCity(CityData city)
        {
            if (city == null)
            {
                Debug.LogError("NationData: 不存在此城市！");
                return;
            }

            if (ownedCitiesNames.Contains(city.cityName))
            {
                ownedCitiesNames.Remove(city.cityName);
            }
            else
            {
                Debug.LogWarning("NationData: 要删除的城市不在此国家！");
                return;
            }
        }
    }
    #nullable disable
}
