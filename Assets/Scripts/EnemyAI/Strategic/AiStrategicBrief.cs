using System.Collections.Generic;
using GlobalConqueror.EnemyAI.Core;

namespace GlobalConqueror.EnemyAI.Strategic
{
    /// <summary>
    /// 战略层输出：主攻敌国、建议攻城顺序等，供运营/经济/战术层消费。
    /// </summary>
    public sealed class AiStrategicBrief
    {
        /// <summary>主攻敌国 nationId；-1 表示未确定。</summary>
        public int PrimaryEnemyNationId { get; }

        /// <summary>按优先级排序的敌方城市（仅 owner 为敌且非战败过滤在构建时完成）。</summary>
        public IReadOnlyList<AiCityStrategicInfo> PrioritizedEnemyCities { get; }

        /// <summary>供调试或 UI 展示。</summary>
        public string DebugSummary { get; }

        public AiStrategicBrief(
            int primaryEnemyNationId,
            IReadOnlyList<AiCityStrategicInfo> prioritizedEnemyCities,
            string debugSummary)
        {
            PrimaryEnemyNationId = primaryEnemyNationId;
            PrioritizedEnemyCities = prioritizedEnemyCities;
            DebugSummary = debugSummary ?? "";
        }

        public static AiStrategicBrief Empty(int actingNationId) =>
            new(-1, new List<AiCityStrategicInfo>(), $"acting={actingNationId}, no data");
    }
}
