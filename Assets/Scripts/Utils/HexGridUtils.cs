using System.Collections.Generic;
using UnityEngine;

namespace GlobalConqueror.Utils
{
    /// <summary>
    /// 尖顶六边形格子工具（Offset 坐标，Unity Grid Hexagon Point Top）
    /// </summary>
    public static class HexGridUtils
    {
        /// <summary>
        /// 获取尖顶六边形的 6 个邻格（Offset 坐标）
        /// 偶数列 (x % 2 == 0): 右上、右、右下、左下、左、左上
        /// 奇数列 (x % 2 != 0): 右上、右、右下、左下、左、左上（y 偏移与偶数列不同）
        /// </summary>
        public static List<Vector3Int> GetPointyTopNeighbors(Vector3Int cell)
        {
            int x = cell.x;
            int y = cell.y;
            bool evenColumn = (y & 1) == 0;

            var list = new List<Vector3Int>(6);
            if (evenColumn)
            {
                list.Add(new Vector3Int(x, y + 1, 0));   // 右上
                list.Add(new Vector3Int(x, y - 1, 0));   // 右下
                list.Add(new Vector3Int(x + 1, y, 0)); // 右
                list.Add(new Vector3Int(x - 1, y + 1, 0));    // 左上
                list.Add(new Vector3Int(x - 1, y - 1, 0)); // 左下
                list.Add(new Vector3Int(x - 1, y, 0));    // 左
            }
            else
            {
                list.Add(new Vector3Int(x + 1, y + 1, 0));   // 右上
                list.Add(new Vector3Int(x + 1, y - 1, 0));   // 右下
                list.Add(new Vector3Int(x + 1, y, 0));    // 右
                list.Add(new Vector3Int(x, y + 1, 0)); // 左上
                list.Add(new Vector3Int(x, y - 1, 0));   // 左下
                list.Add(new Vector3Int(x - 1, y, 0)); // 左
            }
            return list;
        }

        /// <summary>
        /// 尖顶六边形下两格之间的步数（六边形距离）
        /// </summary>
        public static int GetHexDistance(Vector3Int a, Vector3Int b)
        {
            int q1 = a.x;
            int r1 = a.y - (a.x - (a.x & 1)) / 2;

            int q2 = b.x;
            int r2 = b.y - (b.x - (b.x & 1)) / 2;

            int s1 = -q1 - r1;
            int s2 = -q2 - r2;

            return (Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2) + Mathf.Abs(s1 - s2)) / 2;
        }

        /// <summary>
        /// 获取与 center 六边形距离不超过 maxDistance 的所有格子（不校验地图边界）
        /// </summary>
        public static HashSet<Vector3Int> GetCellsWithinHexDistance(Vector3Int center, int maxDistance)
        {
            var set = new HashSet<Vector3Int>();
            int x0 = center.x, y0 = center.y;
            for (int dx = -maxDistance; dx <= maxDistance; dx++)
            {
                for (int dy = -maxDistance; dy <= maxDistance; dy++)
                {
                    var cell = new Vector3Int(x0 + dx, y0 + dy, 0);
                    if (GetHexDistance(center, cell) <= maxDistance)
                        set.Add(cell);
                }
            }
            return set;
        }
    }
}
