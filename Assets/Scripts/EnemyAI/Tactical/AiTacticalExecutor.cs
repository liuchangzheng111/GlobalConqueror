using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalConqueror.Controllers;
using GlobalConqueror.EnemyAI.Core;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using UnityEngine;

namespace GlobalConqueror.EnemyAI.Tactical
{
    /// <summary>
    /// 层 4/5 战术：防守增援 → 火炮先攻 → 寻路推进 / 占空城（文档 §七.层4～5）。
    /// </summary>
    public static class AiTacticalExecutor
    {
        public static IEnumerator Run(AiNationTurnContext context, float pauseBetweenActions, Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null || UnitManager.instance == null)
                yield break;

            int nationId = context.ActingNation.nationId;
            Vector3Int? pushAnchor = context.OperationalPlan?.MainPushAnchor;
            bool defensive = context.OperationalPlan?.PreferDefensivePosture == true;

            List<UnitData> units = UnitManager.instance.GetUnitsByNation(nationId);
            Shuffle(units);

            if (defensive && context.Snapshot?.Situation != null)
            {
                yield return CoRunDefensiveGarrison(context, units, pauseBetweenActions, shouldContinue);
                if (shouldContinue != null && !shouldContinue())
                    yield break;
            }

            List<UnitData> artilleryFirst = units
                .Where(u => u != null && IsArtillery(u))
                .ToList();
            List<UnitData> others = units
                .Where(u => u != null && !IsArtillery(u))
                .ToList();

            foreach (UnitData unit in artilleryFirst.Concat(others))
            {
                if (unit == null || unit.unitType == null) continue;
                if (!UnitManager.instance.AllUnits.Contains(unit)) continue;
                if (shouldContinue != null && !shouldContinue()) yield break;

                yield return CoActWithUnit(unit, nationId, context, pushAnchor, pauseBetweenActions, shouldContinue);
            }
        }

        private static IEnumerator CoRunDefensiveGarrison(
            AiNationTurnContext context,
            List<UnitData> units,
            float pause,
            Func<bool> shouldContinue)
        {
            IReadOnlyList<Vector3Int> garrisonTargets = context.Snapshot.Situation.OwnCityCellsNeedingGarrison;
            Vector3Int? priority = context.Snapshot.Situation.PriorityDefendCell;

            var targets = new List<Vector3Int>();
            if (priority.HasValue) targets.Add(priority.Value);
            foreach (Vector3Int c in garrisonTargets)
            {
                if (!targets.Contains(c)) targets.Add(c);
            }

            foreach (Vector3Int cityCell in targets)
            {
                if (UnitManager.instance.GetUnitAtPosition(cityCell) != null) continue;

                foreach (UnitData unit in units)
                {
                    if (unit == null || unit.hasMovedThisTurn || unit.isUnderConstruction) continue;
                    if (!UnitManager.instance.AllUnits.Contains(unit)) continue;
                    if (shouldContinue != null && !shouldContinue()) yield break;

                    if (!TryPickMoveToward(unit, cityCell, out Vector3Int dest)) continue;

                    yield return CoExecuteMove(unit, dest, pause);
                    break;
                }
            }
        }

        private static IEnumerator CoActWithUnit(
            UnitData unit,
            int nationId,
            AiNationTurnContext context,
            Vector3Int? pushAnchor,
            float pause,
            Func<bool> shouldContinue)
        {
            if (!unit.hasAttackedThisTurn && !unit.isUnderConstruction)
            {
                if (TryPickAttackCell(unit, nationId, context, out Vector3Int attackCell) &&
                    UnitManager.instance.TryAttack(unit, attackCell))
                {
                    UnitController.instance?.ClearHighlightsAfterAiUnitAttack(unit);
                    if (pause > 0f) yield return new WaitForSeconds(pause);
                }
            }

            if (shouldContinue != null && !shouldContinue()) yield break;

            if (!unit.hasMovedThisTurn && !unit.isUnderConstruction)
            {
                if (TryPickOffensiveMove(unit, nationId, context, pushAnchor, out Vector3Int moveDest))
                    yield return CoExecuteMove(unit, moveDest, pause);
            }
        }

