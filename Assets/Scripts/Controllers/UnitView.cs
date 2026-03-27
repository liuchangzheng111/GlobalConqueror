using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 单位视图 - 挂在单位预制体上，绑定国旗、血条等显示。
    /// 在 Inspector 中拖好 Flag Image、Health Bar 等引用后，由 UnitController 调用 Setup 绑定数据。
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [Header("国旗")]
        [SerializeField] private Image flagImage;

        [Header("血条")]
        [SerializeField] private Slider healthSlider;

        private UnitData _boundUnit;

        private void Awake()
        {
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = 1f; 
                healthSlider.interactable = false; 
            }

            if (flagImage != null)
            {
                flagImage.enabled = false;
            }
        }

        /// <summary>
        /// 绑定单位数据并刷新国旗、血条显示
        /// </summary>
        public void Setup(UnitData unit)
        {
            _boundUnit = unit;

            ResetUI();

            if (unit == null)
            {
                Debug.LogWarning($"UnitView: 绑定空单位数据，UI已重置（物体：{gameObject.name}）");
                return;
            }

            RefreshFlag();
            RefreshHealthBar();
        }

        /// <summary>
        /// 重置所有UI（隐藏国旗、血条）
        /// </summary>
        private void ResetUI()
        {
            if (flagImage != null)
            {
                flagImage.enabled = false;
                flagImage.sprite = null;
            }

            if (healthSlider != null)
            {
                healthSlider.value = 1f;
                healthSlider.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// 刷新血条
        /// </summary>
        public void RefreshHealthBar()
        {
            if (healthSlider != null)
            {
                if (_boundUnit == null)
                {
                    healthSlider.gameObject.SetActive(false);
                    return;
                }

                int maxHealth = _boundUnit.maxHealth;
                int currentHealth = _boundUnit.currentHealth;

                if (maxHealth <= 0)
                {
                    healthSlider.gameObject.SetActive(false);
                    return;
                }

                float fillRatio = Mathf.Clamp01((float)currentHealth / maxHealth);

                healthSlider.value = fillRatio;
                healthSlider.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("UnitView: 未绑定healthSlider！");
            }
        }

        private void RefreshFlag()
        {
            if (flagImage != null)
            {
                if (_boundUnit == null)
                {
                    healthSlider.gameObject.SetActive(false);
                    return;
                }

                if (_boundUnit.ownerNationId < 0)
                {
                    Debug.LogError("UnitView: 未绑定国家！");
                    return;
                }
                NationData nation = NationManager.instance != null ? NationManager.instance.GetNation(_boundUnit.ownerNationId) : null;
                if (nation == null)
                {
                    Debug.LogError("UnitView: 不存在此国家！");
                    return;
                }
                Sprite flagSprite = nation.nationFlag;

                if (flagSprite != null)
                {
                    flagImage.enabled = true;
                    flagImage.sprite = flagSprite;
                }
                else
                {
                    Debug.LogError("UnitView: 此国家不存在国旗！");
                    return;
                }
            }
            else
            {
                Debug.LogError("UnitView: 未绑定flagImage！");
            }
        }
    }
}
