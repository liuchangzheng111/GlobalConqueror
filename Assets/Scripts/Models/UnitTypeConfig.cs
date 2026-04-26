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
        [Multiline(5)]
        public string description;

        [Header("行动力与范围")]
        public float movementRange = 2;
        public int attackRange = 1;

        [Header("战斗属性")]
        [Tooltip("对装甲单位")]
        public int attackStrength_Armor = 20;
        [Tooltip("对步兵单位")]
        public int attackStrength_Soldier = 20;
        [Tooltip("对堡垒单位")]
        public int attackStrength_Fort = 20;
        [Tooltip("对小型舰船单位")]
        public int attackStrength_Warship = 20;
        [Tooltip("对重型舰船单位")]
        public int attackStrength_Battleship = 20;

        public int health = 100;

        [Header("购买消耗")]
        public int goldCost = 100;
        public int industryCost = 50;
        public int scienceCost = 0;

        [Header("单位类别")]
        public UnitProperty unitProperty = UnitProperty.Armor;

        [Header("建造阶段图标（Fort 专用，可选）")]
        [Tooltip("建造剩余 3 回合时显示的图标")]
        public Sprite constructionIconTurn3;
        [Tooltip("建造剩余 2 回合时显示的图标")]
        public Sprite constructionIconTurn2;
        [Tooltip("建造剩余 1 回合时显示的图标")]
        public Sprite constructionIconTurn1;

        [Header("特殊规则")]
        [Tooltip("潜艇：与陆军单位互相不可攻击")]
        public bool isSubmarine = false;
        [Tooltip("是否不可反击（由战斗逻辑决定）")]
        public bool cannotBeReversed = false;

        [Header("生产条件")]
        public int produceCondition;

        [Header("地形通行")]
        [Tooltip("平原与城市移动消耗")]
        public float plainAndCityMoveCost = 1; 
        [Tooltip("山地移动消耗")]
        public float mountainMoveCost = 2;
        [Tooltip("森林移动消耗")]
        public float forestMoveCost = 1;
        [Tooltip("水域移动消耗")]
        public float waterMoveCost = 2;
    }
}
