using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 浮动伤害数字 - 挂在预制体上，由 FloatingDamageManager 实例化后调用 Play。
    /// 预制体建议：世界空间下带 TextMeshPro（或 TMP + Canvas World Space），本物体为根。
    /// </summary>
    public class FloatingDamageText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMeshWorld;
        [SerializeField] private TextMeshProUGUI textMeshUI;

        [SerializeField] private float riseDistance = 1.2f;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        private readonly Tween _tween;

        private void OnDestroy()
        {
            _tween?.Kill();
        }

        /// <summary>
        /// 在世界坐标 position 处播放浮动伤害（相对当前 transform.position 上移）
        /// </summary>
        public void Play(int damage, Color color)
        {
            string display = $"{damage}";

            if (textMeshWorld != null)
            {
                textMeshWorld.text = display;
                textMeshWorld.color = color;
                AnimateRoot();
            }
            else if (textMeshUI != null)
            {
                textMeshUI.text = display;
                textMeshUI.color = color;
                AnimateRoot();
            }
            else
            {
                Debug.LogWarning("FloatingDamageText: 未绑定 TextMeshPro 或 TextMeshProUGUI");
                Destroy(gameObject);
            }
        }

        private void AnimateRoot()
        {
            Vector3 endPos = transform.position + Vector3.up * riseDistance;
            _tween?.Kill();

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(endPos, duration).SetEase(moveEase));
            seq.OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
