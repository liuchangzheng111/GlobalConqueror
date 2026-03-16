using UnityEngine;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 兵种配置 - ScriptableObject，可在编辑器中配置多种兵种
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitType", menuName = "GlobalConqueror/Unit Type Config")]
    public class UnitTypeConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string unitTypeName = "步兵";
        public Sprite unitIcon;

        [Header("行动力与范围")]
        public int movementRange = 2;
        public int attackRange = 1;

        [Header("战斗属性")]
        public int attackStrength = 10;
        public int health = 10;

        [Header("购买消耗")]
        public int goldCost = 100;
        public int industryCost = 50;
        public int scienceCost = 0;

        [Header("地形通行")]
        [Tooltip("平原与城市移动消耗")]
        public int plainAndCityMoveCost = 1; 
        [Tooltip("山地移动消耗")]
        public int mountainMoveCost = 2;
        [Tooltip("森林移动消耗")]
        public int forestMoveCost = 1;
        [Tooltip("水域移动消耗")]
        public int waterMoveCost = 2;
    }
}
