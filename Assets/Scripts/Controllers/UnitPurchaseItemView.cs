using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单个国家信息条目视图
    /// </summary>
    public class UnitPurchaseItemView : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI UnitNameText;
        [SerializeField] private TextMeshProUGUI GoldText;
        [SerializeField] private TextMeshProUGUI IndustryText;
        [SerializeField] private TextMeshProUGUI ScienceText;
        [SerializeField] private Image UnitImage;
        [SerializeField] private Color BackgroundColor;

        /// <summary>
        /// 使用指定国家数据刷新 UI
        /// </summary>
        public void Setup(UnitTypeConfig unitType)
        {
            if (unitType == null)
            {
                return;
            }

            if (UnitNameText != null)
            {
                UnitNameText.text = unitType.unitTypeName;
            }

            if (GoldText != null)
            {
                GoldText.text = $"金钱 {unitType.goldCost}";
            }

            if (IndustryText != null)
            {
                IndustryText.text = $"工业 {unitType.industryCost}";
            }

            if (ScienceText != null)
            {
                ScienceText.text = $"科技 {unitType.scienceCost}";
            }

            if (UnitImage != null)
            {
                UnitImage.sprite = unitType.unitIcon;
            }
        }
    }
}

