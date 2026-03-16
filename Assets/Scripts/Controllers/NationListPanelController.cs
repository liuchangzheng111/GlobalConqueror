using System.Collections.Generic;
using DG.Tweening;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 国家列表侧边滑出面板控制器
    /// </summary>
    public class NationListPanelController : MonoBehaviour
    {
        [Header("侧边面板根节点")]
        [SerializeField] private RectTransform panelRoot;

        [Header("内容区域")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private NationListItemView itemPrefab;

        [Header("切换按钮")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleButtonLabel;

        [Header("滑动动效设置")]
        [SerializeField] private float animationDuration = 0.25f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        [Header("锚点位置设置")]
        [SerializeField] private Vector2 shownAnchoredPosition;
        [SerializeField] private Vector2 hiddenAnchoredPosition;

        private bool isVisible = false;
        private Tween currentTween;

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            // 初始隐藏在摄像机外侧，只露出按钮
            SetPanelPosition(hiddenAnchoredPosition);
            isVisible = false;
            UpdateButtonLabel();
        }

        private void Start()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnNationTurnStart += RefreshList;
            }
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }

            if (NationManager.instance != null)
            {
                NationManager.instance.OnNationTurnStart -= RefreshList;
            }
        }

        /// <summary>
        /// 供按钮调用：打开/关闭国家列表侧边面板
        /// </summary>
        public void Toggle()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// 打开面板（从右侧滑入）
        /// </summary>
        public void Show()
        {
            if (isVisible)
            {
                return;
            }

            isVisible = true;

            RefreshList();

            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            if (panelRoot != null)
            {
                currentTween = panelRoot.DOAnchorPos(shownAnchoredPosition, animationDuration)
                    .SetEase(slideEase);
            }

            UpdateButtonLabel();
        }

        /// <summary>
        /// 关闭面板（向左侧滑出）
        /// </summary>
        public void Hide()
        {
            if (!isVisible)
            {
                return;
            }

            isVisible = false;

            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            if (panelRoot != null)
            {
                currentTween = panelRoot.DOAnchorPos(hiddenAnchoredPosition, animationDuration)
                    .SetEase(slideEase);
            }

            UpdateButtonLabel();
        }

        /// <summary>
        /// 立即设置面板位置
        /// </summary>
        private void SetPanelPosition(Vector2 anchoredPosition)
        {
            if (panelRoot != null)
            {
                panelRoot.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// 根据当前显示状态更新按钮文本（> / <）
        /// </summary>
        private void UpdateButtonLabel()
        {
            if (toggleButtonLabel == null)
            {
                return;
            }

            // isVisible == true 时，显示 "<" 表示可以向右收起；
            // isVisible == false 时，显示 ">" 表示可以向左滑入。
            toggleButtonLabel.text = isVisible ? "<" : ">";
        }

        /// <summary>
        /// 根据当前国家列表刷新 UI
        /// </summary>
        private void RefreshList(NationData nationIfUse = null)
        {
            if (contentRoot == null || itemPrefab == null)
            {
                return;
            }

            foreach (Transform child in contentRoot)
            {
                Destroy(child.gameObject);
            }

            if (NationManager.instance == null || NationManager.instance.Nations == null)
            {
                return;
            }

            List<NationData> nations = NationManager.instance.Nations;

            for (int i = 0; i < nations.Count; i++)
            {
                NationData nation = nations[i];
                NationListItemView item = Instantiate(itemPrefab, contentRoot);
                item.Setup(nation);
            }
        }
    }
}

