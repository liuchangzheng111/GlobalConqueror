using UnityEngine;

namespace GlobalConqueror.EnemyAI.Operational
{
    /// <summary>
    /// 战役层计划占位：主攻轴、次要牵制等，后续由运营层填充并由战术层执行。
    /// </summary>
    public sealed class AiOperationalPlan
    {
        public bool HasDesignatedMainPush { get; }
        public Vector3Int? MainPushAnchor { get; }

        public AiOperationalPlan(bool hasDesignatedMainPush, Vector3Int? mainPushAnchor)
        {
            HasDesignatedMainPush = hasDesignatedMainPush;
            MainPushAnchor = mainPushAnchor;
        }

        public static AiOperationalPlan Empty() => new(false, null);
    }
}
