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
        public int maxHealth;

        [Header("建造状态（用于堡垒等）")]
        public bool isUnderConstruction = false;
        public int constructionTurnsRemaining = 0;

        /// <summary>当前生命值比例</summary>
        public float HealthRate => (float)currentHealth / (float)maxHealth;

        public UnitData(int id, UnitTypeConfig type, Vector3Int pos, int ownerId)
        {
            unitId = id;
            unitType = type;
            position = pos;
            ownerNationId = ownerId;
            currentHealth = type != null ? type.health : 1;
            hasMovedThisTurn = false;
            hasAttackedThisTurn = false;

            maxHealth = type.health;
        }

        public float MovementRange => unitType != null ? unitType.movementRange : 0;
        public int AttackRange => unitType != null ? unitType.attackRange : 0;

        public override string ToString()
        {
            string typeName = unitType != null ? unitType.unitTypeName : "未知";
            return $"单位ID:{unitId} | {typeName} | 位置:{position} | 所属:{ownerNationId}";
        }

        public static string GetUnitPropertyString(UnitTypeConfig unitType)
        {
            string result = unitType.unitProperty switch
            {
                UnitProperty.Soldier => "步兵单位",
                UnitProperty.Armor => "装甲单位",
                UnitProperty.Fort => "堡垒单位",
                UnitProperty.Warship => "轻型舰艇单位",
                UnitProperty.Battleship => "重型舰艇单位",
                _ => "",
            };
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
