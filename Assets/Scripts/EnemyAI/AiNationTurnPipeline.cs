using System;
using System.Collections;
using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.EnemyAI.Economy;
using GlobalConqueror.EnemyAI.Operational;
using GlobalConqueror.EnemyAI.Strategic;
using GlobalConqueror.Models;
using UnityEngine;

namespace GlobalConqueror.EnemyAI
{
    /// <summary>
    /// AI 回合总管线：快照 → 战略 → 战役计划 → 经济（购兵/防空/堡垒/空军）→ 野战战术。
    /// <see cref="Managers.NationManager"/> 仅依赖此入口，后续扩展阶段不改调度处。
    /// </summary>
    public static class AiNationTurnPipeline
    {
        /// <summary>
        /// 是否输出简报日志（可在编辑器中改为 true 调试）。
        /// </summary>
        public static bool LogStrategicBrief = false;

        public static IEnumerator RunFullAiTurn(
            NationData actingNation,
            float tacticalActionPauseSeconds,
            Func<bool> shouldContinue)
        {
            if (actingNation == null)
                yield break;

            AiWorldSnapshot snapshot = AiWorldSnapshot.Build(actingNation.nationId);
            AiStrategicBrief brief = AiStrategicAnalyzer.Analyze(snapshot, actingNation);
            AiOperationalPlan plan = AiOperationalPlanner.Draft(snapshot, brief);

            if (LogStrategicBrief)
                Debug.Log($"[AI] {actingNation.nationName}: {brief.DebugSummary}");

            var context = new AiNationTurnContext(actingNation, snapshot, brief, plan);

            yield return AiEconomyPhaseRunner.Run(context, shouldContinue);

            if (shouldContinue != null && !shouldContinue())
                yield break;

            yield return SimpleNationSkirmishAi.RunTacticalTurn(
                context,
                tacticalActionPauseSeconds,
                shouldContinue);
        }
    }
}
