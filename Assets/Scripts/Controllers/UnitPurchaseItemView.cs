using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单位购买项视图 - 挂在单位购买项预制体上，绑定单位信息。
    /// </summary>
    public class UnitPurchaseItemView : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI UnitNameText;
        [SerializeField] private TextMeshProUGUI GoldText;
        [SerializeField] private TextMeshProUGUI IndustryText;
        [SerializeField] private TextMeshProUGUI ScienceText;
        [SerializeField] private TextMeshProUGUI HealthText;
        [SerializeField] private TextMeshProUGUI DescriptionText;
        [SerializeField] private Image ProduceConditionImage;
        [SerializeField] private Image UnitImage;
        [SerializeField] private Color BackgroundColor;

        /// <summary>
        /// 使用指定单位数据刷新 UI
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

            if (HealthText != null)
            {
                HealthText.text = $"生命值 {unitType.health}";
            }

            if (DescriptionText != null)
            {
                DescriptionText.text = $"{unitType.description}";
            }

            if (ProduceConditionImage != null && UnitManager.instance != null)
            {
                ProduceConditionImage.enabled = unitType.unitProperty != UnitProperty.Fort;
                ProduceConditionImage.sprite = UnitManager.instance.GetUnitProduceConditionSprite(unitType);
            }

            if (UnitImage != null)
            {
                UnitImage.sprite = unitType.unitIcon;
            }
        }
    }
}

