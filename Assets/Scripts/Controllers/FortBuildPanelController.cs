using System.Collections;
using System.Collections.Generic;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 选格建造堡垒面板
    /// 列表生成方式与 <see cref="UnitPurchaseUI"/> 一致，复用 unitPurchaseButtonPrefab + <see cref="UnitPurchaseItemView"/>。
    /// </summary>
    public class FortBuildPanelController : MonoBehaviour
    {
        [Header("主按钮")]
        [SerializeField] private Button buildFortButton;
        [SerializeField] private TextMeshProUGUI buildFortButtonLabel;

        [Header("列表区域（与 UnitPurchaseUI 对齐）")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject unitPurchaseButtonPrefab;

        [Header("页面按钮")]
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button artilleryButton;
        [SerializeField] private Button planeButton;

        [Header("关闭")]
        [SerializeField] private Button closePanelButton;

        private Vector3Int? _selectedCell;
        private List<GameObject> _currentAvailableFortPrefabs;
        private bool _fortVisibilityDirty;

        private void Awake()
        {
            Hide();
            if (buildFortButton != null)
            {
                buildFortButton.onClick.AddListener(ToggleFortList);
            }

            if (closePanelButton != null)
            {
                closePanelButton.onClick.AddListener(OnClosePanelClicked);
            }
        }

        private void OnEnable()
        {
            StartCoroutine(BindWhenReady());
        }

        private IEnumerator BindWhenReady()
        {
            while (MapManager.instance == null || NationManager.instance == null || UnitManager.instance == null)
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

        private void OnDestroy()
        {
            if (buildFortButton != null) buildFortButton.onClick.RemoveAllListeners();
            if (closePanelButton != null) closePanelButton.onClick.RemoveAllListeners();
        }

        private void OnClosePanelClicked()
        {
            if (listRoot != null) listRoot.SetActive(false);
        }

        private void OnTileSelected(Vector3Int cell)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            _selectedCell = cell;
            // 与其它 OnTileSelected 订阅者同帧执行时，单位移动协程可能尚未把 isAnimating 置为 true，
            // 推迟到 LateUpdate 再判定，避免“点移动目标空格却先弹出建造堡垒”的冲突。
            _fortVisibilityDirty = true;
        }

        private void LateUpdate()
        {
            if (!_fortVisibilityDirty)
            {
                return;
            }

            _fortVisibilityDirty = false;

            if (_selectedCell == null)
            {
                return;
            }

            Vector3Int cell = _selectedCell.Value;

            if (UnitController.IsUnitCommandActive)
            {
                Hide();
                return;
            }

            if (IsBuildableFortCell(cell))
            {
                ShowFortButton();
            }
            else
            {
                Hide();
            }
        }

        private bool IsBuildableFortCell(Vector3Int cell)
        {
            if (MapManager.instance == null || NationManager.instance == null || UnitManager.instance == null) return false;
            if (!MapManager.instance.IsCoordinateValid(cell)) return false;

            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null) return false;

            if (tile.ownerId != nation.nationId) return false;
            if (tile.tileType != TileType.Plain && tile.tileType != TileType.Forest && tile.tileType != TileType.Mountain) return false;

            if (UnitManager.instance.GetUnitAtPosition(cell) != null) return false;
            return true;
        }

        private void ToggleFortList()
        {
            if (listRoot == null) return;
            bool next = !listRoot.activeSelf;
            if (next)
            {
                RefreshFortButtons(UnitManager.instance != null ? UnitManager.instance.AvailableFort : null);
            }
            listRoot.SetActive(next);
            soldierButton.gameObject.SetActive(false);
            armorButton.gameObject.SetActive(false);
            artilleryButton.gameObject.SetActive(false);
            planeButton.gameObject.SetActive(false);
            closePanelButton.gameObject.SetActive(true);
        }

        private void RefreshFortButtons(List<GameObject> availableFortPrefabs)
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null || availableFortPrefabs == null ||
                UnitManager.instance == null || NationManager.instance == null)
            {
                return;
            }

            ClearContainer();

            if (_selectedCell == null || !IsBuildableFortCell(_selectedCell.Value))
            {
                return;
            }

            _currentAvailableFortPrefabs = availableFortPrefabs;

            foreach (var fortPrefab in availableFortPrefabs)
            {
                if (fortPrefab == null) continue;
                var spawn = fortPrefab.GetComponent<InitialUnitSpawn>();
                if (spawn == null || spawn.unitType == null) continue;
                if (spawn.unitType.unitProperty != UnitProperty.Fort) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                if (go.TryGetComponent<UnitPurchaseItemView>(out var view))
                {
                    view.Setup(spawn.unitType);
                }

                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.interactable = CanAffordFort(spawn.unitType);
                    GameObject capturedPrefab = fortPrefab;
                    btn.onClick.AddListener(() => OnFortPurchaseClicked(capturedPrefab));
                }
            }
        }

        private void OnFortPurchaseClicked(GameObject fortPrefab)
        {
            if (_selectedCell == null || fortPrefab == null || UnitManager.instance == null) return;
            var spawn = fortPrefab.GetComponent<InitialUnitSpawn>();
            if (spawn == null || spawn.unitType == null) return;

            bool ok = UnitManager.instance.TryBuildFort(_selectedCell.Value, spawn.unitType);
            if (ok)
            {
                Hide();
            }
            else
            {
                ShowFortButton();
                RefreshFortButtons(UnitManager.instance != null ? UnitManager.instance.AvailableFort : null);
            }
        }

        private static bool CanAffordFort(UnitTypeConfig fortType)
        {
            if (fortType == null) return false;
            if (NationManager.instance == null) return false;
            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;
            return nation.gold >= fortType.goldCost &&
                   nation.industry >= fortType.industryCost &&
                   nation.science >= fortType.scienceCost;
        }

        private void Hide()
        {
            if (buildFortButton != null) buildFortButton.gameObject.SetActive(false);
            if (listRoot != null) listRoot.SetActive(false);
            ClearContainer();
            _currentAvailableFortPrefabs = null;
            _selectedCell = null;
        }

        private void ShowFortButton()
        {
            if (listRoot != null) listRoot.SetActive(false);

            if (buildFortButton != null) buildFortButton.gameObject.SetActive(true);
            if (buildFortButtonLabel != null) buildFortButtonLabel.text = "建造堡垒";

            ClearContainer();
            _currentAvailableFortPrefabs = null;
        }

        private void ClearContainer()
        {
            if (buttonContainer == null) return;
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
