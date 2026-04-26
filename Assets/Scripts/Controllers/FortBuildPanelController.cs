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
    /// 堡垒建造面板：选中合法地块时显示「建造堡垒」，列表生成方式与 <see cref="UnitPurchaseUI"/> 一致，
    /// 复用同一套 <c>unitPurchaseButtonPrefab</c> + <see cref="UnitPurchaseItemView"/>。
    /// </summary>
    public class FortBuildPanelController : MonoBehaviour
    {
        [Header("主按钮")]
        [SerializeField] private Button buildButton;
        [SerializeField] private TextMeshProUGUI buildButtonLabel;

        [Header("列表区域（与 UnitPurchaseUI 对齐）")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject unitPurchaseButtonPrefab;

        [Header("页面按钮")]
        [SerializeField] private Button soldierButton;
        [SerializeField] private Button armorButton;
        [SerializeField] private Button artilleryButton;
        [SerializeField] private Button planeButton;
        [SerializeField] private Button antiaircraftButton;

        private Vector3Int? _selectedCell;
        private List<GameObject> _currentAvailableFortPrefabs;

        private void Awake()
        {
            Hide();
            if (buildButton != null)
            {
                buildButton.onClick.AddListener(ToggleList);
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
            if (buildButton != null)
            {
                buildButton.onClick.RemoveListener(ToggleList);
            }
        }

        /// <summary>
        /// 地块选中事件
        /// </summary>
        /// <param name="cell"></param>
        private void OnTileSelected(Vector3Int cell)
        {
            // 点击到 UI 上不处理（避免误触）
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            _selectedCell = cell;

            if (IsBuildableCell(cell))
            {
                ShowButtonOnly();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// 判断地块是否可建造堡垒
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private bool IsBuildableCell(Vector3Int cell)
        {
            if (MapManager.instance == null || NationManager.instance == null || UnitManager.instance == null) return false;
            if (!MapManager.instance.IsCoordinateValid(cell)) return false;

            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;

            MapTileData tile = MapManager.instance.GetTileData(cell);
            if (tile == null) return false;

            // 仅己方领土
            if (tile.ownerId != nation.nationId) return false;

            // 仅普通陆地格
            if (tile.tileType != TileType.Plain && tile.tileType != TileType.Forest && tile.tileType != TileType.Mountain) return false;

            // 目标格必须无单位
            if (UnitManager.instance.GetUnitAtPosition(cell) != null) return false;

            return true;
        }

        /// <summary>
        /// 切换列表显示
        /// </summary>
        private void ToggleList()
        {
            if (listRoot == null) return;
            bool next = !listRoot.activeSelf;
            if (next)
            {
                RefreshButtons(UnitManager.instance != null ? UnitManager.instance.AvailableFort : null);
            }

            // 不显示无关按钮
            soldierButton.gameObject.SetActive(false);
            armorButton.gameObject.SetActive(false);
            artilleryButton.gameObject.SetActive(false);
            planeButton.gameObject.SetActive(false);
            antiaircraftButton.gameObject.SetActive(false);

            listRoot.SetActive(next);
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        private void RefreshButtons(List<GameObject> availableFortPrefabs)
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null || availableFortPrefabs == null ||
                UnitManager.instance == null || NationManager.instance == null)
            {
                return;
            }

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            if (_selectedCell == null || !IsBuildableCell(_selectedCell.Value))
            {
                return;
            }

            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return;

            _currentAvailableFortPrefabs = availableFortPrefabs;

            foreach (var fortPrefab in availableFortPrefabs)
            {
                if (fortPrefab == null) continue;

                var spawn = fortPrefab.GetComponent<InitialUnitSpawn>();
                if (spawn == null || spawn.unitType == null) continue;
                if (spawn.unitType.unitProperty != UnitProperty.Fort) continue;

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                if (go.TryGetComponent<UnitPurchaseItemView>(out var unitPurchaseItemView))
                {
                    unitPurchaseItemView.Setup(spawn.unitType);
                }

                // 与 UnitPurchaseUI 一致优先根节点 Button；兼容 Button 在子节点的情况
                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.interactable = CanAfford(spawn.unitType);
                    GameObject capturedPrefab = fortPrefab;
                    btn.onClick.AddListener(() => OnFortPurchaseClicked(capturedPrefab));
                }
            }
        }

        /// <summary>
        /// 判断是否能购买堡垒
        /// </summary>
        /// <param name="fortType"></param>
        /// <returns></returns>
        private static bool CanAfford(UnitTypeConfig fortType)
        {
            if (fortType == null) return false;
            if (NationManager.instance == null) return false;
            NationData nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;
            return nation.gold >= fortType.goldCost &&
                   nation.industry >= fortType.industryCost &&
                   nation.science >= fortType.scienceCost;
        }

        /// <summary>
        /// 堡垒购买事件
        /// </summary>
        /// <param name="fortPrefab"></param>
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
                ShowButtonOnly();
                RefreshButtons(_currentAvailableFortPrefabs);
            }
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        private void Hide()
        {
            if (buildButton != null) buildButton.gameObject.SetActive(false);
            if (listRoot != null) listRoot.SetActive(false);
            if (buttonContainer != null)
            {
                foreach (Transform child in buttonContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            _currentAvailableFortPrefabs = null;
            _selectedCell = null;
        }
        
        /// <summary>
        /// 只显示按钮
        /// </summary>
        private void ShowButtonOnly()
        {
            if (listRoot != null) listRoot.SetActive(false);
            if (buildButton != null) buildButton.gameObject.SetActive(true);

            if (buildButtonLabel != null)
            {
                buildButtonLabel.text = "建造堡垒";
            }

            if (buttonContainer != null)
            {
                foreach (Transform child in buttonContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            _currentAvailableFortPrefabs = null;
        }
    }
}
