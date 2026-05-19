using System.Linq;
using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.EnemyAI.Strategic;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Operational
{
    /// <summary>
    /// 根据战略简报与态势生成战役计划（主攻、集结、防守切换）。
    /// </summary>
    public static class AiOperationalPlanner
    {
        public static AiOperationalPlan Draft(AiWorldSnapshot snapshot, AiStrategicBrief brief)
        {
            if (brief?.PrioritizedEnemyCities == null || brief.PrioritizedEnemyCities.Count == 0)
                return AiOperationalPlan.Empty();

            AiCityStrategicInfo primary = brief.PrioritizedEnemyCities[0];
            Vector3Int anchor = primary.CityLocation;

            bool defensive = snapshot?.Situation != null && snapshot.Situation.DefensiveThreatActive;
            Vector3Int? assembly = FindNearestOwnCityTo(snapshot.ActingNationId, anchor);

            return new AiOperationalPlan(
                true,
                anchor,
                primary,
                assembly,
                defensive);
        }

        private static Vector3Int? FindNearestOwnCityTo(int nationId, Vector3Int target)
        {
            if (CityManager.instance?.AllCities == null) return null;
            CityData best = null;
            int bestDist = int.MaxValue;
            foreach (CityData c in CityManager.instance.AllCities)
            {
                if (c == null || c.ownerNationId != nationId) continue;
                int d = HexGridUtils.GetHexDistance(c.cityLocation, target);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best?.cityLocation;
        }
    }
}
