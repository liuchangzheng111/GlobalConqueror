using System.Collections;
using System.Collections.Generic;
using GlobalConqueror.Models;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 联盟管理：开局从配置表建立 nationId → 运行时阵营 id，供移动/战斗等判断盟友关系。
    /// </summary>
    public class AllianceManager : MonoBehaviour
    {
        /// <summary>
        /// 未出现在任何联盟里的国家，其「单独阵营」id = 本值 + nationId；须大于任意自动分配的联盟编号。
        /// </summary>
        public const int SoloAllianceIdBase = 1_000_000;

        public static AllianceManager instance;

        [Header("从编辑器指定的联盟列表（开局指定；未列入的国家各自为单独阵营）")]
        [SerializeField] private List<AllianceData> alliancesFromEditor = new();

        private List<AllianceData> alliances = new();

        private readonly Dictionary<int, AllianceData> _nationIdToAlliance = new();

        public List<AllianceData> Alliances => alliances;

        public Dictionary<int, AllianceData> NationIdToAlliance => _nationIdToAlliance;

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
            StartCoroutine(BuildAllianceMapWhenReady());
        }

        private IEnumerator BuildAllianceMapWhenReady()
        {
            while (NationManager.instance == null || !NationManager.instance.IsNationsInitialized)
                yield return null;

            BuildAllianceMap();
        }

        private void BuildAllianceMap()
        {
            _nationIdToAlliance.Clear();
            alliances.Clear();
            alliancesFromEditor ??= new List<AllianceData>();

            foreach (AllianceData alliance in alliancesFromEditor)
            {
                if (alliance == null || alliance.nationsId == null)
                    continue;      

                bool anyNationInThisRow = false;
                foreach (int n in alliance.nationsId)
                {
                    NationData nation = NationManager.instance.GetNation(n);
                    if (nation == null)
                        continue;

                    anyNationInThisRow = true;

                    if (_nationIdToAlliance.ContainsKey(nation.nationId))
                    {
                        Debug.LogWarning(
                            $"AllianceManager: 国家 {nation.nationId}（{nation.nationName}）出现在多个联盟配置中，后配置会覆盖先配置");
                    }

                    _nationIdToAlliance[nation.nationId] = alliance;
                }

                if (anyNationInThisRow)
                    alliances.Add(alliance);
            }

            // 绑定剩下的单独国家
            if (NationManager.instance != null)
            {
                foreach (var item in NationManager.instance.Nations)
                {
                    if (!_nationIdToAlliance.ContainsKey(item.nationId))
                    {
                        AllianceData allianceData = new("", new() { item.nationId });
                        alliances.Add(allianceData);
                        _nationIdToAlliance[item.nationId] = allianceData;
                    }
                }
            }

            Debug.Log($"AllianceManager: 已建立联盟映射，共 {_nationIdToAlliance.Count} 个国家条目，联盟组数为 {alliances.Count}。");
        }
         
        /// <summary>
        /// 运行时阵营 id：在联盟表中的国家用自动分配的组号；否则为 <see cref="SoloAllianceIdBase"/> + nationId。
        /// </summary>
        public AllianceData GetAllianceForNation(int nationId)
        {
            if (_nationIdToAlliance.TryGetValue(nationId, out AllianceData aid))
                return aid;
            return null;
        }

        /// <summary>
        /// 两国是否同阵营（同一 nation 视为盟友；无 AllianceManager 时仅同 id 为盟）。
        /// </summary>
        public static bool AreAllied(int nationIdA, int nationIdB)
        {
            if (nationIdA == nationIdB)
                return true;
            if (instance == null)
                return false;
            return instance.GetAllianceForNation(nationIdA) == instance.GetAllianceForNation(nationIdB);
        }
    }
}
