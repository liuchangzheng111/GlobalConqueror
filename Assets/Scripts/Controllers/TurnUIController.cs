using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 回合制UI控制器 - 显示回合信息和资源
    /// </summary>
    public class TurnUIController : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI nationText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI industryText;
        [SerializeField] private TextMeshProUGUI scienceText;
        [SerializeField] private Button endTurnButton;

        private void Start()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnTurnStart += UpdateUI;
                NationManager.instance.OnNationTurnStart += UpdateNationUI;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            UpdateUI(1);
        }

        private void OnDestroy()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnTurnStart -= UpdateUI;
                NationManager.instance.OnNationTurnStart -= UpdateNationUI;
            }
        }

        private void Update()
        {
            if (NationManager.instance != null && NationManager.instance.CurrentNation != null)
            {
                UpdateResourceUI(NationManager.instance.CurrentNation);
            }
        }

        private void UpdateUI(int turn)
        {
            if (turnText != null)
            {
                turnText.text = $"回合数 {turn}";
            }
        }

        private void UpdateNationUI(NationData nation)
        {
            if (nationText != null)
            {
                nationText.text = $"{nation.nationName}";
            }

            //if (endTurnButton != null)
            //{
            //    endTurnButton.interactable = nation.isPlayer;
            //}

            UpdateResourceUI(nation);
        }

        private void UpdateResourceUI(NationData nation)
        {
            if (goldText != null)
            {
                goldText.text = $"金钱 {nation.gold}";
            }

            if (industryText != null)
            {
                industryText.text = $"工业 {nation.industry}";
            }

            if (scienceText != null)
            {
                scienceText.text = $"科技 {nation.science}";
            }
        }

        private void OnEndTurnClicked()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.EndTurn();
            }
        }
    }
}
