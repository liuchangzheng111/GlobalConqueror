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
        
        public List<Tilemap> ownedCities;
        public string? capital;      // TOEXTEND:

        [Tooltip("勾选：该国回合由本地人类操作；不勾选：由 NationManager 配置的 AI 接手")]
        public bool isPlayer = false;
        public bool isDefeated = false;

        public NationData(
            int id, 
            string name, 
            Sprite flag,
            Color color, 
            List<Tilemap> cities,
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
            ownedCities = cities;           
            
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

            if (!ownedCities.Contains(city.cityTiles))
            {
                ownedCities.Add(city.cityTiles);
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

            if (ownedCities.Contains(city.cityTiles))
            {
                ownedCities.Remove(city.cityTiles);
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
