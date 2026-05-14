using System;
using System.Collections;
using System.Collections.Generic;
using GlobalConqueror.Models;
using GlobalConqueror.Utils;
using GlobalConqueror.Controllers;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 极简陆战 AI：各单位先尝试向最近敌军或敌方城市靠近一格路径终点，
    /// 再尝试攻击范围内血量最低的敌军。不造兵、不用空军。
    /// </summary>
    public static class SimpleNationSkirmishAi
    {
        /// <param name="shouldContinue">若返回 false（例如回合已切换），立即停止。</param>
        public static IEnumerator RunSimpleSkirmishTurn(NationData aiNation, float pauseBetweenActions, Func<bool> shouldContinue)
        {
            if (aiNation == null || UnitManager.instance == null)
                yield break;

            List<UnitData> units = UnitManager.instance.GetUnitsByNation(aiNation.nationId);
            for (int i = 0; i < units.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, units.Count);
                (units[i], units[j]) = (units[j], units[i]);
            }

            foreach (UnitData unit in units)
            {
                if (unit == null || unit.unitType == null)
                    continue;
                if (!UnitManager.instance.AllUnits.Contains(unit))
                    continue;
                if (shouldContinue != null && !shouldContinue())
                    yield break;

                if (!unit.hasMovedThisTurn && !unit.isUnderConstruction)
                {
                    if (TryPickBestMoveCell(unit, aiNation.nationId, out Vector3Int moveDest))
                    {
                        Coroutine moveRoutine = null;
                        if (UnitController.instance != null)
                            moveRoutine = UnitController.instance.StartAnimatedMoveForAi(unit, moveDest);
                        if (moveRoutine != null)
                            yield return moveRoutine;
                        else if (UnitManager.instance.TryMoveUnit(unit, moveDest))
                            SyncUnitVisualToGrid(unit);

                        if (pauseBetweenActions > 0f)
                            yield return new WaitForSeconds(pauseBetweenActions);
                    }
                }

                if (shouldContinue != null && !shouldContinue())
                    yield break;

                if (!unit.hasAttackedThisTurn && !unit.isUnderConstruction)
                {
                    if (TryPickAttackCell(unit, out Vector3Int attackCell) &&
                        UnitManager.instance.TryAttack(unit, attackCell))
                    {
                        Vector3Int attackerCell = unit.position;
                        if (UnitController.instance != null)
                            UnitController.instance.ClearActionableHighlightAt(attackerCell);

                        if (pauseBetweenActions > 0f)
                            yield return new WaitForSeconds(pauseBetweenActions);
                    }
                }
            }
        }

        /// <summary>
        /// 尝试找到战略目标
        /// 优先选择最近敌军或敌方城市，若没有则选择最近敌军或敌方城市。
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="nationId">国家ID</param>
        /// <param name="goal">目标</param>
        /// <returns>是否找到目标</returns>
        private static bool TryFindStrategicGoal(Vector3Int from, int nationId, out Vector3Int goal)
        {
            goal = default;
            Vector3Int? best = null;
            int bestDist = int.MaxValue;

            void Consider(Vector3Int pos)
            {
                int d = HexGridUtils.GetHexDistance(from, pos);
                if (!best.HasValue || d < bestDist || (d == bestDist && CompareCell(pos, best.Value) < 0))
                {
                    bestDist = d;
                    best = pos;
                }
            }

            UnitManager um = UnitManager.instance;
            if (um != null)
            {
                foreach (UnitData u in um.AllUnits)
                {
                    if (u == null || u.ownerNationId < 0 || u.ownerNationId == nationId)
                        continue;
                    Consider(u.position);
                }
            }

            if (CityManager.instance != null)
            {
                foreach (CityData city in CityManager.instance.AllCities)
                {
                    if (city == null || city.ownerNationId == nationId)
                        continue;
                    Consider(city.cityLocation);
                }
            }

            if (!best.HasValue)
                return false;
            goal = best.Value;
            return true;
        }

        /// <summary>
        /// 尝试找到最佳移动单元
        /// 优先选择最近敌军或敌方城市，若没有则选择最近敌军或敌方城市。
        /// 如果无法到达目标，则返回 false。
        /// </summary>
        /// <param name="unit">单位</param>
        /// <param name="nationId">国家ID</param>
        /// <param name="dest">目标</param>
        /// <returns>是否找到目标</returns>
        private static bool TryPickBestMoveCell(UnitData unit, int nationId, out Vector3Int dest)
        {
            dest = default;
            if (unit == null || UnitManager.instance == null || MapManager.instance == null)
                return false;

            HashSet<Vector3Int> reachable = UnitManager.instance.GetReachablePositions(unit);
            if (reachable == null || reachable.Count == 0)
                return false;

            if (!TryFindStrategicGoal(unit.position, nationId, out Vector3Int goal))
                return false;

            int distHere = HexGridUtils.GetHexDistance(unit.position, goal);
            bool any = false;
            Vector3Int bestCell = default;
            int bestDist = int.MaxValue;

            foreach (Vector3Int c in reachable)
            {
                int d = HexGridUtils.GetHexDistance(c, goal);
                if (!any || d < bestDist || (d == bestDist && CompareCell(c, bestCell) < 0))
                {
                    bestDist = d;
                    bestCell = c;
                    any = true;
                }
            }

            if (!any || bestDist >= distHere)
                return false;

            dest = bestCell;
            return true;
        }

        /// <summary>
        /// 尝试找到最佳攻击单元
        /// 优先选择血量最低的敌军，若没有则选择最近敌军或敌方城市。
        /// 如果无法攻击，则返回 false。
        /// </summary>
        /// <param name="unit">单位</param>
        /// <param name="cell">目标</param>
        /// <returns>是否找到目标</returns>
        private static bool TryPickAttackCell(UnitData unit, out Vector3Int cell)
        {
            cell = default;
            if (unit == null || UnitManager.instance == null)
                return false;

            HashSet<Vector3Int> attackable = UnitManager.instance.GetAttackablePositions(unit);
            if (attackable == null || attackable.Count == 0)
                return false;

            UnitData bestDef = null;
            Vector3Int bestCell = default;
            foreach (Vector3Int c in attackable)
            {
                UnitData def = UnitManager.instance.GetUnitAtPosition(c);
                if (def == null)
                    continue;
                if (bestDef == null ||
                    def.currentHealth < bestDef.currentHealth ||
                    (def.currentHealth == bestDef.currentHealth && CompareCell(c, bestCell) < 0))
                {
                    bestDef = def;
                    bestCell = c;
                }
            }

            if (bestDef == null)
                return false;
            cell = bestCell;
            return true;
        }

        /// <summary>
        /// 比较两个单元
        /// 优先选择血量最低的敌军，若没有则选择最近敌军或敌方城市。
        /// 如果无法攻击，则返回 false。
        /// </summary>
        /// <param name="a">单元</param>
        /// <param name="b">单元</param>
        /// <returns>比较结果</returns>
        private static int CompareCell(Vector3Int a, Vector3Int b)
        {
            int c = a.x.CompareTo(b.x);
            if (c != 0) return c;
            return a.y.CompareTo(b.y);
        }

        /// <summary>
        /// AI 不经由 <see cref="Controllers.UnitController"/> 的 DOTween 移动时，将模型对齐到逻辑格。
        /// 驳船/上岸若触发了换预制体，由 <see cref="UnitManager"/> 事件另行处理。
        /// </summary>
        private static void SyncUnitVisualToGrid(UnitData unit)
        {
            if (unit == null || MapManager.instance == null || MapManager.instance.Tilemap == null)
                return;

            Vector3 world = MapManager.instance.Tilemap.GetCellCenterWorld(unit.position);
            if (UnitController.instance != null)
            {
                GameObject go = UnitController.instance.GetUnitGameObject(unit);
                if (go != null)
                    go.transform.position = world;
            }
        }
    }
}
