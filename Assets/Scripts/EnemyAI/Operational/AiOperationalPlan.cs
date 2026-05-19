using GlobalConqueror.EnemyAI.Core;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Operational
{
    /// <summary>
    /// 战役层计划：主攻目标、集结城、防守姿态（文档 §七.层2、层5）。
    /// </summary>
    public sealed class AiOperationalPlan
    {
        public bool HasDesignatedMainPush { get; }
        public Vector3Int? MainPushAnchor { get; }
        public AiCityStrategicInfo? PrimaryTargetCity { get; }
        /// <summary>距主攻目标最近的己方城市，用于集结购兵。</summary>
        public Vector3Int? AssemblyCityLocation { get; }
        /// <summary>敌军威胁首都/前沿时，本回合以防守为先。</summary>
        public bool PreferDefensivePosture { get; }

        public AiOperationalPlan(
            bool hasDesignatedMainPush,
            Vector3Int? mainPushAnchor,
            AiCityStrategicInfo? primaryTargetCity,
            Vector3Int? assemblyCityLocation,
            bool preferDefensivePosture)
        {
            HasDesignatedMainPush = hasDesignatedMainPush;
            MainPushAnchor = mainPushAnchor;
            PrimaryTargetCity = primaryTargetCity;
            AssemblyCityLocation = assemblyCityLocation;
            PreferDefensivePosture = preferDefensivePosture;
        }

        public static AiOperationalPlan Empty() => new(false, null, null, null, false);
    }
}
