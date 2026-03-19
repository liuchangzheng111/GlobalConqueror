using System;
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
        /// 六边形下两格之间的步数（六边形距离）
        /// </summary>
        /// <param name="a">第一个六边形格子坐标</param>
        /// <param name="b">第二个六边形格子坐标</param>
        /// <returns>两个格子之间的最小步数（六边形距离）</returns>
        public static int GetHexDistance(Vector3Int a, Vector3Int b)
        {
            var cubeA = ConvertOffsetToCube(a);
            var cubeB = ConvertOffsetToCube(b);

            int dx = Mathf.Abs(cubeA.x - cubeB.x);
            int dy = Mathf.Abs(cubeA.y - cubeB.y);
            int dz = Mathf.Abs(cubeA.z - cubeB.z);

            int distance = (dx + dy + dz) / 2;

            return distance;
        }

        /// <summary>
        /// 辅助函数：将尖顶六边形的偏移坐标（Offset）转换为立方体坐标（Cube）
        /// </summary>
        /// <param name="cell">偏移坐标（x=行，y=列，z=0）</param>
        /// <returns>立方体坐标 (x, y, z)，满足 x+y+z=0</returns>
        private static (int x, int y, int z) ConvertOffsetToCube(Vector3Int cell)
        {  
            int cubeX = cell.x - (cell.y - (cell.y & 1)) / 2;
            int cubeY = cell.y; 
            int cubeZ = -cubeX - cubeY;

            return (cubeX, cubeY, cubeZ);
        }

        /// <summary>
        /// 辅助函数：立方体坐标转偏移坐标（适配偶数列尖顶六边形）
        /// </summary>
        private static Vector3Int ConvertCubeToOffset((int x, int y, int z) cube)
        {
            int x = cube.x + (cube.y - (cube.y & 1)) / 2;
            int y = cube.y;
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 获取与 center 六边形距离不超过 maxDistance 的所有格子（不校验地图边界）
        /// </summary>
        public static HashSet<Vector3Int> GetCellsWithinHexDistance(Vector3Int center, int maxDistance)
        {
            if (maxDistance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDistance), "最大距离不能为负数");
            }

            var result = new HashSet<Vector3Int>();
            var centerCube = ConvertOffsetToCube(center);

            // 遍历立方体坐标的六边形范围
            for (int x = -maxDistance; x <= maxDistance; x++)
            {
                for (int y = Math.Max(-maxDistance, -x - maxDistance); y <= Math.Min(maxDistance, -x + maxDistance); y++)
                {
                    int z = -x - y;
                    var offsetCell = ConvertCubeToOffset((centerCube.x + x, centerCube.y + y, centerCube.z + z));
                    result.Add(offsetCell);
                }
            }

            return result;
        }
    }
}
