using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Economy
{
    /// <summary>
    /// AI 在己方领土建造防空与堡垒（依赖 <see cref="NationManager.CurrentNation"/> 为行动国）。
    /// </summary>
    public static class AiEconomyDefenseExecutor
    {
        public const int MaxAntiAirBuildsPerTurn = 2;
        public const int MaxFortBuildsPerTurn = 1;

        public static IEnumerator CoExecuteDefense(
            AiNationTurnContext context,
            float pauseAfterActionSeconds,
            System.Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null)
                yield break;
            if (NationManager.instance == null ||
                NationManager.instance.CurrentNation?.nationId != context.ActingNation.nationId)
                yield break;

            Vector3Int? anchor = context.OperationalPlan?.PreferDefensivePosture == true &&
                                 context.Snapshot?.Situation?.PriorityDefendCell is { } defend
                ? defend
                : AiEconomyAnchor.Resolve(context);
            int nationId = context.ActingNation.nationId;

            int aaBuilt = 0;
            foreach (Vector3Int cell in CollectDefenseCandidateCells(nationId, anchor, preferCityCore: true))
            {
                if (shouldContinue != null && !shouldContinue()) yield break;
                if (aaBuilt >= MaxAntiAirBuildsPerTurn) break;
                if (TryBuildBestAntiAir(cell, context.ActingNation))
                {
                    aaBuilt++;
                    if (pauseAfterActionSeconds > 0f)
                        yield return new WaitForSeconds(pauseAfterActionSeconds);
                }
            }

            int fortBuilt = 0;
            foreach (Vector3Int cell in CollectDefenseCandidateCells(nationId, anchor, preferCityCore: false))
            {
                if (shouldContinue != null && !shouldContinue()) yield break;
                if (fortBuilt >= MaxFortBuildsPerTurn) break;
                if (TryBuildCheapestFort(cell, context.ActingNation))
                {
                    fortBuilt++;
                    if (pauseAfterActionSeconds > 0f)
                        yield return new WaitForSeconds(pauseAfterActionSeconds);
                }
            }
        }

        private static IEnumerable<Vector3Int> CollectDefenseCandidateCells(
            int nationId,
            Vector3Int? anchor,
            bool preferCityCore)
        {
            var seen = new HashSet<Vector3Int>();
            var result = new List<Vector3Int>();

            if (CityManager.instance?.AllCities == null || MapManager.instance == null)
                yield break;

            var myCities = CityManager.instance.AllCities
                .Where(c => c != null && c.ownerNationId == nationId)
                .OrderBy(c => AiEconomyAnchor.Distance(c.cityLocation, anchor))
                .ToList();

            foreach (CityData city in myCities)
            {
                if (preferCityCore)
                    TryAddCell(city.cityLocation, nationId, seen, result);

                foreach (Vector3Int neighbor in HexGridUtils.GetPointNeighbors(city.cityLocation))
                {
                    if (!preferCityCore)
                        TryAddCell(neighbor, nationId, seen, result);
                }
            }

            result.Sort((a, b) => AiEconomyAnchor.Distance(a, anchor).CompareTo(AiEconomyAnchor.Distance(b, anchor)));
            foreach (Vector3Int c in result)
                yield return c;
        }

        private static void TryAddCell(Vector3Int cell, int nationId, HashSet<Vector3Int> seen, List<Vector3Int> result)
        {
            if (!seen.Add(cell)) return;
            if (!MapManager.instance.IsCoordinateValid(cell)) return;
            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null || tile.ownerId != nationId) return;
            result.Add(cell);
        }

        private static bool TryBuildBestAntiAir(Vector3Int cell, NationData nation)
        {
            if (AntiAirManager.instance == null || nation == null) return false;
            MapTileData tile = MapManager.instance?.GetTileData(cell);
            if (tile?.antiAir != null) return false;
            if (!AntiAirManager.instance.CanBuildAntiAir(cell)) return false;

            AntiAirConfig best = null;
            int bestCost = int.MaxValue;
            foreach (AntiAirConfig cfg in AntiAirManager.instance.antiAir)
            {
                if (cfg == null) continue;
                int cost = cfg.goldCost + cfg.industryCost * 2 + cfg.scienceCost * 3;
                if (!CanAfford(nation, cfg.goldCost, cfg.industryCost, cfg.scienceCost))
                    continue;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = cfg;
                }
            }

            return best != null && AntiAirManager.instance.TryBuildAntiAir(cell, best);
        }

        private static bool TryBuildCheapestFort(Vector3Int cell, NationData nation)
        {
            if (UnitManager.instance == null || nation == null) return false;
            UnitTypeConfig bestType = null;
            int bestCost = int.MaxValue;

            foreach (GameObject prefab in UnitManager.instance.AvailableFort)
            {
                if (prefab == null) continue;
                if (!prefab.TryGetComponent<InitialUnitSpawn>(out var spawn) || spawn.unitType == null)
                    continue;
                UnitTypeConfig t = spawn.unitType;
                if (t.unitProperty != UnitProperty.Fort) continue;
                int cost = t.goldCost + t.industryCost * 2 + t.scienceCost * 3;
                if (!CanAfford(nation, t.goldCost, t.industryCost, t.scienceCost))
                    continue;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestType = t;
                }
            }

            return bestType != null && UnitManager.instance.TryBuildFort(cell, bestType);
        }

        private static bool CanAfford(NationData nation, int gold, int industry, int science) =>
            nation.gold >= gold && nation.industry >= industry && nation.science >= science;
    }
}
