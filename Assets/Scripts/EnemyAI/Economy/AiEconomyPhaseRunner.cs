using System;
using System.Collections;
using GlobalConqueror.EnemyAI;

namespace GlobalConqueror.EnemyAI.Economy
{
    /// <summary>
    /// AI 经济阶段：造兵/升防/空军等。当前为空实现，仅占位与管线挂钩。
    /// </summary>
    public static class AiEconomyPhaseRunner
    {
        /// <summary>预留：后续在此调用 <see cref="Managers.UnitManager.TryPurchaseUnit"/> 等 API。</summary>
        public static IEnumerator Run(AiNationTurnContext context, Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null)
                yield break;
            if (shouldContinue != null && !shouldContinue())
                yield break;

            yield return null;
        }
    }
}
