using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalConqueror.Models;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System;

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

        [Header("单机 AI（isPlayer=false 的国家由 AI 执行回合）")]
        [SerializeField] private bool autoEndTurnForAiNations = true;
        [SerializeField] private float aiEndTurnDelaySeconds = 0.75f;
        [Tooltip("AI 每次移动/攻击之间的停顿（秒），便于观察")]
        [SerializeField] private float aiActionPauseSeconds = 0.35f;

        private readonly Dictionary<string, NationData> nationsDic = new();
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

        public bool IsNationsInitialized { get; private set; } = false;

        private Coroutine _aiAutoEndTurnRoutine;

        /// <summary>
        /// 当前行动方是否为本地人类：读取编辑器里为该国配置的 <see cref="NationData.isPlayer"/>。
        /// </summary>
        public bool IsLocalHumanTurn()
        {
            return currentNation != null && currentNation.isPlayer;
        }

        /// <summary>
        /// 当前行动方是否由 AI 接手：与 <see cref="NationData.isPlayer"/> 相反（未勾选 isPlayer 即为 AI 国）。
        /// </summary>
        public bool IsCurrentNationAiControlled()
        {
            return currentNation != null && !currentNation.isPlayer;
        }

        /// <summary>
        /// 读取某国在场景配置中是否为人类玩家国（仅看 <see cref="NationData.isPlayer"/>，不做推断）。
        /// </summary>
        public static bool IsHumanPlayerNation(NationData nation)
        {
            return nation != null && nation.isPlayer;
        }

        /// <summary>
        /// 读取某国是否应由 AI 接手（<see cref="NationData.isPlayer"/> 为 false）。
        /// </summary>
        public static bool IsAiControlledNation(NationData nation)
        {
            return nation != null && !nation.isPlayer;
        }

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

        private void OnDestroy()
        {
            if (_aiAutoEndTurnRoutine != null)
            {
                StopCoroutine(_aiAutoEndTurnRoutine);
                _aiAutoEndTurnRoutine = null;
            }
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

            if (!IsNationsInitialized)
            {
                InitializeNations();
                IsNationsInitialized = true;
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
            nations ??= new List<NationData>();

            if (nations.Count == 0)
            {
                Debug.LogWarning("请在编辑器中初始化国家列表");
                return;
            }

            // 绑定其所拥有的城市，并赋予国家颜色，同时将该城市 Tilemap 上所有有瓦片的格子的地块归属设为该国
            foreach (var nation in nations)
            {
                foreach (var city in nation.ownedCities)
                {
                    CityData cityData = CityManager.instance.CitiesDic[city.name];
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

            LogPlayerNationConfigWarnings();

            currentNationIndex = 0;
            currentNation = nations[0];
        }

        /// <summary>
        /// 根据编辑器里配置的 <see cref="NationData.isPlayer"/> 做校验，不修改任何配置。
        /// </summary>
        private void LogPlayerNationConfigWarnings()
        {
            if (nations == null || nations.Count == 0) return;

            int playerCount = 0;
            foreach (var n in nations)
            {
                if (n != null && n.isPlayer) playerCount++;
            }

            if (playerCount == 0)
            {
                Debug.LogWarning("NationManager: 国家列表中没有任何 isPlayer=true 的国家，本局将没有本地人类操作回合（若需人类操作，请在编辑器中勾选一国 isPlayer）。");
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

            ScheduleAiAutoEndTurnIfNeeded();
        }

        /// <summary>
        /// 如果需要，则调度 AI 自动结束回合
        /// </summary>
        private void ScheduleAiAutoEndTurnIfNeeded()
        {
            if (!autoEndTurnForAiNations || currentNation == null || IsHumanPlayerNation(currentNation))
                return;

            if (_aiAutoEndTurnRoutine != null)
                StopCoroutine(_aiAutoEndTurnRoutine);
            _aiAutoEndTurnRoutine = StartCoroutine(CoAutoEndTurnForCurrentAiNation());
        }

        /// <summary>
        /// 自动结束当前 AI 回合
        /// </summary>
        /// <returns></returns>
        private IEnumerator CoAutoEndTurnForCurrentAiNation()
        {
            yield return new WaitForSeconds(aiEndTurnDelaySeconds);
            _aiAutoEndTurnRoutine = null;

            if (currentNation == null || IsHumanPlayerNation(currentNation))
                yield break;

            NationData actingNation = currentNation;

            yield return SimpleNationSkirmishAi.RunSimpleSkirmishTurn(
                actingNation,
                aiActionPauseSeconds,
                () => instance != null && currentNation == actingNation && !IsHumanPlayerNation(currentNation));

            if (currentNation == null || currentNation != actingNation || IsHumanPlayerNation(currentNation))
                yield break;

            EndTurn();
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
            if (nation is null)
            {
                throw new ArgumentNullException(nameof(nation));
            }
        }

        /// <summary>
        /// 处理国家回合结束
        /// </summary>
        private void ProcessNationTurnEnd(NationData nation)
        {
            if (nation is null)
            {
                throw new ArgumentNullException(nameof(nation));
            }
        }

        /// <summary>
        /// 处理国家回合开始准备的资源生产
        /// </summary>
        private void ProcessResourceProduction(NationData nation)
        {
            int goldProduction = 0;
            int industryProduction = 0;
            int scienceProduction = 0;

            if (nation.ownedCities == null || nation.ownedCities.Count == 0)
            {
                Debug.Log($"{nation.nationName} 回合开始 - 无城市，无资源产出");
                return;
            }

            foreach (var city in nation.ownedCities)
            {
                if (!CityManager.instance.CitiesDic.ContainsKey(city.name)) continue;

                CityData cityData = CityManager.instance.CitiesDic[city.name];
                goldProduction += cityData.CityGoldProduced;
                industryProduction += cityData.CityIndustryProduced;
                scienceProduction += cityData.CityScienceProduced;

                if (city.name == nation.capital)
                {
                    goldProduction += 50;
                    industryProduction += 20;
                    scienceProduction += 5;
                }
            }

            if (PortManager.instance != null && PortManager.instance.NationOwnPorts.TryGetValue(nation.nationName,out List<PortData> ports))
            {
                foreach (var port in ports)
                {
                    goldProduction += port.PortGoldProduced;
                    industryProduction += port.PortIndustryProduced;
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

            bool hasNoCities = nation.ownedCities.Count == 0;

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
