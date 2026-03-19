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
        [SerializeField] private TextMeshProUGUI DetailText;
        [SerializeField] private Image FlagImage;
        [SerializeField] private Button endTurnButton;

        private void Start()
        {
            StartCoroutine(BindWhenNationManagerReady());
        }

        /// <summary>
        /// 等待 NationManager 初始化完成后再绑定事件，避免脚本执行顺序问题
        /// </summary>
        private System.Collections.IEnumerator BindWhenNationManagerReady()
        {
            while (NationManager.instance == null)
            {
                yield return null;
            }

            NationManager.instance.OnTurnStart += UpdateUI;
            NationManager.instance.OnNationTurnStart += (nationData) => UpdateNationUI(nationData.nationId);

            if (UnitManager.instance != null)
            {
                yield return null;
            }
            
            UnitManager.instance.OnCityCaptured += (unitData, cityData) => UpdateDetailUI(unitData.ownerNationId);
            UnitManager.instance.OnUnitSpawned += (unitData, gameObject) => UpdateNationUI(unitData.ownerNationId);

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            // 初始化 UI（若此时已经在某个回合中，可根据实际需要读取当前 turn）
            UpdateUI(NationManager.instance.CurrentTurn > 0 ? NationManager.instance.CurrentTurn : 1);
        }

        private void OnDestroy()
        {
            if (NationManager.instance != null)
            {
                NationManager.instance.OnTurnStart -= UpdateUI;
                NationManager.instance.OnNationTurnStart -= (nationData) => UpdateNationUI(nationData.nationId);
            }

            if (UnitManager.instance != null)
            {
                UnitManager.instance.OnCityCaptured -= (unitData, cityData) => UpdateDetailUI(unitData.ownerNationId);
                UnitManager.instance.OnUnitSpawned -= (unitData, gameObject) => UpdateNationUI(unitData.ownerNationId);
            }
        }

        private void Update()
        {
        }

        private void UpdateUI(int turn)
        {
            if (turnText != null)
            {
                turnText.text = $"回合数 {turn}";
            }
        }

        private void UpdateNationUI(int nationId)
        {
            NationData nation = null;
            if (NationManager.instance != null)
            {
                nation = NationManager.instance.GetNation(nationId);
            }

            if (nationText != null)
            {
                nationText.text = $"{nation.nationName}";
            }

            //if (endTurnButton != null)
            //{
            //    endTurnButton.interactable = nation.isPlayer;
            //}

            if (FlagImage != null && nation.nationFlag != null)
            {
                FlagImage.sprite = nation.nationFlag;
            }

            UpdateResourceUI(nation);
            UpdateDetailUI(nation.nationId);
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

        private void UpdateDetailUI(int nationId)
        {
            NationData nationData = null;
            if (NationManager.instance != null)
            {
                nationData = NationManager.instance.GetNation(nationId);
            }
            if (nationData != null && DetailText != null && UnitManager.instance != null)
            {
                string capitalName = string.IsNullOrEmpty(nationData.capital) ? "无" : nationData.capital;
                DetailText.text = $"首都 {capitalName}\n" + $"城市数 {nationData.ownedCitiesNames.Count}\n" + $"部队数 {UnitManager.instance.GetUnitsByNation(nationData.nationId).Count}";
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
