using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.EnemyAI.Operational;
using GlobalConqueror.EnemyAI.Strategic;
using GlobalConqueror.Models;

namespace GlobalConqueror.EnemyAI
{
    /// <summary>
    /// 单次 AI 回合的上下文：快照、战略简报、战役计划与行动国引用。
    /// </summary>
    public sealed class AiNationTurnContext
    {
        public NationData ActingNation { get; }
        public AiWorldSnapshot Snapshot { get; }
        public AiStrategicBrief StrategicBrief { get; }
        public AiOperationalPlan OperationalPlan { get; }

        public AiNationTurnContext(
            NationData actingNation,
            AiWorldSnapshot snapshot,
            AiStrategicBrief strategicBrief,
            AiOperationalPlan operationalPlan)
        {
            ActingNation = actingNation;
            Snapshot = snapshot;
            StrategicBrief = strategicBrief;
            OperationalPlan = operationalPlan;
        }
    }
}
