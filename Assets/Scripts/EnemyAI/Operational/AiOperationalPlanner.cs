using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.EnemyAI.Strategic;

namespace GlobalConqueror.EnemyAI.Operational
{
    /// <summary>
    /// 根据战略简报生成战役占位计划；当前返回 <see cref="AiOperationalPlan.Empty"/>。
    /// </summary>
    public static class AiOperationalPlanner
    {
        public static AiOperationalPlan Draft(AiWorldSnapshot _, AiStrategicBrief brief)
        {
            if (brief?.PrioritizedEnemyCities == null || brief.PrioritizedEnemyCities.Count == 0)
                return AiOperationalPlan.Empty();

            // 占位：以优先级最高的敌方城格作为「主攻锚点」，供后续路径/集结逻辑使用。
            AiCityStrategicInfo top = brief.PrioritizedEnemyCities[0];
            return new AiOperationalPlan(true, top.CityLocation);
        }
    }
}
