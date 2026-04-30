using UnityEngine;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 地块类型枚举
    /// </summary>
    public enum TileType
    {
        Plain,      // 平原
        Mountain,   // 山地
        Forest,     // 森林
        Water,      // 海洋
        City,       // 城市
        Port        // 港口
    }

    /// <summary>
    /// 地块数据模型
    /// </summary>
    [System.Serializable]
    public class MapTileData
    {
        public TileBase tile;             // 瓦片
        public TileType tileType;         // 地块类型
        public int ownerId;               // 所有者ID（-1表示无主）
        public int defenseBonus;          // 防御加成
        public int resourceProduction;    // 资源产量
        public int antiAirLevel;          // 防空等级（0无，1机枪，2防空炮，3防空导弹）

        public MapTileData(TileBase _tile, TileType type)
        {
            tile = _tile;
            tileType = type;
            ownerId = -1;
            defenseBonus = 0;
            resourceProduction = 0;
            antiAirLevel = 0;
        }

        public bool IsOwned => ownerId >= 0;
    }
}
