using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// ����/�ۿ�������������
    /// </summary>
    public class CityDetailsPanelController : MonoBehaviour
    {
        [Header("���ڵ�")]
        [SerializeField] private GameObject panelRoot;

        [Header("�������")]
        [SerializeField] private UnitPurchaseUI unitPurchaseUI;

        [Header("�رհ�ť����ѡ��")]
        [SerializeField] private Button closeButton;
        [Header("����ť")]
        [SerializeField] private Button PurchaseButton;

        [Header("����/������Ϣ")]
        [SerializeField] private TextMeshProUGUI cityNameText;
        [SerializeField] private TextMeshProUGUI nationNameText;

        [Header("ͼ��")]
        [SerializeField] private Image nationFlagImage;
        [SerializeField] private Image industryImage;
        [SerializeField] private Image airportImage;
        [SerializeField] private Image scienceImage;
        [SerializeField] private Image supplyImage;

        [Header("�ȼ�")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI industryText;
        [SerializeField] private TextMeshProUGUI airportText;
        [SerializeField] private TextMeshProUGUI scienceText;
        [SerializeField] private TextMeshProUGUI supplyText;

        [Header("����")]
        [SerializeField] private TextMeshProUGUI gold;
        [SerializeField] private TextMeshProUGUI industry;
        [SerializeField] private TextMeshProUGUI science;


        private CityData currentCity;
        private PortData currentPort;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private Canvas _canvas;
        private Camera _uiCamera;
        private Action<NationData> _onNationTurnEndHideHandler;
        private Action<UnitData, GameObject> _onUnitSpawnedHideHandler;
        private UnityAction _purchaseClickHandler;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
            Hide();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            if (PurchaseButton != null)
            {
                _purchaseClickHandler ??= () => unitPurchaseUI.OnPurchaseBottomClick(currentCity, currentPort);
                PurchaseButton.onClick.AddListener(_purchaseClickHandler);
            }
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

            MapManager.instance.OnTileSelected += OnTileSelected;
            _onNationTurnEndHideHandler ??= (_) => Hide();
            _onUnitSpawnedHideHandler ??= (unitData, gameObject) => Hide();
            NationManager.instance.OnNationTurnEnd += _onNationTurnEndHideHandler;
            UnitManager.instance.OnUnitSpawned += _onUnitSpawnedHideHandler;
        }

        private void OnDisable()
        {
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnTileSelected;
            }
            if (NationManager.instance != null)
            {
                if (_onNationTurnEndHideHandler != null)
                {
                    NationManager.instance.OnNationTurnEnd -= _onNationTurnEndHideHandler;
                }
            }
            if (UnitManager.instance != null)
            {
                if (_onUnitSpawnedHideHandler != null)
                {
                    UnitManager.instance.OnUnitSpawned -= _onUnitSpawnedHideHandler;
                }
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
            if (PurchaseButton != null)
            {
                if (_purchaseClickHandler != null)
                {
                    PurchaseButton.onClick.RemoveListener(_purchaseClickHandler);
                }
            }
        }

        private void OnTileSelected(Vector3Int coordinate)
        {
            // ��������ڶԵ�λ�´��ƶ�/����ָ���λ�����ƶ������У����������������
            if (UnitController.IsUnitCommandActive)
            {
                Hide();
                return;
            }

            if (CityManager.instance == null || NationManager.instance == null || NationManager.instance.CurrentNation == null)
            {
                Hide();
                return;
            }

            CityData city = CityManager.instance.GetCityAtPosition(coordinate);
            PortData port = PortManager.instance.GetPortAtPosition(coordinate);
            if (city == null && port == null)
            {
                Hide();
                return;
            }
            else if (city != null)
            {
                ShowCity(city);
                return;
            }
            else  if(port != null)
            {
                ShowPort(port);
                return;
            }
        }

        public void ShowCity(CityData city)
        {
            if (city == null)
            {
                Hide();
                return;
            }

            currentCity = city;
            ResetUI();

            if (panelRoot != null) panelRoot.SetActive(true);
            else gameObject.SetActive(true);

            NationData nation = NationManager.instance != null ? NationManager.instance.GetNation(city.ownerNationId) : null;

            if (cityNameText != null)
            {
                cityNameText.enabled = city.cityName != null;
                cityNameText.text = city.cityName ?? "δ֪����";
            }

            if (nationNameText != null)
            {
                nationNameText.enabled = nation != null;
                nationNameText.text = nation != null ? $"���� {nation.nationName}" : "δ֪����";
            }

            if (nationFlagImage != null)
            {
                nationFlagImage.enabled = nation != null;
                nationFlagImage.sprite = nation?.nationFlag;
                nationFlagImage.preserveAspect = true;
            }

            if (levelText != null)
            {
                string text = city.cityKindsLevel.GetCityLevelString();
                levelText.enabled = text != "";
                levelText.text = text;
            }

            if (industryText != null && industryImage != null && city.cityKindsLevel.industryLevel > 0)
            {
                industryText.enabled = true;
                industryImage.enabled = true;
                industryText.text = $"��ҵ {city.cityKindsLevel.industryLevel} ��";
                industryImage.sprite = CityManager.instance.industry[city.cityKindsLevel.industryLevel - 1];
            }

            if (airportText != null && airportImage != null && city.cityKindsLevel.airportLevel > 0)
            {
                airportText.enabled = true;
                airportImage.enabled = true;
                airportText.text = $"���� {city.cityKindsLevel.airportLevel} ��";
                airportImage.sprite = CityManager.instance.airport[city.cityKindsLevel.airportLevel - 1];
            }

            if (scienceText != null && scienceImage != null && city.cityKindsLevel.scienceLevel > 0)
            {
                scienceText.enabled = true;
                scienceImage.enabled = true;
                scienceText.text = $"�Ƽ� {city.cityKindsLevel.scienceLevel} ��";
                scienceImage.sprite = CityManager.instance.science[city.cityKindsLevel.scienceLevel - 1];
            }

            if (supplyText != null && supplyImage != null && city.cityKindsLevel.supplyLevel > 0)
            {
                supplyText.enabled = true;
                supplyImage.enabled = true;
                supplyText.text = $"���� {city.cityKindsLevel.supplyLevel} ��";
                supplyImage.sprite = CityManager.instance.supply[city.cityKindsLevel.supplyLevel - 1];
            }

            if (gold != null && industry != null && science != null)
            {
                gold.enabled = true;
                industry.enabled = true;
                science.enabled = true;
                gold.text = $"ÿ�غϽ�Ǯ���� {city.CityGoldProduced}";
                industry.text = $"ÿ�غϹ�ҵ���� {city.CityIndustryProduced}";
                science.text = $"ÿ�غϿ�ѧ���� {city.CityScienceProduced}";
            }

            if (PurchaseButton != null && city.ownerNationId == NationManager.instance.CurrentNation.nationId && UnitManager.instance.GetUnitAtPosition(city.cityLocation) == null)
            {
                PurchaseButton.gameObject.SetActive(true);
            }
        }
        public void ShowPort(PortData port)
        {
            if (port == null)
            {
                Hide();
                return;
            }

            currentPort = port;
            ResetUI();

            if (panelRoot != null) panelRoot.SetActive(true);
            else gameObject.SetActive(true);

            NationData nation = NationManager.instance != null ? NationManager.instance.GetNation(port.ownerNationId) : null;

            if (cityNameText != null)
            {
                cityNameText.enabled = port.portName != null;
                cityNameText.text = port.portName ?? "δ֪�ۿ�";
            }

            if (nationNameText != null)
            {
                nationNameText.enabled = nation != null;
                nationNameText.text = nation != null ? $"���� {nation.nationName}" : "δ֪����";
            }

            if (nationFlagImage != null)
            {
                nationFlagImage.enabled = nation != null;
                nationFlagImage.sprite = nation?.nationFlag;
                nationFlagImage.preserveAspect = true;
            }

            if (levelText != null)
            {
                string text = port.GetPortLevelString();
                levelText.enabled = text != "";
                levelText.text = text;
            }

            if (gold != null && industry != null)
            {
                gold.enabled = true;
                industry.enabled = true;
                gold.text = $"ÿ�غϽ�Ǯ���� {port.PortGoldProduced}";
                industry.text = $"ÿ�غϹ�ҵ���� {port.PortIndustryProduced}";
            }

            if (PurchaseButton != null && port.ownerNationId == NationManager.instance.CurrentNation.nationId && UnitManager.instance.GetUnitAtPosition(port.portLocation) == null)
            {
                PurchaseButton.gameObject.SetActive(true);
            }
        }
        private void ResetUI()
        {
            if (cityNameText != null && levelText != null)
            {
                cityNameText.enabled = false;
                cityNameText.text = "";
                levelText.enabled = false;
                levelText.text = "";
            }
            if (industryImage != null && industryText != null)
            {
                industryImage.enabled = false;
                industryImage.sprite = null;
                industryText.enabled = false;
                industryText.text = "";
            }
            if (airportImage != null && airportText != null)
            {
                airportImage.enabled = false;
                airportImage.sprite = null;
                airportText.enabled = false;
                airportText.text = "";

            }
            if (scienceImage != null && scienceText != null)
            {
                scienceImage.enabled = false;
                scienceImage.sprite = null;
                scienceText.enabled = false;
                scienceText.text = "";
            }
            if (supplyImage != null && supplyText != null)
            {
                supplyImage.enabled = false;
                supplyImage.sprite = null;
                supplyText.enabled = false;
                supplyText.text = "";
            }
            if (gold != null && industry != null && science != null)
            {
                gold.enabled = false;
                gold.text = "";
                industry.enabled = false;
                industry.text = "";
                science.enabled = false;
                science.text = "";
            }
            if (PurchaseButton != null)
            {
                PurchaseButton.gameObject.SetActive(false);
            }
            unitPurchaseUI.Hide();
        }

        public void Hide()
        {
            currentCity = null;
            currentPort = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
            unitPurchaseUI.Hide();
        }
    }
}

