using System.Collections.Generic;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Core
{
    /// <summary>
    /// 层 0 态势：空降风险、可夺空城、防守威胁等（文档 §七.层0）。
    /// </summary>
    public sealed class AiSituationAssessment
    {
        /// <summary>己方城市格上无单位，易被敌方空降占城。</summary>
        public IReadOnlyList<Vector3Int> OwnCityCellsNeedingGarrison { get; }

        /// <summary>敌方城市格无守军，陆进或空降可占。</summary>
        public IReadOnlyList<Vector3Int> EmptyCapturableEnemyCityCells { get; }

        /// <summary>敌军进入己方首都或前沿城威胁范围。</summary>
        public bool DefensiveThreatActive { get; }

        /// <summary>优先需要增援的城格（首都优先）。</summary>
        public Vector3Int? PriorityDefendCell { get; }

        public AiSituationAssessment(
            IReadOnlyList<Vector3Int> ownCityCellsNeedingGarrison,
            IReadOnlyList<Vector3Int> emptyCapturableEnemyCityCells,
            bool defensiveThreatActive,
            Vector3Int? priorityDefendCell)
        {
            OwnCityCellsNeedingGarrison = ownCityCellsNeedingGarrison;
            EmptyCapturableEnemyCityCells = emptyCapturableEnemyCityCells;
            DefensiveThreatActive = defensiveThreatActive;
            PriorityDefendCell = priorityDefendCell;
        }

        public static AiSituationAssessment Build(int actingNationId)
        {
            var needGarrison = new List<Vector3Int>();
            var emptyEnemyCities = new List<Vector3Int>();
            bool threat = false;
            Vector3Int? defendCell = null;
            int bestThreatDist = int.MaxValue;

            if (CityManager.instance?.AllCities == null || UnitManager.instance == null)
                return new AiSituationAssessment(needGarrison, emptyEnemyCities, false, null);

            NationData self = NationManager.instance?.GetNation(actingNationId);

            foreach (CityData city in CityManager.instance.AllCities)
            {
                if (city == null) continue;

                if (city.ownerNationId == actingNationId)
                {
                    if (UnitManager.instance.GetUnitAtPosition(city.cityLocation) == null)
                        needGarrison.Add(city.cityLocation);

                    int enemyNear = MinEnemyDistanceToCell(city.cityLocation, actingNationId);
                    if (enemyNear <= ThreatRadiusHexes)
                    {
                        threat = true;
                        bool isCapital = self != null && !string.IsNullOrEmpty(self.capital) &&
                                         city.cityName == self.capital;
                        int priority = isCapital ? -1 : enemyNear;
                        if (priority < bestThreatDist)
                        {
                            bestThreatDist = priority;
                            defendCell = city.cityLocation;
                        }
                    }
                }
                else if (!AllianceManager.AreAllied(actingNationId, city.ownerNationId))
                {
                    if (UnitManager.instance.GetUnitAtPosition(city.cityLocation) == null)
                        emptyEnemyCities.Add(city.cityLocation);
                }
            }

            return new AiSituationAssessment(needGarrison, emptyEnemyCities, threat, defendCell);
        }

        /// <summary>敌军单位到某格的最小六角距。</summary>
        public const int ThreatRadiusHexes = 4;

        private static int MinEnemyDistanceToCell(Vector3Int cell, int actingNationId)
        {
            int min = int.MaxValue;
            if (UnitManager.instance == null) return min;
            foreach (UnitData u in UnitManager.instance.AllUnits)
            {
                if (u == null || u.ownerNationId < 0 || AllianceManager.AreAllied(actingNationId, u.ownerNationId))
                    continue;
                int d = HexGridUtils.GetHexDistance(cell, u.position);
                if (d < min) min = d;
            }
            return min;
        }
    }
}
