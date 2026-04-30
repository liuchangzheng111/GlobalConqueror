using UnityEngine;

namespace GlobalConqueror.Models
{
    public enum AirMissionType
    {
        AttackTarget,
        ParadropInfantry
    }

    [CreateAssetMenu(fileName = "NewAirMission", menuName = "GlobalConqueror/Air Mission Config")]
    public class AirMissionConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string missionName;
        public Sprite icon;
        [Multiline(4)]
        public string description;

        [Header("消耗（可选）")]
        public int goldCost;
        public int industryCost;
        public int scienceCost;

        [Header("任务类型")]
        public AirMissionType type;

        [Header("航程（六边形距离）")]
        [Min(0)]
        public int range = 0;

        [Header("使用条件（机场最低等级）")]
        [Min(0)]
        public int airportLevel = 0;

        [Header("伤害")]
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

        [Tooltip("空投生成的步兵类型")]
        public UnitTypeConfig paradropInfantryType;
    }
}

