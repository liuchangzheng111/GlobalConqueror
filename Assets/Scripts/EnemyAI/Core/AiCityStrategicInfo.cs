using UnityEngine;

namespace GlobalConqueror.EnemyAI.Core
{
    /// <summary>
    /// 单座城市在战略层的只读摘要（回合内构建一次，不随战斗即时变化）。
    /// </summary>
    public readonly struct AiCityStrategicInfo
    {
        public readonly int CityId;
        public readonly string CityName;
        public readonly int OwnerNationId;
        public readonly Vector3Int CityLocation;
        /// <summary>粗略经济权重（金+工+科），用于排序攻城优先级。</summary>
        public readonly int IncomeWeight;
        /// <summary>是否该国配置的首都（<see cref="NationData.capital"/> 与城名一致）。</summary>
        public readonly bool IsRecordedCapital;

        public AiCityStrategicInfo(
            int cityId,
            string cityName,
            int ownerNationId,
            Vector3Int cityLocation,
            int incomeWeight,
            bool isRecordedCapital)
        {
            CityId = cityId;
            CityName = cityName ?? "";
            OwnerNationId = ownerNationId;
            CityLocation = cityLocation;
            IncomeWeight = incomeWeight;
            IsRecordedCapital = isRecordedCapital;
        }
    }
}
