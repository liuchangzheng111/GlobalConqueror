using GlobalConqueror.Models;

using UnityEngine;

/// <summary>
/// 开局单位标记 - 挂在 UnitsContainer 子物体上，用于从场景初始化已有单位。
/// 子物体世界坐标会被转换为 Tilemap 格子坐标作为单位出生位置。
/// </summary>
public class InitialUnitSpawn : MonoBehaviour
{
    [Tooltip("兵种类型")]
    public UnitTypeConfig unitType;

    [Tooltip("所属国家名称（与 NationData.nationName 一致）。空字符串时根据所在地块的所属国家自动识别（MapTileData.ownerId；城市格在国家初始化时已设好）")]
    public string ownerNationName = "";

}