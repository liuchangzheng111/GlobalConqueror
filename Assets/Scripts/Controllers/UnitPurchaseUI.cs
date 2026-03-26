using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using UnityEditor.Experimental.GraphView;

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

        [Header("页面按钮")]
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button artilleryButton;
        [SerializeField] private Button planeButton;
        [SerializeField] private Button antiaircraftButton;

        private CityData currentCity;
        private PortData currentPort;
        private List<GameObject> currentAvailable;
        private Canvas _canvas;
        private Camera _uiCamera;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
            Hide();
        }

        private void OnEnable()
        {
            StartCoroutine(BindWhenMapManagerReady());
        }

        private System.Collections.IEnumerator BindWhenMapManagerReady()
        {
            while (UnitController.instance == null || NationManager.instance == null || UnitManager.instance == null)
            {
                yield return null;
            }

            NationManager.instance.OnNationTurnEnd += (nationData) => Hide();
            UnitManager.instance.OnUnitSpawned += (unitData, gameObject) => Hide();

            if (soldierButton != null)
            {
                soldierButton.onClick.AddListener(() => RefreshButtons(UnitManager.instance.AvailableSoldier));
            }
            if (armorButton != null)
            {
                armorButton.onClick.AddListener(() => RefreshButtons(UnitManager.instance.AvailableArmor));
            }
            if (artilleryButton != null)
            {
                artilleryButton.onClick.AddListener(() => RefreshButtons(UnitManager.instance.AvailableArtillery));
            }
            if (planeButton != null)
            {

            }
            if (antiaircraftButton != null)
            {

            }
        }

        private void OnDisable()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnNationTurnEnd -= (nationData) => Hide();
            }
            if (UnitManager.instance != null)
            {
                UnitManager.instance.OnUnitSpawned -= (unitData, gameObject) => Hide();
            }
        }
        private void OnDestroy()
        {
            if (soldierButton != null)
            {
                soldierButton.onClick.RemoveListener(() => RefreshButtons(UnitManager.instance.AvailableSoldier));
            }
            if (armorButton != null)
            {
                armorButton.onClick.RemoveListener(() => RefreshButtons(UnitManager.instance.AvailableArmor));
            }
            if (artilleryButton != null)
            {
                artilleryButton.onClick.RemoveListener(() => RefreshButtons(UnitManager.instance.AvailableArtillery));
            }
            if (planeButton != null)
            {

            }
            if (antiaircraftButton != null)
            {

            }
        }

        public void OnPurchaseBottomClick(CityData city, PortData port)
        {
            // 若玩家正在对单位下达移动/攻击指令（或单位正在移动动画中），不弹出购买面板
            if (UnitController.IsUnitCommandActive)
            {
                Hide();
                return;
            }

            if (CityManager.instance == null || NationManager.instance?.CurrentNation == null)
            {
                Hide();
                return;
            }

            Vector3Int vector3Int = city != null ? city.cityLocation : (port != null ? port.portLocation : Vector3Int.zero);
                       
            // 若该格有单位，不显示购买面板（由 UnitController 处理单位选中）
            if (UnitManager.instance != null && UnitManager.instance.GetUnitAtPosition(vector3Int) != null)
            {
                Hide();
                return;
            }
            
            if (city == null || city.ownerNationId != NationManager.instance.CurrentNation.nationId)
            {
                if (port != null && port.ownerNationId == NationManager.instance.CurrentNation.nationId)
                {
                    ShowForPort(port);
                    return;
                }

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
            if (UnitManager.instance != null)
            {
                currentCity = city;
                if (panelRoot != null) panelRoot.SetActive(true);

                soldierButton.gameObject.SetActive(true);
                armorButton.gameObject.SetActive(true);
                artilleryButton.gameObject.SetActive(true);
                planeButton.gameObject.SetActive(true);
                antiaircraftButton.gameObject.SetActive(true);

                RefreshButtons(UnitManager.instance.AvailableSoldier);
            }
        }

        /// <summary>
        /// 为指定港口显示购买面板
        /// </summary>
        public void ShowForPort(PortData port)
        {
            if (UnitManager.instance != null)
            {
                currentPort = port;
                if (panelRoot != null) panelRoot.SetActive(true);

                soldierButton.gameObject.SetActive(false);
                armorButton.gameObject.SetActive(false);
                artilleryButton.gameObject.SetActive(false);
                planeButton.gameObject.SetActive(false);
                antiaircraftButton.gameObject.SetActive(false);

                RefreshButtons(UnitManager.instance.AvailableShip);
            }
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            currentCity = null;
            currentPort = null;
            currentAvailable = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>
        /// 刷新按钮
        /// </summary>
        private void RefreshButtons(List<GameObject> AvailableUnits)
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null || AvailableUnits == null || UnitManager.instance == null)
                return;

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            var nation = NationManager.instance?.CurrentNation;
            if (nation == null || (currentCity == null && currentPort == null)) return;

            currentAvailable = AvailableUnits;
            foreach (var unit in AvailableUnits)
            {
                if (unit == null) continue;

                var unitType = unit.GetComponent<InitialUnitSpawn>().unitType;
                if (unitType == null) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                var btn = go.GetComponent<Button>();
                var unitPurchaseItemView = go.GetComponent<UnitPurchaseItemView>();

                if (unitPurchaseItemView != null)
                {
                    unitPurchaseItemView.Setup(unitType);
                }

                if (btn != null)
                {
                    if (currentCity != null)
                    {
                        btn.interactable = UnitManager.instance.CanSatisfyProduceCondition(currentCity, unitType);                       
                    }
                    else
                    {
                        btn.interactable = UnitManager.instance.CanSatisfyProduceCondition(currentPort, unitType); 
                    }
                    btn.onClick.AddListener(() => OnPurchaseClicked(unit));
                }
            }
        }

        /// <summary>
        /// 点击购买
        /// </summary>
        /// <param name="unitType"></param>
        private void OnPurchaseClicked(GameObject unit)
        {
            if ((currentCity == null && currentPort == null) || unit == null) return;

            if (currentCity != null)
            {
                if (UnitManager.instance.TryPurchaseUnit(currentCity, unit))
                {
                    RefreshButtons(currentAvailable);
                }
            }
            else
            {
                if (UnitManager.instance.TryPurchaseUnit(currentPort, unit))
                {
                    RefreshButtons(currentAvailable);
                }
            }
        }
    }
}
