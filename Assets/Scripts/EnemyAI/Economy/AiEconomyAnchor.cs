using GlobalConqueror.EnemyAI.Operational;
using GlobalConqueror.EnemyAI.Strategic;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Economy
{
    /// <summary>
    /// 经济/建设阶段共用的战略锚点（主攻敌城格等）。
    /// </summary>
    internal static class AiEconomyAnchor
    {
        public static Vector3Int? Resolve(AiNationTurnContext context)
        {
            if (context?.OperationalPlan is { HasDesignatedMainPush: true, MainPushAnchor: { } p })
                return p;
            if (context?.StrategicBrief?.PrioritizedEnemyCities != null &&
                context.StrategicBrief.PrioritizedEnemyCities.Count > 0)
                return context.StrategicBrief.PrioritizedEnemyCities[0].CityLocation;
            return null;
        }

        /// <summary>购兵/集结排序用：防守时靠近需增援城，否则靠近集结城或主攻锚点。</summary>
        public static Vector3Int? ResolvePurchaseSortOrigin(AiNationTurnContext context)
        {
            if (context?.OperationalPlan?.PreferDefensivePosture == true &&
                context.Snapshot?.Situation?.PriorityDefendCell is { } defend)
                return defend;
            if (context?.OperationalPlan?.AssemblyCityLocation is { } assembly)
                return assembly;
            return Resolve(context);
        }

        public static int Distance(Vector3Int cell, Vector3Int? anchor)
        {
            if (!anchor.HasValue) return 0;
            return HexGridUtils.GetHexDistance(cell, anchor.Value);
        }
    }
}
