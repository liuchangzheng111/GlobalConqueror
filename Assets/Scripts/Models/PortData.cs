using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 港口数据模型
    /// </summary>
    [System.Serializable]
    public class PortData
    {
        public int portId;
        public string portName;

        public int PortGoldProduced => portLevel * 15;
        public int PortIndustryProduced => portLevel * 5;

        public Vector3Int portLocation;

        public int ownerNationId;
        public int portLevel;

        public PortData(int id, string name, Vector3Int location, int ownerId, int level)
        {
            portId = id;
            portName = name;
            portLocation = location;
            ownerNationId = ownerId;
            portLevel = level;
        }

        public override string ToString()
        {
            int level = portLevel;
            return $"港口ID: {portId} | 名称: {portName} | 等级: {level} | 所属国家: {ownerNationId}\n" +
                   $"资源产出: 金币{PortGoldProduced} | 工业{PortIndustryProduced}" +
                   $"位置: {portLocation}";
        }

        public string GetPortLevelString()
        {
            string result = "";
            switch (portLevel)
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
                default:
                    Debug.LogWarning("PortView: 港口等级错误！");
                    result = "";
                    break;
            }
            return result;
        }
    }
}
