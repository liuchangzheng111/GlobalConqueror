using System;
using System.Collections;
using GlobalConqueror.EnemyAI;

namespace GlobalConqueror.EnemyAI.Economy
{
    /// <summary>
    /// AI 经济阶段：购兵 → 防空/堡垒 → 空军任务（战术阶段由 <see cref="AiNationTurnPipeline"/> 接续）。
    /// </summary>
    public static class AiEconomyPhaseRunner
    {
        public const float DefaultPauseAfterActionSeconds = 0.35f;

        public static IEnumerator Run(AiNationTurnContext context, Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null)
                yield break;
            if (shouldContinue != null && !shouldContinue())
                yield break;

            yield return AiEconomyPurchaseExecutor.CoExecutePurchases(
                context,
                DefaultPauseAfterActionSeconds,
                shouldContinue);

            if (shouldContinue != null && !shouldContinue())
                yield break;

            yield return AiEconomyDefenseExecutor.CoExecuteDefense(
                context,
                DefaultPauseAfterActionSeconds,
                shouldContinue);

            if (shouldContinue != null && !shouldContinue())
                yield break;

            yield return AiEconomyAirExecutor.CoExecuteAirMissions(
                context,
                DefaultPauseAfterActionSeconds,
                shouldContinue);
        }
    }
}
