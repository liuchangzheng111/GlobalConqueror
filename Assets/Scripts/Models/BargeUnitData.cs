using UnityEngine;

namespace GlobalConqueror.Models
{
	/// <summary>
	/// 驳船单位数据模型
	/// </summary>
	[System.Serializable]
	public class BargeUnitMapping : MonoBehaviour
	{
		[Tooltip("驳船兵种类型")]
		public UnitTypeConfig bargeUnitType;

        [Tooltip("（初始化时填）驳船中陆地兵种类型")]
        public UnitTypeConfig landUnitType;

        [Tooltip("陆地兵种图像引用")]
        public SpriteRenderer landUnitSprite;

		void Start ()
		{
			if (landUnitSprite != null && landUnitType != null)
			{
                landUnitSprite.sprite = landUnitType.unitIcon;
            }
		}
    }
}

