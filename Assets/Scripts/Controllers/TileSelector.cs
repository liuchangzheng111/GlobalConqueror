using UnityEngine;
using GlobalConqueror.Models;
using GlobalConqueror.Managers;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

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

        private Camera mainCamera;
        private GameObject currentHighlight;
        private Vector3Int? lastSelectedCoordinate = null;
        private Vector3 startMousePosition;

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
        }

        /// <summary>
        /// 处理地块选择
        /// </summary>
        private void HandleTileSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    startMousePosition = Vector3.zero;
                    return;
                }

                startMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (startMousePosition == Vector3.zero)
                    return;

                float moveDistance = Vector3.Distance(Input.mousePosition, startMousePosition);

                if (moveDistance > 5f)
                    return;

                Vector3 worldPos = Input.mousePosition;
                worldPos = Camera.main.ScreenToWorldPoint(worldPos);
                worldPos.z = 0;
                Vector3Int cellPos = MapManager.instance.Tilemap.WorldToCell(worldPos);

                if (MapManager.instance.IsCoordinateValid(cellPos))
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
            
            UpdateHighlight(coordinate);
        }

        /// <summary>
        /// 更新高亮显示
        /// </summary>
        private void UpdateHighlight(Vector3Int selectedCoord)
        {
            Vector3 worldPos = MapManager.instance.Tilemap.CellToWorld(selectedCoord);
            if (currentHighlight == null)
            {
                currentHighlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity);
            }
            else
                currentHighlight.transform.position = worldPos;
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
