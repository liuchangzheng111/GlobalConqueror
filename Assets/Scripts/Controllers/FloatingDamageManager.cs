using UnityEngine;
using GlobalConqueror.Managers;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 浮动伤害管理器 - 场景中放一个，拖入浮动文字预制体。
    /// </summary>
    public class FloatingDamageManager : MonoBehaviour
    {
        public static FloatingDamageManager instance;

        [Header("预制体需挂 FloatingDamageText，并绑定 TMP 组件")]
        [SerializeField] private GameObject floatingDamagePrefab;

        [Header("颜色")]
        [SerializeField] private Color damageColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color counterDamageColor = new Color(1f, 0.6f, 0.2f, 1f);

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 在地图格子中心显示伤害数字
        /// </summary>
        public void ShowDamageAtCell(Vector3Int cell, int amount, Color? colorOverride = null)
        {
            if (floatingDamagePrefab == null || MapManager.instance?.Tilemap == null || amount <= 0)
                return;

            Vector3 world = MapManager.instance.Tilemap.GetCellCenterWorld(cell);
            world.z = 0f;

            GameObject go = Instantiate(floatingDamagePrefab, world, Quaternion.identity);
            var fd = go.GetComponent<FloatingDamageText>();
            if (fd != null)
            {
                Color c = colorOverride ?? damageColor;
                fd.Play(amount, c);
            }
            else
            {
                Destroy(go);
            }
        }

        /// <summary>
        /// 对防守方造成的伤害（通常为红色系）
        /// </summary>
        public void ShowDefenderDamage(Vector3Int defenderCell, int amount)
        {
            ShowDamageAtCell(defenderCell, amount, damageColor);
        }

        /// <summary>
        /// 反击对进攻方造成的伤害（通常为橙色系）
        /// </summary>
        public void ShowAttackerCounterDamage(Vector3Int attackerCell, int amount)
        {
            ShowDamageAtCell(attackerCell, amount, counterDamageColor);
        }
    }
}
