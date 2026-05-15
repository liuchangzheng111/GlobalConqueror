using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.Events;

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

        [Header("关闭")]
        [SerializeField] private Button closePanelButton;

        private CityData currentCity;
        private PortData currentPort;
        private List<GameObject> currentAvailable;
        private Canvas _canvas;
        private Camera _uiCamera;
        private Action<NationData> _onNationTurnEndHideHandler;
        private Action<UnitData, GameObject> _onUnitSpawnedHideHandler;
        private UnityAction _soldierPageHandler;
        private UnityAction _armorPageHandler;
        private UnityAction _artilleryPageHandler;
        private UnityAction _planePageHandler;

        private bool _isSelectingAirTarget = false;
        private AirMissionConfig _selectedAirMission;
        private Vector3Int _airTargets = new();

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

            _onNationTurnEndHideHandler ??= (_) => Hide();
            _onUnitSpawnedHideHandler ??= (unitData, gameObject) => Hide();
            NationManager.instance.OnNationTurnEnd += _onNationTurnEndHideHandler;
            UnitManager.instance.OnUnitSpawned += _onUnitSpawnedHideHandler;

            if (soldierButton != null)
            {
                _soldierPageHandler ??= () => RefreshButtons(UnitManager.instance.AvailableSoldier);
                soldierButton.onClick.AddListener(_soldierPageHandler);
            }
            if (armorButton != null)
            {
                _armorPageHandler ??= () => RefreshButtons(UnitManager.instance.AvailableArmor);
                armorButton.onClick.AddListener(_armorPageHandler);
            }
            if (artilleryButton != null)
            {
                _artilleryPageHandler ??= () => RefreshButtons(UnitManager.instance.AvailableArtillery);
                artilleryButton.onClick.AddListener(_artilleryPageHandler);
            }
            if (planeButton != null)
            {
                _planePageHandler ??= ShowAirMissionList;
                planeButton.onClick.AddListener(_planePageHandler);
            }
            if (closePanelButton != null)
            {
                closePanelButton.onClick.AddListener(OnClosePanelClicked);
            }
        }

        private void OnDisable()
        {
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

            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnAirTargetTileSelected;
            }
            if (closePanelButton != null) closePanelButton.onClick.RemoveAllListeners();
        }
        private void OnDestroy()
        {
            if (soldierButton != null)
            {
                if (_soldierPageHandler != null)
                {
                    soldierButton.onClick.RemoveListener(_soldierPageHandler);
                }
            }
            if (armorButton != null)
            {
                if (_armorPageHandler != null)
                {
                    armorButton.onClick.RemoveListener(_armorPageHandler);
                }
            }
            if (artilleryButton != null)
            {
                if (_artilleryPageHandler != null)
                {
                    artilleryButton.onClick.RemoveListener(_artilleryPageHandler);
                }
            }
            if (planeButton != null)
            {
                if (_planePageHandler != null)
                {
                    planeButton.onClick.RemoveListener(_planePageHandler);
                }
            }
        }
        private void OnClosePanelClicked()
        {
            Hide();
        }

        /// <summary>
        /// 显示空军任务列表
        /// </summary>
        private void ShowAirMissionList()
        {
            if (AirManager.instance == null || AirManager.instance.AvailableAircrafts == null) return;
            if (currentCity == null) return;
            if (currentCity.cityKindsLevel == null || currentCity.cityKindsLevel.airportLevel <= 0) return;

            CancelAirTargetSelection();

            if (buttonContainer == null || unitPurchaseButtonPrefab == null) return;
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var mission in AirManager.instance.AvailableAircrafts)
            {
                if (mission == null) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                var btn = go.GetComponent<Button>();
                if (go.TryGetComponent<UnitPurchaseItemView>(out var unitPurchaseItemView))
                {
                    unitPurchaseItemView.Setup(mission);
                }

                if (btn != null && NationManager.instance != null && NationManager.instance.CurrentNation != null)
                {
                    var nation = NationManager.instance.CurrentNation;
                    btn.interactable = nation.gold >= mission.goldCost &&
                                      nation.industry >= mission.industryCost &&
                                      nation.science >= mission.scienceCost &&
                                      AirManager.instance.CanUseMissionFromCity(currentCity, mission);
                    AirMissionConfig captured = mission;
                    btn.onClick.AddListener(() => BeginAirTargetSelection(captured));
                }
            }
        }

        /// <summary>
        /// 开始选择空军目标
        /// </summary>
        /// <param name="mission">空军任务</param>
        private void BeginAirTargetSelection(AirMissionConfig mission)
        {
            if (mission == null) return;
            if (currentCity == null) return;

            _selectedAirMission = mission;
            _isSelectingAirTarget = true;

            if (panelRoot != null) panelRoot.SetActive(false);

            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnAirTargetTileSelected;
                MapManager.instance.OnTileSelected += OnAirTargetTileSelected;
            }

            //显示高亮
            if (UnitController.instance != null && AirManager.instance != null)
            {
                if (mission.type == AirMissionType.AttackTarget)
                {
                    HashSet<Vector3Int> positions = AirManager.instance.GetAttackablePositions(mission, currentCity);
                    UnitController.instance.ShowAttackRangeHighlights(positions);
                }
                else
                {
                    HashSet<Vector3Int> positions = AirManager.instance.GetParadropPositions(mission, currentCity);
                    UnitController.instance.ShowMoveRangeHighlights(positions);
                }
                AirManager.instance.currentCity = currentCity;
            }

            Debug.Log($"空军任务已选择：{mission.missionName}，请点击地图选择目标。");
        }

        /// <summary>
        /// 取消选择空军目标
        /// </summary>
        private void CancelAirTargetSelection()
        {
            _isSelectingAirTarget = false;
            _selectedAirMission = null;
            _airTargets.Set(0, 0, 0);
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileSelected -= OnAirTargetTileSelected;
            }

            if (UnitController.instance != null)
            {
                UnitController.instance.ClearHighlightObjects();
            }
            if(AirManager.instance != null)
            {
                AirManager.instance.currentCity = null;
            }
        }

        /// <summary>
        /// 选择空军目标格子
        /// </summary>
        /// <param name="cell">目标格子</param>
        private void OnAirTargetTileSelected(Vector3Int cell)
        {
            if (!_isSelectingAirTarget) return;
            if (_selectedAirMission == null) return;
            if (AirManager.instance == null) return;

            bool ok = AirManager.instance.TryExecuteMission(_selectedAirMission, cell);
            
            if (!ok)
            {
                CancelAirTargetSelection();
                Debug.Log("空军出击失败：目标不合法/航程不足/资源不足。");
                return;
            }

            CancelAirTargetSelection();
            Debug.Log("空军出击成功。");        
        }

        /// <summary>
        /// 按钮点击
        /// </summary>
        /// <param name="city">城市</param>
        /// <param name="port">港口</param>
        public void OnPurchaseBottomClick(CityData city, PortData port)
        {
            // 若玩家正在对单位下达移动/攻击指令（或单位正在移动动画中），不弹出购买面板
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

            if (UnitManager.instance == null)
            {
                Hide();
                return;
            }

            NationData currentNation = NationManager.instance.CurrentNation;

            // 城市：格上有己方单位时走空军任务，否则走陆军/生产面板（与城市逻辑绑定）
            if (city != null)
            {
                if (city.ownerNationId != currentNation.nationId)
                {
                    Hide();
                    return;
                }

                Vector3Int cityCell = city.cityLocation;
                if (UnitManager.instance.GetUnitAtPosition(cityCell) != null)
                    ShowAirMissionForCity(city);
                else
                    ShowForCity(city);
                return;
            }

            // 港口：仅海军购买面板，不使用城市的空军/生产分支
            if (port != null)
            {
                if (port.ownerNationId != currentNation.nationId)
                {
                    Hide();
                    return;
                }

                if (UnitManager.instance.GetUnitAtPosition(port.portLocation) != null)
                {
                    Hide();
                    return;
                }

                ShowForPort(port);
                return;
            }

            Hide();
        }

        /// <summary>
        /// 为指定城市显示购买面板
        /// </summary>
        public void ShowForCity(CityData city)
        {
            if (UnitManager.instance != null)
            {
                currentPort = null;
                currentCity = city;
                if (panelRoot != null) panelRoot.SetActive(true);

                soldierButton.gameObject.SetActive(true);
                soldierButton.interactable = true;
                armorButton.gameObject.SetActive(true);
                armorButton.interactable = true;
                artilleryButton.gameObject.SetActive(true);
                artilleryButton.interactable = true;
                planeButton.gameObject.SetActive(true);
                planeButton.interactable = true;

                RefreshButtons(UnitManager.instance.AvailableSoldier);
            }
        }

        /// <summary>
        /// 为指定城市显示空军面板
        /// </summary>
        public void ShowAirMissionForCity(CityData city)
        {
            if (UnitManager.instance != null)
            {
                currentPort = null;
                currentCity = city;
                if (panelRoot != null) panelRoot.SetActive(true);

                soldierButton.gameObject.SetActive(true);
                soldierButton.interactable = false;
                armorButton.gameObject.SetActive(true);
                armorButton.interactable = false;
                artilleryButton.gameObject.SetActive(true);
                artilleryButton.interactable = false;
                planeButton.gameObject.SetActive(true);
                planeButton.interactable = true;

                ShowAirMissionList();
            }
        }

        /// <summary>
        /// 为指定港口显示购买面板
        /// </summary>
        public void ShowForPort(PortData port)
        {
            if (UnitManager.instance != null)
            {
                currentCity = null;
                currentPort = port;
                if (panelRoot != null) panelRoot.SetActive(true);

                soldierButton.gameObject.SetActive(false);
                armorButton.gameObject.SetActive(false);
                artilleryButton.gameObject.SetActive(false);
                planeButton.gameObject.SetActive(false);

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
            if (buttonContainer == null || unitPurchaseButtonPrefab == null || AvailableUnits == null || UnitManager.instance == null || NationManager.instance == null)
                return;

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            var nation = NationManager.instance.CurrentNation;
            if (nation == null || (currentCity == null && currentPort == null)) return;

            currentAvailable = AvailableUnits;
            foreach (var unit in AvailableUnits)
            {
                if (unit == null) continue;

                var unitType = unit.GetComponent<InitialUnitSpawn>().unitType;
                if (unitType == null) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                var btn = go.GetComponent<Button>();
                
                if (go.TryGetComponent<UnitPurchaseItemView>(out var unitPurchaseItemView))
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
