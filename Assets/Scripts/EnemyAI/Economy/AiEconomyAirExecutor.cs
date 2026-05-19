using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Economy
{
    /// <summary>
    /// AI 空军：优先对空城敌城空降占点，其次空袭低血敌军。
    /// </summary>
    public static class AiEconomyAirExecutor
    {
        public const int MaxAirMissionsPerTurn = 3;

        public static IEnumerator CoExecuteAirMissions(
            AiNationTurnContext context,
            float pauseAfterActionSeconds,
            System.Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null || AirManager.instance == null)
                yield break;
            if (NationManager.instance == null ||
                NationManager.instance.CurrentNation?.nationId != context.ActingNation.nationId)
                yield break;

            int nationId = context.ActingNation.nationId;
            Vector3Int? anchor = AiEconomyAnchor.Resolve(context);
            int missionsDone = 0;

            List<CityData> airBases = CityManager.instance?.AllCities?
                .Where(c => c != null && c.ownerNationId == nationId && c.cityKindsLevel != null &&
                            c.cityKindsLevel.airportLevel > 0)
                .OrderBy(c => AiEconomyAnchor.Distance(c.cityLocation, anchor))
                .ToList() ?? new List<CityData>();

            if (airBases.Count == 0)
                yield break;

            // 1) 空降占空城（战略简报顺序）
            if (context.StrategicBrief?.PrioritizedEnemyCities != null)
            {
                foreach (AiCityStrategicInfo target in context.StrategicBrief.PrioritizedEnemyCities)
                {
                    if (shouldContinue != null && !shouldContinue()) yield break;
                    if (missionsDone >= MaxAirMissionsPerTurn) yield break;
                    if (AllianceManager.AreAllied(nationId, target.OwnerNationId)) continue;
                    if (UnitManager.instance?.GetUnitAtPosition(target.CityLocation) != null) continue;

                    if (TryParadropOnCell(airBases, target.CityLocation, context.ActingNation))
                    {
                        missionsDone++;
                        if (pauseAfterActionSeconds > 0f)
                            yield return new WaitForSeconds(pauseAfterActionSeconds);
                    }
                }
            }

            // 2) 空袭低血敌军
            while (missionsDone < MaxAirMissionsPerTurn)
            {
                if (shouldContinue != null && !shouldContinue()) yield break;
                if (!TryBestAirStrike(airBases, nationId, anchor, context.ActingNation, out _))
                    break;
                missionsDone++;
                if (pauseAfterActionSeconds > 0f)
                    yield return new WaitForSeconds(pauseAfterActionSeconds);
            }
        }

        private static bool TryParadropOnCell(List<CityData> airBases, Vector3Int targetCell, NationData nation)
        {
            foreach (CityData city in airBases)
            {
                foreach (AirMissionConfig mission in GetParadropMissionsAffordable(nation))
                {
                    if (!AirManager.instance.CanUseMissionFromCity(city, mission)) continue;
                    HashSet<Vector3Int> reachable = AirManager.instance.GetParadropPositions(mission, city);
                    if (reachable == null || !reachable.Contains(targetCell)) continue;

                    AirManager.instance.currentCity = city;
                    bool ok = AirManager.instance.TryExecuteMission(mission, targetCell);
                    AirManager.instance.currentCity = null;
                    if (ok) return true;
                }
            }

            return false;
        }

        private static bool TryBestAirStrike(
            List<CityData> airBases,
            int nationId,
            Vector3Int? anchor,
            NationData nation,
            out Vector3Int targetCell)
        {
            targetCell = default;
            UnitData bestUnit = null;
            Vector3Int bestPos = default;
            int bestHp = int.MaxValue;
            int bestDist = int.MaxValue;

            if (UnitManager.instance == null) return false;

            foreach (UnitData u in UnitManager.instance.AllUnits)
            {
                if (u == null || u.ownerNationId < 0 || AllianceManager.AreAllied(nationId, u.ownerNationId))
                    continue;
                int d = anchor.HasValue ? HexGridUtils.GetHexDistance(u.position, anchor.Value) : 0;
                if (u.currentHealth < bestHp || (u.currentHealth == bestHp && d < bestDist))
                {
                    bestHp = u.currentHealth;
                    bestDist = d;
                    bestUnit = u;
                    bestPos = u.position;
                }
            }

            if (bestUnit == null) return false;

            foreach (CityData city in airBases)
            {
                foreach (AirMissionConfig mission in GetAttackMissionsAffordable(nation))
                {
                    if (!AirManager.instance.CanUseMissionFromCity(city, mission)) continue;
                    HashSet<Vector3Int> attackable = AirManager.instance.GetAttackablePositions(mission, city);
                    if (attackable == null || !attackable.Contains(bestPos)) continue;

                    AirManager.instance.currentCity = city;
                    bool ok = AirManager.instance.TryExecuteMission(mission, bestPos);
                    AirManager.instance.currentCity = null;
                    if (ok)
                    {
                        targetCell = bestPos;
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<AirMissionConfig> GetParadropMissionsAffordable(NationData nation)
        {
            if (AirManager.instance?.AvailableAircrafts == null) yield break;
            foreach (AirMissionConfig m in AirManager.instance.AvailableAircrafts
                         .Where(m => m != null && m.type == AirMissionType.ParadropInfantry)
                         .OrderBy(m => m.goldCost + m.industryCost * 2 + m.scienceCost * 3))
            {
                if (nation.gold >= m.goldCost && nation.industry >= m.industryCost && nation.science >= m.scienceCost)
                    yield return m;
            }
        }

        private static IEnumerable<AirMissionConfig> GetAttackMissionsAffordable(NationData nation)
        {
            if (AirManager.instance?.AvailableAircrafts == null) yield break;
            foreach (AirMissionConfig m in AirManager.instance.AvailableAircrafts
                         .Where(m => m != null && m.type == AirMissionType.AttackTarget)
                         .OrderBy(m => m.goldCost + m.industryCost * 2 + m.scienceCost * 3))
            {
                if (nation.gold >= m.goldCost && nation.industry >= m.industryCost && nation.science >= m.scienceCost)
                    yield return m;
            }
        }
    }
}
