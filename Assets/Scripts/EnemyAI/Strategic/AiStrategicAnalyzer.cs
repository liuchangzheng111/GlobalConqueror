using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.EnemyAI.Core;

namespace GlobalConqueror.EnemyAI.Strategic
{
    /// <summary>
    /// 从 <see cref="AiWorldSnapshot"/> 生成 <see cref="AiStrategicBrief"/>。
    /// 当前为轻量启发式：优先玩家国，否则城数最多的敌国；城市按经济权重与首都标记排序。
    /// </summary>
    public static class AiStrategicAnalyzer
    {
        public static AiStrategicBrief Analyze(AiWorldSnapshot snapshot, NationData self)
        {
            if (snapshot == null || self == null)
                return AiStrategicBrief.Empty(self?.nationId ?? -1);

            int primaryEnemy = PickPrimaryEnemyNationId(snapshot);
            var enemyCities = new List<AiCityStrategicInfo>();

            foreach (AiCityStrategicInfo c in snapshot.Cities)
            {
                if (c.OwnerNationId == snapshot.ActingNationId) continue;
                if (!snapshot.EnemyNationIds.Contains(c.OwnerNationId)) continue;
                enemyCities.Add(c);
            }

            // 分数：经济 + 首都加成 + 格上无守军加成（鼓励空降/快占思路，后续战术层可用）
            IEnumerable<AiCityStrategicInfo> ordered = enemyCities
                .OrderByDescending(c => ScoreCity(c, snapshot, primaryEnemy));

            var list = ordered.ToList();
            var sb = new StringBuilder();
            sb.Append($"acting={snapshot.ActingNationId}, primaryEnemy={primaryEnemy}, turn={snapshot.CurrentGlobalTurn}, targets={list.Count}");
            return new AiStrategicBrief(primaryEnemy, list, sb.ToString());
        }

        private static int PickPrimaryEnemyNationId(AiWorldSnapshot snapshot)
        {
            if (NationManager.instance?.Nations == null || snapshot.EnemyNationIds.Count == 0)
                return snapshot.EnemyNationIds.Count > 0 ? snapshot.EnemyNationIds[0] : -1;

            foreach (NationData n in NationManager.instance.Nations)
            {
                if (n == null || n.isDefeated) continue;
                if (n.nationId == snapshot.ActingNationId) continue;
                if (n.isPlayer && snapshot.EnemyNationIds.Contains(n.nationId))
                    return n.nationId;
            }

            int bestId = snapshot.EnemyNationIds[0];
            int bestCities = -1;
            foreach (int id in snapshot.EnemyNationIds)
            {
                NationData n = NationManager.instance.GetNation(id);
                if (n == null) continue;
                int k = n.ownedCities != null ? n.ownedCities.Count : 0;
                if (k > bestCities)
                {
                    bestCities = k;
                    bestId = id;
                }
            }

            return bestId;
        }

        private static int ScoreCity(AiCityStrategicInfo c, AiWorldSnapshot snapshot, int primaryEnemyNationId)
        {
            int score = c.IncomeWeight * 10;
            if (c.IsRecordedCapital) score += 500;
            if (c.OwnerNationId == primaryEnemyNationId) score += 200;

            if (UnitManager.instance != null &&
                UnitManager.instance.GetUnitAtPosition(c.CityLocation) == null)
                score += 80;

            return score;
        }
    }
}
