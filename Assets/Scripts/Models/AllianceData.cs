using System.Collections.Generic;
using UnityEngine;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 开局配置的联盟：本条目内的国家互为盟友。
    /// </summary>
    [System.Serializable]
    public class AllianceData
    {
        [Tooltip("联盟名称")]
        public string allianceName;

        [Tooltip("拖入 NationManager 国家列表中的同一批国家引用（以 nationId 登记）。")]
        public List<int> nationsId = new();

        public AllianceData(string name, List<int> nationsId)
        {
            allianceName = name;
            this.nationsId = nationsId;
        }
    }
}
