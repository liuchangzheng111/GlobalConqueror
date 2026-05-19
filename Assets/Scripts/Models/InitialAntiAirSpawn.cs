using UnityEngine;

namespace GlobalConqueror.Models
{
    /// <summary>
    /// 开局防空标记 - 挂在 AntiAirsContainer 子物体上，地图初始化后写入对应格子的 <see cref="MapTileData.antiAir"/>。
    /// 子物体世界坐标会转换为 Tilemap 格子坐标（与 <see cref="InitialUnitSpawn"/> 相同）。
    /// </summary>
    public class InitialAntiAirSpawn : MonoBehaviour
    {
        [Tooltip("直接指定防空配置（优先于等级索引）")]
        public AntiAirConfig antiAirConfig;

        [Tooltip("使用 AntiAirManager.antiAir 列表中的等级（1=列表第1项）。仅当 antiAirConfig 为空时生效")]
        [Min(0)]
        public int antiAirLevelIndex;
    }
}