        private static IEnumerator CoExecuteMove(UnitData unit, Vector3Int dest, float pause)
        {
            Coroutine routine = UnitController.instance != null
                ? UnitController.instance.StartAnimatedMoveForAi(unit, dest)
                : null;
            if (routine != null)
                yield return routine;
            else if (UnitManager.instance.TryMoveUnit(unit, dest))
                SyncVisual(unit);

            if (pause > 0f)
                yield return new WaitForSeconds(pause);
        }

        private static bool TryPickOffensiveMove(
            UnitData unit,
            int nationId,
            AiNationTurnContext context,
            Vector3Int? pushAnchor,
            out Vector3Int dest)
        {
            dest = default;
            if (unit == null || UnitManager.instance == null || MapManager.instance == null)
                return false;

            HashSet<Vector3Int> reachable = UnitManager.instance.GetReachablePositions(unit);
            if (reachable == null || reachable.Count == 0) return false;

            // 最高优先：本回合可踏入的空敌城（直接占城）
            if (context.Snapshot?.Situation?.EmptyCapturableEnemyCityCells != null)
            {
                foreach (Vector3Int cityCell in context.Snapshot.Situation.EmptyCapturableEnemyCityCells)
                {
                    if (reachable.Contains(cityCell) && UnitManager.instance.GetUnitAtPosition(cityCell) == null)
                    {
                        dest = cityCell;
                        return true;
                    }
                }
            }

            Vector3Int? goal = pushAnchor;
            if (!goal.HasValue)
            {
                if (!TryFindStrategicGoal(unit.position, nationId, null, out Vector3Int g))
                    return false;
                goal = g;
            }

            // 沿 FindPath 走到本回合可达的最远格
            List<Vector3Int> path = UnitManager.instance.FindPath(unit, goal.Value);
            if (path != null && path.Count > 1)
            {
                for (int i = path.Count - 1; i >= 1; i--)
                {
                    if (reachable.Contains(path[i]))
                    {
                        int distHere = HexGridUtils.GetHexDistance(unit.position, goal.Value);
                        int distDest = HexGridUtils.GetHexDistance(path[i], goal.Value);
                        if (distDest < distHere)
                        {
                            dest = path[i];
                            return true;
                        }
                    }
                }
            }

            return TryPickGreedyStepToward(unit, nationId, goal, reachable, out dest);
        }

        private static bool TryPickMoveToward(UnitData unit, Vector3Int target, out Vector3Int dest)
        {
            dest = default;
            HashSet<Vector3Int> reachable = UnitManager.instance.GetReachablePositions(unit);
            if (reachable == null || reachable.Count == 0) return false;

            int distHere = HexGridUtils.GetHexDistance(unit.position, target);
            bool any = false;
            Vector3Int best = default;
            int bestDist = int.MaxValue;
            foreach (Vector3Int c in reachable)
            {
                int d = HexGridUtils.GetHexDistance(c, target);
                if (!any || d < bestDist || (d == bestDist && CompareCell(c, best) < 0))
                {
                    bestDist = d;
                    best = c;
                    any = true;
                }
            }

            if (!any || bestDist >= distHere) return false;
            dest = best;
            return true;
        }

        private static bool TryPickGreedyStepToward(
            UnitData unit,
            int nationId,
            Vector3Int? goal,
            HashSet<Vector3Int> reachable,
            out Vector3Int dest)
        {
            dest = default;
            if (!goal.HasValue) return false;
            if (!TryFindStrategicGoal(unit.position, nationId, goal, out Vector3Int g))
                g = goal.Value;

            int distHere = HexGridUtils.GetHexDistance(unit.position, g);
            bool any = false;
            Vector3Int bestCell = default;
            int bestDist = int.MaxValue;
            foreach (Vector3Int c in reachable)
            {
                int d = HexGridUtils.GetHexDistance(c, g);
                if (!any || d < bestDist || (d == bestDist && CompareCell(c, bestCell) < 0))
                {
                    bestDist = d;
                    bestCell = c;
                    any = true;
                }
            }

            if (!any || bestDist >= distHere) return false;
            dest = bestCell;
            return true;
        }

