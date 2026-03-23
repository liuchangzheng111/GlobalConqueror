using UnityEngine;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 军队单位数据模型
    /// </summary>
    [System.Serializable]
    public class UnitData
    {
        public int unitId;
        public UnitTypeConfig unitType;
        public Vector3Int position;
        public int ownerNationId;

        /// <summary>当前生命值，用于血条显示与战斗扣血</summary>
        public int currentHealth;

        /// <summary>
        /// 本回合是否已移动
        /// </summary>
        public bool hasMovedThisTurn;

        /// <summary>
        /// 本回合是否已攻击
        /// </summary>
        public bool hasAttackedThisTurn;

        /// <summary>最大生命值（来自兵种配置）</summary>
        public int MaxHealth => unitType != null ? unitType.health : 1;

        /// <summary>当前生命值比例</summary>
        public float HealthRate => (float)currentHealth / (float)MaxHealth;

        public UnitData(int id, UnitTypeConfig type, Vector3Int pos, int ownerId)
        {
            unitId = id;
            unitType = type;
            position = pos;
            ownerNationId = ownerId;
            currentHealth = type != null ? type.health : 1;
            hasMovedThisTurn = false;
            hasAttackedThisTurn = false;
        }

        public float MovementRange => unitType != null ? unitType.movementRange : 0;
        public int AttackRange => unitType != null ? unitType.attackRange : 0;

        public override string ToString()
        {
            string typeName = unitType != null ? unitType.unitTypeName : "未知";
            return $"单位ID:{unitId} | {typeName} | 位置:{position} | 所属:{ownerNationId}";
        }

        public string GetUnitPropertyString()
        {
            string result = "";
            switch (unitType.unitProperty)
            {
                case UnitProperty.Soldier:
                    result = "步兵单位";
                    break;
                case UnitProperty.Armor:
                    result = "装甲单位";
                    break;
                case UnitProperty.Fort:
                    result = "堡垒单位";
                    break;
                case UnitProperty.Warship:
                    result = "轻型舰艇单位";
                    break;
                case UnitProperty.Battleship:
                    result = "重型舰艇单位";
                    break;
                default:
                    result = "";
                    break;
            }
            return result;
        }
    }

    public enum UnitProperty { 
        Soldier,
        Armor,
        Fort,
        Warship,
        Battleship
    }
}
