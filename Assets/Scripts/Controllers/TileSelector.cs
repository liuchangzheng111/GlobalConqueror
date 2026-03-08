using UnityEngine;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using UnityEngine.Tilemaps;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 地块选择器 - 处理地块点击选中和高亮功能
    /// </summary>
    public class TileSelector : MonoBehaviour
    {
        [Header("高亮设置")]
        [SerializeField] private GameObject highlightPrefab; // 高亮预制体（可选）
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.5f); // 高亮颜色
        [SerializeField] private float highlightHeight = 0.1f; // 高亮高度偏移

        [Header("调试显示")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float debugTextHeight = 2f;

        private Camera mainCamera;
        private GameObject currentHighlight;
        private Vector3Int? lastSelectedCoordinate = null;

        // 用于存储每个地块的高亮对象（如果使用预制体）
        private System.Collections.Generic.Dictionary<Vector3Int, GameObject> highlightObjects = 
            new System.Collections.Generic.Dictionary<Vector3Int, GameObject>();

        private void Awake()
        {          
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        private void Update()
        {
            HandleTileSelection();
            UpdateHighlight();
        }

        /// <summary>
        /// 处理地块选择
        /// </summary>
        private void HandleTileSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = Input.mousePosition;
                worldPos = Camera.main.ScreenToWorldPoint(worldPos);
                Vector3Int cellPos = MapManager.instance.Tilemap.WorldToCell(worldPos);

                if (cellPos != null && MapManager.instance.IsCoordinateValid(cellPos))
                    SelectTile(cellPos);
            }
        }

        /// <summary>
        /// 选择地块
        /// </summary>
        private void SelectTile(Vector3Int coordinate)
        {
            lastSelectedCoordinate = coordinate;
            MapManager.instance.SetSelectedTile(coordinate);

            MapTileData tileData = MapManager.instance.GetTileData(coordinate);
            if (tileData != null)
            {
                Debug.Log($"选中地块: {coordinate} | 类型: {tileData.tileType} | 所有者: {tileData.ownerId}");
            }
            else
            {
                Debug.Log($"选中地块: {coordinate}");
            }
        }

        /// <summary>
        /// 更新高亮显示
        /// </summary>
        private void UpdateHighlight()
        {
            Vector3Int? selectedCoord = MapManager.instance.SelectedTileCoordinate;

            // 如果选中了新的地块，更新高亮
            if (selectedCoord.HasValue && selectedCoord != lastSelectedCoordinate)
            {
                ClearHighlight();
                ShowHighlight(selectedCoord.Value);
                lastSelectedCoordinate = selectedCoord;
            }
            else if (!selectedCoord.HasValue && lastSelectedCoordinate.HasValue)
            {
                ClearHighlight();
                lastSelectedCoordinate = null;
            }
        }

        /// <summary>
        /// 显示高亮
        /// </summary>
        private void ShowHighlight(Vector3Int coordinate)
        {
            if (highlightPrefab != null)
            {
                // 使用预制体高亮
                if (!highlightObjects.ContainsKey(coordinate))
                {
                    Vector3 worldPos = MapManager.instance.Tilemap.GetCellCenterWorld(coordinate);
                    worldPos.y = highlightHeight;
                    currentHighlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity);
                    highlightObjects[coordinate] = currentHighlight;
                }
                else
                {
                    currentHighlight = highlightObjects[coordinate];
                    currentHighlight.SetActive(true);
                }
            }
            else
            {
                // 使用Gizmos高亮（在OnDrawGizmos中绘制）
                // 这里可以添加其他高亮方式，比如改变材质颜色等
            }
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        private void ClearHighlight()
        {
            if (currentHighlight != null)
            {
                if (highlightPrefab != null)
                {
                    currentHighlight.SetActive(false);
                }
                else
                {
                    Destroy(currentHighlight);
                }
                currentHighlight = null;
            }
        }
    }
}
