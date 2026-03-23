using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单位详情面板控制器
    /// 将该脚本挂到 UI 面板根物体上，并在 Inspector 绑定各 UI 引用。
    /// </summary>
    public class CityDetailsPanelController : MonoBehaviour
    {
        [Header("根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("购买面板")]
        [SerializeField] private UnitPurchaseUI unitPurchaseUI;

        [Header("关闭按钮（可选）")]
        [SerializeField] private Button closeButton;
        [Header("购买按钮")]
        [SerializeField] private Button PurchaseButton;

        [Header("标题/基础信息")]
        [SerializeField] private TextMeshProUGUI cityNameText;
        [SerializeField] private TextMeshProUGUI nationNameText;

        [Header("图标")]
        [SerializeField] private Image nationFlagImage;
        [SerializeField] private Image industryImage;
        [SerializeField] private Image airportImage;
        [SerializeField] private Image scienceImage;
        [SerializeField] private Image supplyImage;

        [Header("等级")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI industryText;
        [SerializeField] private TextMeshProUGUI airportText;
        [SerializeField] private TextMeshProUGUI scienceText;
        [SerializeField] private TextMeshProUGUI supplyText;

        [Header("产能")]
        [SerializeField] private TextMeshProUGUI gold;
        [SerializeField] private TextMeshProUGUI industry;
        [SerializeField] private TextMeshProUGUI science;


        private CityData currentCity;

        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

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

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            if (PurchaseButton != null)
            {
                PurchaseButton.onClick.AddListener(() => unitPurchaseUI.OnPurchaseBottomClick(currentCity));
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
            NationManager.instance.OnNationTurnEnd += (nationData) => Hide();
            UnitManager.instance.OnUnitSpawned += (unitData, gameObject) => Hide();
        }

        private void OnDisable()
        {
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnTileSelected;
            }
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
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
            if (PurchaseButton != null)
            {
                PurchaseButton.onClick.RemoveListener(() => unitPurchaseUI.OnPurchaseBottomClick(currentCity));
            }
        }

        private void OnTileSelected(Vector3Int coordinate)
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

            CityData city = CityManager.instance.GetCityAtPosition(coordinate);
            if (city == null)
            {
                Hide();
                return;
            }

            Show(city);
        }

        public void Show(CityData city)
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
                cityNameText.text = city.cityName != null ? city.cityName : "未知城市";
            }

            if (nationNameText != null)
            {
                nationNameText.enabled = nation != null;
                nationNameText.text = nation != null ? $"所属 {nation.nationName}" : "未知国家";
            }

            if (nationFlagImage != null)
            {
                nationFlagImage.enabled = nation != null;
                nationFlagImage.sprite = nation != null ? nation.nationFlag : null;
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
                industryText.text = $"工业 {city.cityKindsLevel.industryLevel} 级";
                industryImage.sprite = CityManager.instance.industry[city.cityKindsLevel.industryLevel - 1];
            }

            if (airportText != null && airportImage != null && city.cityKindsLevel.airportLevel > 0)
            {
                airportText.enabled = true;
                airportImage.enabled = true;
                airportText.text = $"机场 {city.cityKindsLevel.airportLevel} 级";
                airportImage.sprite = CityManager.instance.airport[city.cityKindsLevel.airportLevel - 1];
            }

            if (scienceText != null && scienceImage != null && city.cityKindsLevel.scienceLevel > 0)
            {
                scienceText.enabled = true;
                scienceImage.enabled = true;
                scienceText.text = $"科技 {city.cityKindsLevel.scienceLevel} 级";
                scienceImage.sprite = CityManager.instance.science[city.cityKindsLevel.scienceLevel - 1];
            }

            if (supplyText != null && supplyImage != null && city.cityKindsLevel.supplyLevel > 0)
            {
                supplyText.enabled = true;
                supplyImage.enabled = true;
                supplyText.text = $"补给 {city.cityKindsLevel.supplyLevel} 级";
                supplyImage.sprite = CityManager.instance.supply[city.cityKindsLevel.supplyLevel - 1];
            }

            if (gold != null && industry != null && science != null)
            {
                gold.enabled = true;
                industry.enabled = true;
                science.enabled = true;
                gold.text = $"每回合金钱产出 {city.cityGoldProduced}";
                industry.text = $"每回合工业产出 {city.cityIndustryProduced}";
                science.text = $"每回合科学产出 {city.cityScienceProduced}";
            }

            if (PurchaseButton != null && city.ownerNationId == NationManager.instance.CurrentNation.nationId && UnitManager.instance.GetUnitAtPosition(city.cityLocation) == null)
            {
                PurchaseButton.gameObject.SetActive(true);
            }
        }

        private void ResetUI()
        {
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
            if (scienceImage != null && scienceText!=null)
            {
                scienceImage.enabled = false;
                scienceImage.sprite = null;
                scienceText.enabled = false;
                scienceText.text = "";
            }
            if (supplyImage != null && supplyText!=null)
            {
                supplyImage.enabled = false;
                supplyImage.sprite = null;
                supplyText.enabled = false;
                supplyText.text = "";
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
            if (panelRoot != null) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
            unitPurchaseUI.Hide();
        }
    }
}

