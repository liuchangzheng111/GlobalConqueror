using System.Collections.Generic;
using UnityEngine;
using GlobalConqueror.Models;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 国家与回合管理器 - 管理回合制游戏的核心循环
    /// </summary>
    public class NationManager : MonoBehaviour
    {
        public static NationManager instance;

        [Header("回合设置")]
        [SerializeField] private int maxTurns = 100;

        [Header("国家列表")]
        [SerializeField] private List<NationData> nations;

        private Dictionary<string, NationData> nationsDic = new Dictionary<string, NationData>();
        private int currentTurn = 1;
        private int currentNationIndex = 0;
        private NationData currentNation;

        public int CurrentTurn => currentTurn;
        public NationData CurrentNation => currentNation;
        public List<NationData> Nations => nations;
        public Dictionary<string, NationData> NationsDic => nationsDic;

        public System.Action<int> OnTurnStart;
        public System.Action<int> OnTurnEnd;
        public System.Action<NationData> OnNationTurnStart;
        public System.Action<NationData> OnNationTurnEnd;
        public System.Action<NationData> OnNationDefeated;

        public bool isNationsInitialized { get; private set; } = false;

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
            // 等待 CityManager 初始化城市后再初始化国家和开始回合
            StartCoroutine(InitializeWhenReady());
            StartCoroutine(StartWhenReady());
        }

        /// <summary>
        /// 等待城市初始化完成后再初始化国家与回合逻辑，避免脚本执行顺序问题
        /// </summary>
        private System.Collections.IEnumerator InitializeWhenReady()
        {
            // 等待 CityManager 与城市 Tilemap 初始化完成
            while (CityManager.instance == null || !CityManager.instance.IsCityTilemapInitialized)
            {
                yield return null;
            }

            if (!isNationsInitialized)
            {
                InitializeNations();
                isNationsInitialized = true;
            }
        }

        /// <summary>
        /// 所有初始完后开始第一回合
        /// </summary>
        /// <returns></returns>
        private System.Collections.IEnumerator StartWhenReady()
        {
            while (UnitManager.instance == null || !UnitManager.instance.initialUnitsSpawned)
            {
                yield return null;
            }

            StartTurn();
        }

        /// <summary>
        /// 初始化国家列表和其所拥有的城市
        /// </summary>
        private void InitializeNations()
        {
            if (nations == null)
            {
                nations = new List<NationData>();
            }

            if (nations.Count == 0)
            {
                Debug.LogWarning("请在编辑器中初始化国家列表");
                return;
            }

            // 绑定其所拥有的城市，并赋予国家颜色，同时将该城市 Tilemap 上所有有瓦片的格子的地块归属设为该国
            foreach (var nation in nations)
            {
                foreach (var city in nation.ownedCitiesNames)
                {
                    CityData cityData = CityManager.instance.CitiesDic[city];
                    cityData.ownerNationId = nation.nationId;
                    cityData.cityTiles.color = nation.nationColor;

                    if (cityData.cityTiles != null)
                    {
                        BoundsInt bounds = cityData.cityTiles.cellBounds;
                        foreach (Vector3Int pos in bounds.allPositionsWithin)
                        {
                            if (cityData.cityTiles.GetTile(pos) == null)
                                continue;

                            Vector3 worldPos = cityData.cityTiles.CellToWorld(pos);
                            Vector3Int mapCell = MapManager.instance.Tilemap.WorldToCell(worldPos);

                            if (!MapManager.instance.IsCoordinateValid(mapCell))
                                continue;

                            MapManager.instance.SetTileOwner(mapCell, nation.nationId);
                        }
                    }
                }

                // 初始化nationsDic
                nationsDic.Add(nation.nationName, nation);
            }

            currentNationIndex = 0;
            currentNation = nations[0];
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

            // 跳过已战败的国家
            while (currentNationIndex < nations.Count)
            {
                if (nations[currentNationIndex].isDefeated)
                {
                    currentNationIndex++;
                }
                else
                {
                    break;
                }
            }

            // 如果一轮结束，开始下一轮
            if (currentNationIndex >= nations.Count)
            {
                currentNationIndex = 0;
                currentTurn++;
                OnTurnStart?.Invoke(currentTurn);
            }          

            currentNation = nations[currentNationIndex];

            ProcessResourceProduction(currentNation);

            OnNationTurnStart?.Invoke(currentNation);

            ProcessNationTurnStart(currentNation);
        }

        /// <summary>
        /// 结束当前回合
        /// </summary>
        public void EndTurn()
        {
            if (currentNation == null)
            {
                Debug.LogError("NationManager: 当前国家为空，请检查问题！");
                return;
            }

            ProcessNationTurnEnd(currentNation);
            OnNationTurnEnd?.Invoke(currentNation);

            currentNationIndex++;

            StartTurn();
        }

        /// <summary>
        /// 处理国家回合开始
        /// </summary>
        private void ProcessNationTurnStart(NationData nation)
        {
            
        }

        /// <summary>
        /// 处理国家回合结束
        /// </summary>
        private void ProcessNationTurnEnd(NationData nation)
        {

        }

        /// <summary>
        /// 处理国家回合开始准备的资源生产
        /// </summary>
        private void ProcessResourceProduction(NationData nation)
        {
            int goldProduction = 0;
            int industryProduction = 0;
            int scienceProduction = 0;

            if (nation.ownedCitiesNames == null || nation.ownedCitiesNames.Count == 0)
            {
                Debug.Log($"{nation.nationName} 回合开始 - 无城市，无资源产出");
                return;
            }

            foreach (var city in nation.ownedCitiesNames)
            {
                if (!CityManager.instance.CitiesDic.ContainsKey(city)) continue;

                CityData cityData = CityManager.instance.CitiesDic[city];
                goldProduction += cityData.cityGoldProduced;
                industryProduction += cityData.cityIndustryProduced;
                scienceProduction += cityData.cityScienceProduced;

                if (city == nation.capital)
                {
                    goldProduction += 50;
                    industryProduction += 20;
                    scienceProduction += 5;
                }
            }

            nation.gold += goldProduction;
            nation.industry += industryProduction;
            nation.science += scienceProduction;

            Debug.Log($"{nation.nationName} 回合开始 - 资源产出: 金币+{goldProduction}, 工业+{industryProduction}, 科技+{scienceProduction}");
        }

        /// <summary>
        /// 检查国家是否失败（失去所有城市）
        /// </summary>
        private void CheckNationDefeat(NationData nation)
        {
            if (nation.isDefeated) return;

            bool hasNoCities = nation.ownedCitiesNames.Count == 0;

            if (hasNoCities)
            {
                nation.isDefeated = true;
                Debug.Log($"{nation.nationName} 已被击败！");
                
                OnNationDefeated?.Invoke(nation);
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
