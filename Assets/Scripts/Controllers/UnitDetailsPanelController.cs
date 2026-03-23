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
    public class UnitDetailsPanelController : MonoBehaviour
    {
        [Header("根节点")]
        [SerializeField] private GameObject panelRoot;

        [Header("窗口区域（用于点击外部关闭）")]
        [Tooltip("只要点击位置不在此 RectTransform 范围内，就会关闭面板。请将其设置为“面板卡片/窗口”的 RectTransform。")]
        [SerializeField] private RectTransform windowRect;

        [Header("关闭按钮（可选）")]
        [SerializeField] private Button closeButton;

        [Header("标题/基础信息")]
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI nationNameText;

        [Header("图标")]
        [SerializeField] private Image nationFlagImage;
        [SerializeField] private Image unitIconImage;

        [Header("血量")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("属性")]
        [SerializeField] private TextMeshProUGUI statsText;

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
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (Input.GetMouseButtonDown(0) == false) return;

            // 只要不在 windowRect 内，就关闭。
            if (windowRect == null)
                return;

            bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(windowRect, Input.mousePosition, _uiCamera);
            if (!clickedInside)
            {
                Hide();
            }
        }

        public void Show(UnitData unit)
        {
            if (unit == null)
            {
                Hide();
                return;
            }

            if (panelRoot != null) panelRoot.SetActive(true);
            else gameObject.SetActive(true);

            UnitTypeConfig type = unit.unitType;
            NationData nation = NationManager.instance != null ? NationManager.instance.GetNation(unit.ownerNationId) : null;

            if (unitNameText != null)
                unitNameText.text = type != null ? type.unitTypeName : "未知单位";

            if (nationNameText != null)
                nationNameText.text = nation != null ? nation.nationName : "未知国家";

            if (nationFlagImage != null)
            {
                nationFlagImage.sprite = nation != null ? nation.nationFlag : null;
                nationFlagImage.enabled = nationFlagImage.sprite != null;
                nationFlagImage.preserveAspect = true;
            }

            if (unitIconImage != null)
            {
                unitIconImage.sprite = type != null ? type.unitIcon : null;
                unitIconImage.enabled = unitIconImage.sprite != null;
                unitIconImage.preserveAspect = true;
            }

            int maxHp = unit.MaxHealth <= 0 ? 1 : unit.MaxHealth;
            int curHp = Mathf.Clamp(unit.currentHealth, 0, maxHp);
            float ratio = Mathf.Clamp01((float)curHp / maxHp);

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = ratio;
                healthSlider.interactable = false;
            }

            if (healthText != null)
                healthText.text = $"{curHp}/{maxHp}";

            if (statsText != null)
            {
                int atk_soldier = type != null ? type.attackStrength_Soldier : 0;
                int atk_armor = type != null ? type.attackStrength_Armor : 0;
                int atk_fort = type != null ? type.attackStrength_Fort : 0;
                int atk_warship = type != null ? type.attackStrength_Warship : 0;
                int atk_battleship = type != null ? type.attackStrength_Battleship : 0;
                float move = unit.MovementRange;
                int range = unit.AttackRange;

                statsText.text =

                    $"对步兵单位攻击力：{atk_soldier}\n" +
                    $"对装甲单位攻击力：{atk_armor}\n" +
                    $"对堡垒单位攻击力：{atk_fort}\n" +
                    $"对轻型舰艇攻击力：{atk_warship}\n" +
                    $"对重型舰艇攻击力：{atk_battleship}\n" +
                    $"移动范围：{move}\n" +
                    $"攻击范围：{range}\n" +
                    $"本回合：{(unit.hasMovedThisTurn ? "已移动" : "未移动")} / {(unit.hasAttackedThisTurn ? "已攻击" : "未攻击")}\n" +
                    $"价值：金钱 {unit.unitType.goldCost} / 工业 {unit.unitType.industryCost} / 科技 {unit.unitType.scienceCost}";
            }
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}

