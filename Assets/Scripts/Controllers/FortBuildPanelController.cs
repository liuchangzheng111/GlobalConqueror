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
    /// 选格建造面板（堡垒 + 防空）。列表生成方式与 <see cref="UnitPurchaseUI"/> 一致，
    /// 复用同一套 <c>unitPurchaseButtonPrefab</c> + <see cref="UnitPurchaseItemView"/>。
    /// </summary>
    public class FortBuildPanelController : MonoBehaviour
    {
        [Header("主按钮")]
        [SerializeField] private Button buildFortButton;
        [SerializeField] private TextMeshProUGUI buildFortButtonLabel;
        [SerializeField] private Button buildAntiAirButton;
        [SerializeField] private TextMeshProUGUI buildAntiAirButtonLabel;

        [Header("列表区域（与 UnitPurchaseUI 对齐）")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject unitPurchaseButtonPrefab;

        private Vector3Int? _selectedCell;
        private List<GameObject> _currentAvailableFortPrefabs;
        private bool _showingAntiAir = false;

        private void Awake()
        {
            Hide();
            if (buildFortButton != null)
            {
                buildFortButton.onClick.AddListener(() => ToggleList(false));
            }
            if (buildAntiAirButton != null)
            {
                buildAntiAirButton.onClick.AddListener(() => ToggleList(true));
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
            if (buildAntiAirButton != null) buildAntiAirButton.onClick.RemoveAllListeners();
        }

        private void OnTileSelected(Vector3Int cell)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            _selectedCell = cell;

            bool canFort = IsBuildableFortCell(cell);
            bool canAA = AntiAirManager.instance != null && AntiAirManager.instance.CanBuildAntiAir(cell);

            if (canFort || canAA)
            {
                ShowButtons(canFort, canAA);
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

            // 堡垒要求空格
            if (UnitManager.instance.GetUnitAtPosition(cell) != null) return false;
            return true;
        }

        private void ToggleList(bool showAntiAir)
        {
            if (listRoot == null) return;
            bool next = !listRoot.activeSelf;
            if (next)
            {
                _showingAntiAir = showAntiAir;
                if (showAntiAir)
                {
                    RefreshAntiAirButtons();
                }
                else
                {
                    RefreshFortButtons(UnitManager.instance != null ? UnitManager.instance.AvailableFort : null);
                }
            }
            listRoot.SetActive(next);
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

        private void RefreshAntiAirButtons()
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null) return;
            if (AntiAirManager.instance == null || NationManager.instance == null) return;
            if (_selectedCell == null) return;
            if (!AntiAirManager.instance.CanBuildAntiAir(_selectedCell.Value)) return;

            ClearContainer();

            for (int level = 1; level <= 3; level++)
            {
                int gold = AntiAirManager.instance.goldCostByLevel.Length > level ? AntiAirManager.instance.goldCostByLevel[level] : 0;
                int industry = AntiAirManager.instance.industryCostByLevel.Length > level ? AntiAirManager.instance.industryCostByLevel[level] : 0;
                int science = AntiAirManager.instance.scienceCostByLevel.Length > level ? AntiAirManager.instance.scienceCostByLevel[level] : 0;
                Sprite icon = AntiAirManager.instance.GetAntiAirIcon(level);

                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                if (go.TryGetComponent<UnitPurchaseItemView>(out var view))
                {
                    string name = level switch
                    {
                        1 => "防空机枪",
                        2 => "防空炮",
                        _ => "防空导弹"
                    };
                    view.SetupAntiAir(name, level, gold, industry, science, icon, "为该地块提供防空减伤与空投伤害");
                }

                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    int capturedLevel = level;
                    btn.interactable = CanAffordAA(gold, industry, science);
                    btn.onClick.AddListener(() => OnAntiAirBuildClicked(capturedLevel));
                }
            }
        }

        private void OnAntiAirBuildClicked(int level)
        {
            if (_selectedCell == null || AntiAirManager.instance == null) return;
            bool ok = AntiAirManager.instance.TryBuildAntiAir(_selectedCell.Value, level);
            if (ok)
            {
                Hide();
            }
            else
            {
                ShowButtons(IsBuildableFortCell(_selectedCell.Value), AntiAirManager.instance.CanBuildAntiAir(_selectedCell.Value));
                RefreshAntiAirButtons();
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
                bool canAA = AntiAirManager.instance != null && AntiAirManager.instance.CanBuildAntiAir(_selectedCell.Value);
                ShowButtons(true, canAA);
                RefreshFortButtons(_currentAvailableFortPrefabs);
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

        private static bool CanAffordAA(int gold, int industry, int science)
        {
            if (NationManager.instance == null) return false;
            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;
            return nation.gold >= gold && nation.industry >= industry && nation.science >= science;
        }

        private void Hide()
        {
            if (buildFortButton != null) buildFortButton.gameObject.SetActive(false);
            if (buildAntiAirButton != null) buildAntiAirButton.gameObject.SetActive(false);
            if (listRoot != null) listRoot.SetActive(false);
            ClearContainer();
            _currentAvailableFortPrefabs = null;
            _selectedCell = null;
        }

        private void ShowButtons(bool showFort, bool showAntiAir)
        {
            if (listRoot != null) listRoot.SetActive(false);

            if (buildFortButton != null) buildFortButton.gameObject.SetActive(showFort);
            if (buildFortButtonLabel != null) buildFortButtonLabel.text = "建造堡垒";

            if (buildAntiAirButton != null) buildAntiAirButton.gameObject.SetActive(showAntiAir);
            if (buildAntiAirButtonLabel != null) buildAntiAirButtonLabel.text = "建造防空";

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
