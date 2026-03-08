using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using GlobalConqueror.Managers;

namespace GlobalConqueror.Editor
{
    /// <summary>
    /// MapManager的编辑器扩展
    /// </summary>
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapManager mapManager = (MapManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("调试工具", EditorStyles.boldLabel);

            if (GUILayout.Button("重新初始化地图"))
            {
                mapManager.SendMessage("InitializeMap", SendMessageOptions.DontRequireReceiver);
            }

            // Tilemap信息显示
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tilemap信息", EditorStyles.boldLabel);
            
            Tilemap tilemap = FindObjectOfType<Tilemap>();
            if (tilemap != null)
            {
                EditorGUILayout.LabelField($"找到Tilemap: {tilemap.name}");
                EditorGUILayout.LabelField($"Tile数量: {GetTileCount(tilemap)}");
                
                Grid grid = tilemap.layoutGrid;
                if (grid != null)
                {
                    EditorGUILayout.LabelField($"Grid布局: {grid.cellLayout}");
                    EditorGUILayout.LabelField($"Cell大小: {grid.cellSize}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未找到Tilemap！请确保场景中有Tilemap组件。", MessageType.Warning);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);
                
                var selectedCoord = mapManager.SelectedTileCoordinate;
                if (selectedCoord.HasValue)
                {
                    EditorGUILayout.LabelField($"选中坐标: {selectedCoord.Value}");
                }
                else
                {
                    EditorGUILayout.LabelField("未选中地块");
                }
            }
        }

        private int GetTileCount(Tilemap tilemap)
        {
            if (tilemap == null) return 0;
            
            int count = 0;
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (tilemap.GetTile(pos) != null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
