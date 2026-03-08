using System.Collections.Generic;
using UnityEngine;
using GlobalConqueror.Models;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 回合管理器 - 管理回合制游戏的核心循环
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager instance;

        [Header("回合设置")]
        [SerializeField] private int maxTurns = 100;
        [SerializeField] private bool autoEndTurn = false;
        [SerializeField] private float autoEndTurnDelay = 2f;

        [Header("国家列表")]
        [SerializeField] private List<NationData> nations;

        private int currentTurn = 1;
        private int currentNationIndex = 0;
        private NationData currentNation;

        public int CurrentTurn => currentTurn;
        public NationData CurrentNation => currentNation;
        public List<NationData> Nations => nations;

        public System.Action<int> OnTurnStart;
        public System.Action<int> OnTurnEnd;
        public System.Action<NationData> OnNationTurnStart;
        public System.Action<NationData> OnNationTurnEnd;
        public System.Action<NationData> OnNationDefeated;

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
            InitializeNations();
            StartTurn();
        }

        /// <summary>
        /// 初始化国家列表
        /// </summary>
        private void InitializeNations()
        {
            if (nations == null)
            {
                nations = new List<NationData>();
            }

            if (nations.Count == 0)
            {
                List<CityData> playerCities = new List<CityData>();
                List<CityData> aiCities = new List<CityData>();
                
                nations.Add(new NationData(0, "玩家", Color.blue, playerCities, 1000, 200, 50, null, true));
                nations.Add(new NationData(1, "AI国家1", Color.red, aiCities, 1000, 200, 50, null, false));
            }
            
            currentNationIndex = 0;
            if (nations.Count > 0)
            {
                currentNation = nations[0];
            }
        }

        /// <summary>
        /// 开始回合
        /// </summary>
        public void StartTurn()
        {
            if (currentTurn > maxTurns)
            {
                Debug.Log("达到最大回合数，游戏结束");
                return;
            }

            SkipDefeatedNations();

            if (currentNationIndex >= nations.Count)
            {
                currentNationIndex = 0;
                currentTurn++;
                OnTurnEnd?.Invoke(currentTurn - 1);
            }

            if (nations.Count == 0)
            {
                Debug.LogWarning("没有可用的国家，游戏结束");
                return;
            }

            if (currentNationIndex >= nations.Count)
            {
                Debug.LogWarning("所有国家都已失败，游戏结束");
                return;
            }

            currentNation = nations[currentNationIndex];
            
            OnTurnStart?.Invoke(currentTurn);
            OnNationTurnStart?.Invoke(currentNation);

            ProcessNationTurnStart(currentNation);

            if (autoEndTurn && !currentNation.isPlayer)
            {
                Invoke(nameof(EndTurn), autoEndTurnDelay);
            }
        }

        /// <summary>
        /// 跳过已失败的国家
        /// </summary>
        private void SkipDefeatedNations()
        {
            int attempts = 0;
            while (currentNationIndex < nations.Count && attempts < nations.Count)
            {
                if (nations[currentNationIndex].isDefeated)
                {
                    currentNationIndex++;
                    attempts++;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 结束当前回合
        /// </summary>
        public void EndTurn()
        {
            if (currentNation == null) return;

            ProcessNationTurnEnd(currentNation);
            OnNationTurnEnd?.Invoke(currentNation);

            currentNationIndex++;
            if (currentNationIndex >= nations.Count)
            {
                currentNationIndex = 0;
                currentTurn++;
                OnTurnEnd?.Invoke(currentTurn - 1);
            }

            StartTurn();
        }

        /// <summary>
        /// 处理国家回合开始
        /// </summary>
        private void ProcessNationTurnStart(NationData nation)
        {
            ProcessResourceProduction(nation);
        }

        /// <summary>
        /// 处理国家回合结束
        /// </summary>
        private void ProcessNationTurnEnd(NationData nation)
        {
            CheckNationDefeat(nation);
        }

        /// <summary>
        /// 处理资源生产
        /// </summary>
        private void ProcessResourceProduction(NationData nation)
        {
            int goldProduction = 0;
            int industryProduction = 0;
            int scienceProduction = 0;

            if (nation.ownedCity == null || nation.ownedCity.Count == 0)
            {
                Debug.Log($"{nation.nationName} 回合开始 - 无城市，无资源产出");
                return;
            }

            foreach (CityData city in nation.ownedCity)
            {
                if (city == null) continue;

                goldProduction += city.cityGoldProduced;
                industryProduction += city.cityIndustryProduced;
                scienceProduction += city.cityScienceProduced;

                if (city == nation.capital)
                {
                    goldProduction += 50;
                    industryProduction += 30;
                    scienceProduction += 20;
                }
            }

            nation.gold += goldProduction;
            nation.industry += industryProduction;
            nation.science += scienceProduction;

            Debug.Log($"{nation.nationName} 回合开始 - 资源产出: 金币+{goldProduction}, 工业+{industryProduction}, 科技+{scienceProduction}");
        }

        /// <summary>
        /// 检查国家是否失败（失去所有城市或首都）
        /// </summary>
        private void CheckNationDefeat(NationData nation)
        {
            if (nation.isDefeated) return;

            bool hasNoCities = nation.ownedCity == null || nation.ownedCity.Count == 0;
            bool lostCapital = nation.capital == null || !nation.ownedCity.Contains(nation.capital);

            if (hasNoCities || lostCapital)
            {
                nation.isDefeated = true;
                Debug.Log($"{nation.nationName} 已被击败！");
                
                OnNationDefeated?.Invoke(nation);
            }
        }


        /// <summary>
        /// 添加国家
        /// </summary>
        public void AddNation(NationData nation)
        {
            if (!nations.Contains(nation))
            {
                nations.Add(nation);
            }
        }

        /// <summary>
        /// 移除国家
        /// </summary>
        public void RemoveNation(NationData nation)
        {
            nations.Remove(nation);
            if (currentNationIndex >= nations.Count)
            {
                currentNationIndex = 0;
            }
        }

        /// <summary>
        /// 获取指定ID的国家
        /// </summary>
        public NationData GetNation(int nationId)
        {
            return nations.Find(n => n.nationId == nationId);
        }
    }
}
