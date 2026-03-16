using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 城市购买军队 UI - 选中己方城市时显示可购买兵种
    /// </summary>
    public class UnitPurchaseUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform buttonContainer;

        [Header("预制体")]
        [SerializeField] private GameObject unitPurchaseButtonPrefab;

        private CityData currentCity;

        private void OnEnable()
        {
            StartCoroutine(BindWhenMapManagerReady());
        }

        private System.Collections.IEnumerator BindWhenMapManagerReady()
        {
            while (MapManager.instance == null)
            {
                yield return null;
            }

            MapManager.instance.OnTileSelected += OnTileSelected;
        }

        private void OnDisable()
        {
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnTileSelected;
            }
        }

        private void OnTileSelected(Vector3Int coordinate)
        {
            if (CityManager.instance == null || NationManager.instance?.CurrentNation == null)
            {
                Hide();
                return;
            }

            // 若该格有单位，不显示购买面板（由 UnitController 处理单位选中）
            if (UnitManager.instance != null && UnitManager.instance.GetUnitAtPosition(coordinate) != null)
            {
                Hide();
                return;
            }

            CityData city = CityManager.instance.GetCityAtPosition(coordinate);
            if (city == null || city.ownerNationId != NationManager.instance.CurrentNation.nationId)
            {
                Hide();
                return;
            }

            ShowForCity(city);
        }

        /// <summary>
        /// 为指定城市显示购买面板
        /// </summary>
        public void ShowForCity(CityData city)
        {
            currentCity = city;
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshButtons();
        }

        public void Hide()
        {
            currentCity = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void RefreshButtons()
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null || UnitManager.instance == null)
                return;

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            var nation = NationManager.instance?.CurrentNation;
            if (nation == null || currentCity == null) return;

            foreach (var unit in UnitManager.instance.AvailableUnitTypes)
            {
                if (unit == null) continue;

                var unitType = unit.GetComponent<UnitTypeConfig>();
                if (unitType == null) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                var btn = go.GetComponent<Button>();
                var text = go.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{unitType.unitTypeName}\n" +
                        $"金{unitType.goldCost} 工{unitType.industryCost} 科{unitType.scienceCost}";
                }

                bool canAfford = nation.gold >= unitType.goldCost &&
                    nation.industry >= unitType.industryCost &&
                    nation.science >= unitType.scienceCost;

                if (btn != null)
                {
                    btn.interactable = canAfford;
                    UnitTypeConfig captured = unitType;
                    btn.onClick.AddListener(() => OnPurchaseClicked(captured));
                }
            }
        }

        private void OnPurchaseClicked(UnitTypeConfig unitType)
        {
            if (currentCity == null || unitType == null) return;

            if (UnitManager.instance.TryPurchaseUnit(currentCity, unitType))
            {
                RefreshButtons();
            }
        }
    }
}
