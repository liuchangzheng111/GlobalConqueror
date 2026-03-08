using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// Tile类型映射 - 用于自定义Tile到TileType的映射关系
    /// </summary>
    [System.Serializable]
    public class TileTypeMapping
    {
        [Tooltip("Unity Tile对象")]
        public TileBase tile;
        
        [Tooltip("对应的地块类型")]
        public TileType tileType;

        /// <summary>
        /// 创建自定义TileType映射（可以通过ScriptableObject配置）
        /// </summary>
        public static TileType GetTileTypeFromCustomMapping(TileBase tile, System.Collections.Generic.Dictionary<TileBase, TileType> customMapping)
        {
            if (customMapping != null && customMapping.ContainsKey(tile))
            {
                return customMapping[tile];
            }

            return TileType.Plain;
        }
    }
}
