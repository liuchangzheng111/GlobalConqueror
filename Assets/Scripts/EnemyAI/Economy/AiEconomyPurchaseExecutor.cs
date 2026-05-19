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
    /// 在己方城市/港口按条件与资源尝试购买单位（不打开 UI，直接走 <see cref="UnitManager.TryPurchaseUnit"/>）。
    /// </summary>
    public static class AiEconomyPurchaseExecutor
    {
        /// <summary>本回合最多在几座不同城市各尝试买 1 个单位。</summary>
        public const int MaxCityPurchaseAttempts = 4;

        /// <summary>本回合最多在几个不同港口各尝试买 1 个单位。</summary>
        public const int MaxPortPurchaseAttempts = 2;

        /// <summary>
        /// 尝试城市与港口采购；每次成功购买后可停顿若干秒（与出兵动画节奏对齐）。
        /// </summary>
        public static IEnumerator CoExecutePurchases(
            AiNationTurnContext context,
            float pauseAfterBuySeconds,
            System.Func<bool> shouldContinue)
        {
            if (context?.ActingNation == null || UnitManager.instance == null)
                yield break;

            int nationId = context.ActingNation.nationId;
            Vector3Int? sortOrigin = AiEconomyAnchor.ResolvePurchaseSortOrigin(context);

            IReadOnlyList<GameObject> cityPrefabs = CollectCityPurchasePrefabsOrderedByCost().ToList();
            IReadOnlyList<GameObject> portPrefabs = CollectPortPurchasePrefabsOrderedByCost().ToList();

            if (cityPrefabs.Count > 0 && CityManager.instance?.AllCities != null)
            {
                List<CityData> myCities = CityManager.instance.AllCities
                    .Where(c => c != null && c.ownerNationId == nationId)
                    .OrderBy(c => AiEconomyAnchor.Distance(c.cityLocation, sortOrigin))
                    .ToList();

                int attempts = 0;
                foreach (CityData city in myCities)
                {
                    if (shouldContinue != null && !shouldContinue())
                        yield break;
                    if (attempts >= MaxCityPurchaseAttempts)
                        break;
                    if (TryBuyOneUnitAtCity(city, cityPrefabs))
                    {
                        attempts++;
                        if (pauseAfterBuySeconds > 0f)
                            yield return new WaitForSeconds(pauseAfterBuySeconds);
                    }
                }
            }

            if (portPrefabs.Count > 0 && PortManager.instance?.AllPorts != null)
            {
                List<PortData> myPorts = PortManager.instance.AllPorts
                    .Where(p => p != null && p.ownerNationId == nationId)
                    .OrderBy(p => AiEconomyAnchor.Distance(p.portLocation, sortOrigin))
                    .ToList();

                int attempts = 0;
                foreach (PortData port in myPorts)
                {
                    if (shouldContinue != null && !shouldContinue())
                        yield break;
                    if (attempts >= MaxPortPurchaseAttempts)
                        break;
                    if (TryBuyOneUnitAtPort(port, portPrefabs))
                    {
                        attempts++;
                        if (pauseAfterBuySeconds > 0f)
                            yield return new WaitForSeconds(pauseAfterBuySeconds);
                    }
                }
            }
        }

        private static bool TryBuyOneUnitAtCity(CityData city, IReadOnlyList<GameObject> prefabsOrdered)
        {
            foreach (GameObject prefab in prefabsOrdered)
            {
                if (prefab == null) continue;
                if (!prefab.TryGetComponent<InitialUnitSpawn>(out var spawn) || spawn.unitType == null)
                    continue;
                if (spawn.unitType.unitProperty is not (UnitProperty.Soldier or UnitProperty.Armor))
                    continue;
                if (!UnitManager.instance.CanSatisfyProduceCondition(city, spawn.unitType))
                    continue;
                if (UnitManager.instance.TryPurchaseUnit(city, prefab))
                    return true;
            }

            return false;
        }

        private static bool TryBuyOneUnitAtPort(PortData port, IReadOnlyList<GameObject> prefabsOrdered)
        {
            foreach (GameObject prefab in prefabsOrdered)
            {
                if (prefab == null) continue;
                if (!prefab.TryGetComponent<InitialUnitSpawn>(out var spawn) || spawn.unitType == null)
                    continue;
                if (spawn.unitType.unitProperty is not (UnitProperty.Warship or UnitProperty.Battleship))
                    continue;
                if (!UnitManager.instance.CanSatisfyProduceCondition(port, spawn.unitType))
                    continue;
                if (UnitManager.instance.TryPurchaseUnit(port, prefab))
                    return true;
            }

            return false;
        }

        private static IEnumerable<GameObject> CollectCityPurchasePrefabsOrderedByCost()
        {
            if (UnitManager.instance == null) yield break;

            var list = new List<GameObject>();
            void AddRange(List<GameObject> src)
            {
                if (src == null) return;
                foreach (GameObject go in src)
                {
                    if (go != null) list.Add(go);
                }
            }

            AddRange(UnitManager.instance.AvailableSoldier);
            AddRange(UnitManager.instance.AvailableArmor);

            foreach (GameObject go in list
                         .Where(p => p != null && p.GetComponent<InitialUnitSpawn>()?.unitType != null)
                         .OrderBy(p =>
                         {
                             UnitTypeConfig t = p.GetComponent<InitialUnitSpawn>().unitType;
                             return t.goldCost + t.industryCost * 2 + t.scienceCost * 3;
                         }))
                yield return go;
        }

        private static IEnumerable<GameObject> CollectPortPurchasePrefabsOrderedByCost()
        {
            if (UnitManager.instance?.AvailableShip == null) yield break;

            foreach (GameObject go in UnitManager.instance.AvailableShip
                         .Where(p => p != null && p.GetComponent<InitialUnitSpawn>()?.unitType != null)
                         .OrderBy(p =>
                         {
                             UnitTypeConfig t = p.GetComponent<InitialUnitSpawn>().unitType;
                             return t.goldCost + t.industryCost * 2 + t.scienceCost * 3;
                         }))
                yield return go;
        }
    }
}
