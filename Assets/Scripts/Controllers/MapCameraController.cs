using UnityEngine;
using GlobalConqueror.Managers;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace GlobalConqueror.Controllers
{
    /// <summary>
    /// 地图相机控制器 - 处理地图拖拽和缩放功能
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraController : MonoBehaviour
    {
        [Header("拖拽设置")]
        [SerializeField] private bool enableDrag = true;
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private Vector2 dragBoundsMin = new(-50, -50);
        [SerializeField] private Vector2 dragBoundsMax = new(50, 50);

        [Header("缩放设置")]
        [SerializeField] private bool enableZoom = true;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 10f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float zoomSensitivity = 1f;

        [Header("平滑设置")]
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothTime = 0.2f;
        
        [Header("自动初始化")]
        [SerializeField] private float initialZoomRatio = 0.8f; // 初始缩放比例（0.8表示显示80%的地图）
        [SerializeField] private float minZoomRatio = 1f; // 最小缩放比例（能看到100%的地图）
        [SerializeField] private float maxZoomRatio = 0.1f; // 最大缩放比例（能看到10%的地图，即放大10倍）
        [Tooltip("缩放到接近「看全图」时，改用地图中心作为缩放锚点，避免滚轮把画面带偏")]
        [SerializeField] [Range(0.02f, 0.25f)] private float minZoomAnchorBlendRange = 0.08f;

        private Camera mapCamera;
        private Vector3 lastMousePosition;
        private bool isDragging = false;
        private float targetZoom;
        private Vector3 targetPosition;
        private Bounds originalMapBounds;
        private Vector3 _positionVelocity;
        private float _zoomVelocity;

        private void Awake()
        {
            mapCamera = GetComponent<Camera>();
            if (mapCamera == null)
            {
                Debug.LogError("MapCameraController: 相机组件获取失败！", this);
                enabled = false;
                return;
            }

            // 初始化目标值为当前值
            targetZoom = mapCamera.orthographicSize;
            targetPosition = transform.position;
        }     
        
        private void OnDisable()
        {
            isDragging = false; // 重置拖拽状态
        }
        
        private void Start()
        {
            // 如果地图已经初始化，立即初始化相机
            if (MapManager.instance.InitializeMapCompleted)
            {
                InitializeFromMapBounds(MapManager.instance.MapBounds);
            }
        }

        private void Update()
        {
            HandleDrag();
            HandleZoom();
            ApplySmoothMovement();
        }


        private void OnDestroy()
        {
        }

        /// <summary>
        /// 从地图边界初始化相机参数
        /// </summary>
        public void InitializeFromMapBounds(Bounds mapBounds)
        {
            if (mapBounds.size.magnitude <= 0)
            {
                Debug.LogWarning("MapCameraController: 地图边界无效，无法初始化相机");
                return;
            }

            // 补偿单元格半尺寸
            Vector2 cellHalfSize = MapManager.instance.CellSize;
            originalMapBounds = new Bounds(
                mapBounds.center,
                new Vector3(
                    mapBounds.size.x + cellHalfSize.x * 2, // 宽度+1格
                    mapBounds.size.y + cellHalfSize.y * 2, // 高度+1格
                    mapBounds.size.z
                )
            );

            // 计算适合显示整个地图的相机大小
            float mapWidth = mapBounds.size.x;
            float mapHeight = mapBounds.size.y;
            float cameraAspect = mapCamera.aspect;

            float requiredHeight = mapHeight / 2f; 
            float requiredWidth = (mapWidth / 2f) / cameraAspect;

            float fitToMapSize = Mathf.Max(requiredHeight, requiredWidth);

            float initialSize = fitToMapSize * initialZoomRatio; 
            float calculatedMinZoom = fitToMapSize * minZoomRatio; 
            float calculatedMaxZoom = fitToMapSize * maxZoomRatio;

            if (calculatedMinZoom >= calculatedMaxZoom)
            {
                Debug.LogWarning("MapCameraController: 最大缩放≥最小缩放", this);
            }

            // 设置缩放限制
            minZoom = calculatedMinZoom;
            maxZoom = calculatedMaxZoom;

            // 设置初始缩放
            targetZoom = initialSize;
            mapCamera.orthographicSize = initialSize;

            Vector3 mapCenter = GetMapCenter();
            targetPosition = mapCenter;
            transform.position = mapCenter;
            _positionVelocity = Vector3.zero;
            _zoomVelocity = 0f;

            UpdateDynamicDragBounds();
            SnapPositionToMapCenterForLockedAxes();

            Debug.Log($"相机初始化完成:\n" +
                     $"  地图尺寸: {mapWidth}×{mapHeight}\n" +
                     $"  相机宽高比: {cameraAspect:F2}\n" +
                     $"  刚好装下地图的相机size: {fitToMapSize}\n" +
                     $"  初始缩放: {initialSize}\n" +
                     $"  缩放范围: {minZoom} ~ {maxZoom}\n" +
                     $"  拖拽边界: ({dragBoundsMin.x:F1}, {dragBoundsMin.y:F1}) ~ ({dragBoundsMax.x:F1}, {dragBoundsMax.y:F1})");
        }

        /// <summary>
        /// 处理拖拽功能
        /// </summary>
        private void HandleDrag()
        {
            if (!enableDrag) return;

            // 鼠标按下
            if (Input.GetMouseButtonDown(0))
            {
                // 点击UI时不触发拖拽
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    isDragging = false;
                    return;
                }
                lastMousePosition = Input.mousePosition;
                isDragging = true;
            }

            // 鼠标抬起
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            // 拖拽中
            if (isDragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                Vector3 moveDelta = 0.01f * mapCamera.orthographicSize * new Vector3(-delta.x * dragSpeed, -delta.y * dragSpeed, 0);
                
                targetPosition += moveDelta;
               
                lastMousePosition = Input.mousePosition;

                ClampCameraPosition();
            }
        }

        /// <summary>
        /// 处理缩放功能
        /// </summary>
        private void HandleZoom()
        {
            if (!enableZoom) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // 鼠标指针在UI上时不触发缩放
                bool isOverUI = IsPointerOverUIElement();
                if (isOverUI)
                {
                    // 仅在UI上时跳过缩放逻辑，无需修改isDragging（拖拽状态由拖拽逻辑管理）
                    return;
                }

                // 原有缩放逻辑
                float oldZoom = targetZoom;
                float speedMultiplier = Mathf.Lerp(0.5f, 2f, (targetZoom - minZoom) / (maxZoom - minZoom));
                targetZoom -= scroll * zoomSpeed * zoomSensitivity * speedMultiplier;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

                Vector3 zoomAnchor = GetZoomAnchorWorldPosition(oldZoom, targetZoom);
                zoomAnchor.z = targetPosition.z;
                float zoomFactor = targetZoom / oldZoom;
                Vector3 delta = zoomAnchor - targetPosition;
                targetPosition = zoomAnchor - delta * zoomFactor;

                UpdateDynamicDragBounds();
                SnapPositionToMapCenterForLockedAxes();
                ClampCameraPosition();
            }
        }

        /// <summary>
        /// 应用平滑移动
        /// </summary>
        private void ApplySmoothMovement()
        {
            if (smoothMovement)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref _positionVelocity,
                    smoothTime
                );

                mapCamera.orthographicSize = Mathf.SmoothDamp(
                    mapCamera.orthographicSize,
                    targetZoom,
                    ref _zoomVelocity,
                    smoothTime
                );
            }
            else
            {
                transform.position = targetPosition;
                mapCamera.orthographicSize = targetZoom;
            }
        }

        /// <summary>
        /// 动态拖拽边界：视野大于地图的轴向锁定在地图中心，避免「看全图」时仍可拖偏。
        /// </summary>
        private void UpdateDynamicDragBounds()
        {
            if (originalMapBounds.size.magnitude <= 0f)
                return;

            float cameraHalfWidth = targetZoom * mapCamera.aspect;
            float cameraHalfHeight = targetZoom;
            float mapHalfWidth = originalMapBounds.extents.x;
            float mapHalfHeight = originalMapBounds.extents.y;
            Vector3 center = originalMapBounds.center;

            float dragMinX;
            float dragMaxX;
            if (cameraHalfWidth >= mapHalfWidth)
            {
                dragMinX = dragMaxX = center.x;
            }
            else
            {
                dragMinX = originalMapBounds.min.x + cameraHalfWidth;
                dragMaxX = originalMapBounds.max.x - cameraHalfWidth;
            }

            float dragMinY;
            float dragMaxY;
            if (cameraHalfHeight >= mapHalfHeight)
            {
                dragMinY = dragMaxY = center.y;
            }
            else
            {
                dragMinY = originalMapBounds.min.y + cameraHalfHeight;
                dragMaxY = originalMapBounds.max.y - cameraHalfHeight;
            }

            dragBoundsMin = new Vector2(dragMinX, dragMinY);
            dragBoundsMax = new Vector2(dragMaxX, dragMaxY);
        }

        private Vector3 GetMapCenter()
        {
            Vector3 c = originalMapBounds.size.magnitude > 0f
                ? originalMapBounds.center
                : Vector3.zero;
            c.z = transform.position.z;
            return c;
        }

        /// <summary>
        /// 接近「看全图」缩放时用地图中心作锚点，否则用鼠标世界坐标。
        /// </summary>
        private Vector3 GetZoomAnchorWorldPosition(float oldZoom, float newZoom)
        {
            float range = Mathf.Max(maxZoom - minZoom, 0.0001f);
            float blendEnd = minZoom + range * minZoomAnchorBlendRange;
            bool useMapCenter = newZoom <= blendEnd || oldZoom <= blendEnd;
            if (useMapCenter)
                return GetMapCenter();

            Vector3 mouseWorldPos = mapCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = targetPosition.z;
            return mouseWorldPos;
        }

        /// <summary>
        /// 视野已包住地图的轴向，强制对齐地图中心。
        /// </summary>
        private void SnapPositionToMapCenterForLockedAxes()
        {
            if (originalMapBounds.size.magnitude <= 0f)
                return;

            float cameraHalfWidth = targetZoom * mapCamera.aspect;
            float cameraHalfHeight = targetZoom;
            Vector3 center = GetMapCenter();

            if (cameraHalfWidth >= originalMapBounds.extents.x)
                targetPosition.x = center.x;
            if (cameraHalfHeight >= originalMapBounds.extents.y)
                targetPosition.y = center.y;
            targetPosition.z = transform.position.z;
        }

        /// <summary>
        /// 让相机刚好框住目标Bounds
        /// </summary>
        public void FitCameraToBounds(Bounds targetBounds)
        {
            if (targetBounds.size.magnitude <= 0)
            {
                Debug.LogWarning("MapCameraController: 地图边界无效，无法框住目标Bounds");
                return;
            }

            Vector3 minViewport = mapCamera.WorldToViewportPoint(targetBounds.min);
            Vector3 maxViewport = mapCamera.WorldToViewportPoint(targetBounds.max);

            float requiredHeight = Mathf.Abs(maxViewport.y - minViewport.y) * mapCamera.orthographicSize * 2f;
            float requiredWidth = Mathf.Abs(maxViewport.x - minViewport.x) * mapCamera.orthographicSize * 2f / mapCamera.aspect;
            mapCamera.orthographicSize = Mathf.Max(requiredHeight, requiredWidth) / 2f;

            mapCamera.transform.position = new Vector3(targetBounds.center.x, targetBounds.center.y, mapCamera.transform.position.z);
        }

        private void ClampCameraPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, dragBoundsMin.x, dragBoundsMax.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, dragBoundsMin.y, dragBoundsMax.y);
            targetPosition.z = transform.position.z;
        }

        /// <summary>
        /// 设置相机边界
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            dragBoundsMin = min;
            dragBoundsMax = max;
        }

        /// <summary>
        /// 重置相机位置和缩放
        /// </summary>
        public void ResetCamera()
        {
            if (MapManager.instance.MapBounds.size.magnitude > 0)
            {
                InitializeFromMapBounds(MapManager.instance.MapBounds);
            }
            else
            {
                targetPosition = Vector3.zero;
                targetZoom = (minZoom + maxZoom) / 2f;
            }
        }
        
        /// <summary>
        /// 设置缩放限制
        /// </summary>
        public void SetZoomLimits(float min, float max)
        {
            minZoom = min;
            maxZoom = max;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        /// <summary>
        /// 通用UI穿透检测方法（兼容鼠标滚轮/触屏/多输入）
        /// </summary>
        private bool IsPointerOverUIElement()
        {
            if (EventSystem.current == null) return false;

            // 构建鼠标指针的事件数据
            PointerEventData eventData = new(EventSystem.current)
            {
                position = Input.mousePosition
            };

            // 检测是否有UI元素接收该指针事件
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // 有UI元素被检测到 → 返回true（指针在UI上）
            return raycastResults.Count > 0;
        }
    }
}