        private static bool TryPickAttackCell(
            UnitData unit,
            int nationId,
            AiNationTurnContext context,
            out Vector3Int cell)
        {
            cell = default;
            HashSet<Vector3Int> attackable = UnitManager.instance.GetAttackablePositions(unit);
            if (attackable == null || attackable.Count == 0) return false;

            UnitData bestDef = null;
            Vector3Int bestCell = default;
            int bestScore = int.MinValue;
            Vector3Int? anchor = context.OperationalPlan?.MainPushAnchor;

            foreach (Vector3Int c in attackable)
            {
                UnitData def = UnitManager.instance.GetUnitAtPosition(c);
                if (def == null) continue;

                int score = 1000 - def.currentHealth;
                if (CityManager.instance != null)
                {
                    CityData cityAt = CityManager.instance.GetCityAtPosition(c);
                    if (cityAt != null && !AllianceManager.AreAllied(nationId, cityAt.ownerNationId))
                        score += 800;
                }

                if (anchor.HasValue)
                    score -= HexGridUtils.GetHexDistance(c, anchor.Value) * 2;

                if (IsArtillery(unit))
                    score += 200;

                if (bestDef == null || score > bestScore ||
                    (score == bestScore && CompareCell(c, bestCell) < 0))
                {
                    bestScore = score;
                    bestDef = def;
                    bestCell = c;
                }
            }

            if (bestDef == null) return false;
            cell = bestCell;
            return true;
        }

        private static bool TryFindStrategicGoal(Vector3Int from, int nationId, Vector3Int? hint, out Vector3Int goal)
        {
            goal = default;
            Vector3Int? best = null;
            int bestD1 = int.MaxValue;
            int bestD2 = int.MaxValue;

            void Consider(Vector3Int pos)
            {
                int d1 = HexGridUtils.GetHexDistance(from, pos);
                int d2 = hint.HasValue ? HexGridUtils.GetHexDistance(pos, hint.Value) : 0;
                if (!best.HasValue ||
                    d1 < bestD1 ||
                    (d1 == bestD1 && d2 < bestD2) ||
                    (d1 == bestD1 && d2 == bestD2 && CompareCell(pos, best.Value) < 0))
                {
                    bestD1 = d1;
                    bestD2 = d2;
                    best = pos;
                }
            }

            foreach (UnitData u in UnitManager.instance.AllUnits)
            {
                if (u == null || u.ownerNationId < 0 || AllianceManager.AreAllied(nationId, u.ownerNationId))
                    continue;
                Consider(u.position);
            }

            if (CityManager.instance != null)
            {
                foreach (CityData city in CityManager.instance.AllCities)
                {
                    if (city == null || AllianceManager.AreAllied(nationId, city.ownerNationId)) continue;
                    Consider(city.cityLocation);
                }
            }

            if (!best.HasValue) return false;
            goal = best.Value;
            return true;
        }

        private static bool IsArtillery(UnitData unit) =>
            UnitManager.instance != null &&
            UnitManager.instance.IsUnitInAvailableList(unit, UnitManager.instance.AvailableArtillery);

        private static void Shuffle(List<UnitData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static int CompareCell(Vector3Int a, Vector3Int b)
        {
            int c = a.x.CompareTo(b.x);
            return c != 0 ? c : a.y.CompareTo(b.y);
        }

        private static void SyncVisual(UnitData unit)
        {
            if (MapManager.instance?.Tilemap == null || UnitController.instance == null) return;
            GameObject go = UnitController.instance.GetUnitGameObject(unit);
            if (go != null)
                go.transform.position = MapManager.instance.Tilemap.GetCellCenterWorld(unit.position);
        }
    }
}
