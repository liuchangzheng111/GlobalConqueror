using System.Collections;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 防空建造面板：监听选格，在可建造防空的地块上显示按钮与等级列表。
    /// 与 <see cref="FortBuildPanelController"/> 解耦，可挂在同一 Canvas 下的独立物体上。
    /// </summary>
    public class AntiAirBuildPanelController : MonoBehaviour
    {
        [Header("主按钮")]
        [SerializeField] private Button buildAntiAirButton;
        [SerializeField] private TextMeshProUGUI buildAntiAirButtonLabel;

        [Header("列表区域（与 UnitPurchaseUI / 堡垒面板一致）")]
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

        private void Awake()
        {
            Hide();
            if (buildAntiAirButton != null)
            {
                buildAntiAirButton.onClick.AddListener(ToggleAntiAirList);
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
            while (MapManager.instance == null || NationManager.instance == null)
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
            if (buildAntiAirButton != null) buildAntiAirButton.onClick.RemoveAllListeners();
            if (closePanelButton != null) closePanelButton.onClick.RemoveAllListeners();
        }

        private void OnClosePanelClicked()
        {
            if (listRoot != null) listRoot.SetActive(false);
        }

        /// <summary>
        /// 地块选择事件
        /// </summary>
        /// <param name="cell"></param>
        private void OnTileSelected(Vector3Int cell)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            _selectedCell = cell;

            bool canAA = AntiAirManager.instance != null && AntiAirManager.instance.CanBuildAntiAir(cell);
            if (canAA)
            {
                ShowAntiAirButton();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// 切换防空列表
        /// </summary>
        private void ToggleAntiAirList()
        {
            if (listRoot == null) return;
            bool next = !listRoot.activeSelf;
            if (next)
            {
                RefreshAntiAirButtons();
            }
            listRoot.SetActive(next);
            soldierButton.gameObject.SetActive(false);
            armorButton.gameObject.SetActive(false);
            artilleryButton.gameObject.SetActive(false);
            planeButton.gameObject.SetActive(false);
            closePanelButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 刷新防空按钮
        /// </summary>
        private void RefreshAntiAirButtons()
        {
            if (buttonContainer == null || unitPurchaseButtonPrefab == null) return;
            if (AntiAirManager.instance == null || NationManager.instance == null) return;
            if (_selectedCell == null) return;
            if (!AntiAirManager.instance.CanBuildAntiAir(_selectedCell.Value)) return;

            ClearContainer();

            var antiAirList = AntiAirManager.instance.antiAir;
            for (int i = 0; i < antiAirList.Count; i++)
            {
                AntiAirConfig config = antiAirList[i];
                var go = Instantiate(unitPurchaseButtonPrefab, buttonContainer);
                if (go.TryGetComponent<UnitPurchaseItemView>(out var view))
                {
                    view.SetupAntiAir(config);
                }

                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.interactable = CanAffordAA(config);
                    btn.onClick.AddListener(() => OnAntiAirBuildClicked(config));
                }
            }
        }

        /// <summary>
        /// 防空建造点击事件
        /// </summary>
        /// <param name="level"></param>
        private void OnAntiAirBuildClicked(AntiAirConfig antiAir)
        {
            if (_selectedCell == null || AntiAirManager.instance == null) return;
            bool ok = AntiAirManager.instance.TryBuildAntiAir(_selectedCell.Value, antiAir);
            if (ok)
            {
                Hide();
            }
            else
            {
                if (AntiAirManager.instance.CanBuildAntiAir(_selectedCell.Value))
                {
                    ShowAntiAirButton();
                    RefreshAntiAirButtons();
                }
                else
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// 是否可以建造防空
        /// </summary>
        /// <param name="gold"></param>
        /// <param name="industry"></param>
        /// <param name="science"></param>
        /// <returns></returns>
        private static bool CanAffordAA(AntiAirConfig antiAir)
        {
            if (NationManager.instance == null) return false;
            var nation = NationManager.instance.CurrentNation;
            if (nation == null) return false;
            return nation.gold >= antiAir.goldCost && nation.industry >= antiAir.industryCost && nation.science >= antiAir.scienceCost;
        }

        /// <summary>
        /// 隐藏防空建造面板
        /// </summary>
        private void Hide()
        {
            if (buildAntiAirButton != null) buildAntiAirButton.gameObject.SetActive(false);
            if (listRoot != null) listRoot.SetActive(false);
            ClearContainer();
            _selectedCell = null;
        }

        /// <summary>
        /// 显示防空建造按钮
        /// </summary>
        private void ShowAntiAirButton()
        {
            if (listRoot != null) listRoot.SetActive(false);

            if (buildAntiAirButton != null) buildAntiAirButton.gameObject.SetActive(true);
            if (buildAntiAirButtonLabel != null) buildAntiAirButtonLabel.text = "建造防空";

            ClearContainer();
        }

        /// <summary>
        /// 清空按钮容器
        /// </summary>
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
