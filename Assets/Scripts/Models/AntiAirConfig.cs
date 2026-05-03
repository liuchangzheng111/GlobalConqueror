using UnityEngine;

namespace GlobalConqueror.Models
{
    [CreateAssetMenu(fileName = "NewAntiAir", menuName = "GlobalConqueror/AntiAir Config")]
    public class AntiAirConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string antiairName;
        public Sprite icon;

        [Header("地块防空显示")]
        public Sprite tileIcon;

        [Multiline(4)]
        public string description;

        [Header("建造消耗")]
        public int goldCost;
        public int industryCost;
        public int scienceCost;

        [Header("对空投单位伤害")]
        public int paradropDamage;

        [Header("对空袭减伤倍率")]
        [Range(0, 1)]
        public float airStrikeDamageMultiplier = 1;
    }
}

