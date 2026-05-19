using System;
using System.Collections;
using GlobalConqueror.EnemyAI.Tactical;
using GlobalConqueror.Models;

namespace GlobalConqueror.EnemyAI
{
    /// <summary>
    /// 野战入口：委托 <see cref="AiTacticalExecutor"/>（寻路、占城、防守、火炮优先）。
    /// </summary>
    public static class SimpleNationSkirmishAi
    {
        /// <summary>带完整上下文的战术回合（由 <see cref="AiNationTurnPipeline"/> 调用）。</summary>
        public static IEnumerator RunTacticalTurn(
            AiNationTurnContext context,
            float pauseBetweenActions,
            Func<bool> shouldContinue)
        {
            yield return AiTacticalExecutor.Run(context, pauseBetweenActions, shouldContinue);
        }
    }
}
