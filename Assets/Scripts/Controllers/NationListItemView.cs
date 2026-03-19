using GlobalConqueror.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单个国家信息条目视图
    /// </summary>
    public class NationListItemView : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI nationNameText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image NationFlag;
        [SerializeField] private Image nationColorStrip;
        [SerializeField] private Color defeatedColor;

        /// <summary>
        /// 使用指定国家数据刷新 UI
        /// </summary>
        public void Setup(NationData nation)
        {
            if (nation == null)
            {
                return;
            }

            int cityCount = nation.ownedCitiesNames != null ? nation.ownedCitiesNames.Count : 0;

            if (nationNameText != null)
            {
                nationNameText.text = nation.nationName;
            }

            if (detailText != null)
            {
                if (!nation.isDefeated)
                {
                    string capitalName = string.IsNullOrEmpty(nation.capital) ? "无" : nation.capital;
                    detailText.text =
                        $"城市数 {cityCount}  首都 {capitalName}\n" +
                        $"金钱 {nation.gold}  工业 {nation.industry}  科技 {nation.science}\n";
                }
                else
                {
                    detailText.text = "";
                }
            }

            if (statusText != null)
            {
                if (nation.isDefeated)
                {
                    statusText.text = "已灭亡";
                    statusText.color = Color.black;
                    nationColorStrip.color = defeatedColor;
                }
                else
                {
                    statusText.text = "存续中";
                }
            }

            if (nationColorStrip != null)
            {
                if (nation.isDefeated)
                {
                    nationColorStrip.color = new Color(0, 0, 0, 0.2f);
                }
                else
                {
                    nationColorStrip.color = new Color(nation.nationColor.r, nation.nationColor.g, nation.nationColor.b, 0.2f);
                }
            }

            if (NationFlag != null)
            {
                NationFlag.sprite = nation.nationFlag;
            }
        }
    }
}

