using System.Collections.Generic;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Core
{
    /// <summary>
    /// AI 回合开始时的世界只读快照：敌国列表、城市摘要、兵力数量、态势评估等。
    /// </summary>
    public sealed class AiWorldSnapshot
    {
        public int ActingNationId { get; }
        public int CurrentGlobalTurn { get; }
        public IReadOnlyList<int> EnemyNationIds { get; }
        public IReadOnlyList<AiCityStrategicInfo> Cities { get; }
        public IReadOnlyDictionary<int, int> NationUnitCounts { get; }
        public AiSituationAssessment Situation { get; }

        private AiWorldSnapshot(
            int actingNationId,
            int currentGlobalTurn,
            IReadOnlyList<int> enemyNationIds,
            IReadOnlyList<AiCityStrategicInfo> cities,
            IReadOnlyDictionary<int, int> nationUnitCounts,
            AiSituationAssessment situation)
        {
            ActingNationId = actingNationId;
            CurrentGlobalTurn = currentGlobalTurn;
            EnemyNationIds = enemyNationIds;
            Cities = cities;
            NationUnitCounts = nationUnitCounts;
            Situation = situation;
        }

        public static AiWorldSnapshot Build(int actingNationId)
        {
            var enemyIds = new List<int>();
            var cities = new List<AiCityStrategicInfo>();
            var unitCounts = new Dictionary<int, int>();

            int turn = 0;
            if (NationManager.instance != null)
                turn = NationManager.instance.CurrentTurn;

            if (NationManager.instance?.Nations != null)
            {
                foreach (NationData n in NationManager.instance.Nations)
                {
                    if (n == null || n.isDefeated) continue;
                    if (n.nationId == actingNationId) continue;
                    enemyIds.Add(n.nationId);
                }
            }

            if (CityManager.instance?.AllCities != null)
            {
                foreach (CityData city in CityManager.instance.AllCities)
                {
                    if (city == null) continue;

                    NationData ownerNation = NationManager.instance != null
                        ? NationManager.instance.GetNation(city.ownerNationId)
                        : null;

                    bool isCapital = ownerNation != null &&
                                       !string.IsNullOrEmpty(ownerNation.capital) &&
                                       city.cityName == ownerNation.capital;

                    int w = city.CityGoldProduced + city.CityIndustryProduced + city.CityScienceProduced;
                    cities.Add(new AiCityStrategicInfo(
                        city.cityId,
                        city.cityName,
                        city.ownerNationId,
                        city.cityLocation,
                        w,
                        isCapital));
                }
            }

            if (UnitManager.instance != null && NationManager.instance?.Nations != null)
            {
                foreach (NationData n in NationManager.instance.Nations)
                {
                    if (n == null || n.isDefeated) continue;
                    int c = UnitManager.instance.GetUnitsByNation(n.nationId).Count;
                    unitCounts[n.nationId] = c;
                }
            }

            AiSituationAssessment situation = AiSituationAssessment.Build(actingNationId);
            return new AiWorldSnapshot(actingNationId, turn, enemyIds, cities, unitCounts, situation);
        }
    }
}
