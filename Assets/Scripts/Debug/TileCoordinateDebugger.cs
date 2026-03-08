//using UnityEngine;
//using GlobalConqueror.Models;
//using GlobalConqueror.Managers;
//#if UNITY_TEXTMESHPRO
//using TMPro;
//#endif

//namespace GlobalConqueror.DebugTools
//{
//    /// <summary>
//    /// 地块坐标调试器 - 显示点击地块的坐标信息
//    /// </summary>
//    public class TileCoordinateDebugger : MonoBehaviour
//    {
//    [Header("UI设置")]
//#if UNITY_TEXTMESHPRO
//    [SerializeField] private TextMeshProUGUI debugTextPrefab; // 调试文本预制体（可选）
//    private TextMeshProUGUI debugTextInstance;
//#else
//    [SerializeField] private UnityEngine.UI.Text debugTextPrefab; // 调试文本预制体（可选）
//    private UnityEngine.UI.Text debugTextInstance;
//#endif
//    [SerializeField] private bool showOnScreen = true; // 在屏幕上显示
//    [SerializeField] private bool showInConsole = true; // 在控制台显示

//    private MapManager mapManager;

//        private void Awake()
//        {
//            mapManager = FindObjectOfType<MapManager>();
            
//            if (showOnScreen && debugTextPrefab != null)
//            {
//                // 创建UI文本（需要Canvas）
//                Canvas canvas = FindObjectOfType<Canvas>();
//                if (canvas == null)
//                {
//                    GameObject canvasObj = new GameObject("DebugCanvas");
//                    canvas = canvasObj.AddComponent<Canvas>();
//                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//                    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
//                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
//                }

//                debugTextInstance = Instantiate(debugTextPrefab, canvas.transform);
//                debugTextInstance.rectTransform.anchoredPosition = new Vector2(10, -10);
//#if UNITY_TEXTMESHPRO
//                if (debugTextInstance is TextMeshProUGUI tmpText)
//                {
//                    tmpText.alignment = TMPro.TextAlignmentOptions.TopLeft;
//                }
//#else
//                if (debugTextInstance is UnityEngine.UI.Text text)
//                {
//                    text.alignment = TextAnchor.UpperLeft;
//                }
//#endif
//            }
//        }

//        private void OnEnable()
//        {
//            if (mapManager != null)
//            {
//                mapManager.OnTileSelected += OnTileSelected;
//            }
//        }

//        private void OnDisable()
//        {
//            if (mapManager != null)
//            {
//                mapManager.OnTileSelected -= OnTileSelected;
//            }
//        }

//        /// <summary>
//        /// 地块被选中时的回调
//        /// </summary>
//        private void OnTileSelected(HexCoordinate coordinate)
//        {
//            MapTileData tileData = mapManager.GetTileData(coordinate);
//            Vector3 worldPos = coordinate.ToWorldPosition(mapManager.HexSize);

//            string info = $"坐标: {coordinate}\n" +
//                         $"世界位置: ({worldPos.x:F2}, {worldPos.y:F2})\n" + 
//                         $"地块类型: {tileData?.tileType ?? TileType.Plain}\n" +
//                         $"所有者ID: {tileData?.ownerId ?? -1}";

//            if (showInConsole)
//            {
//                Debug.Log($"[坐标调试] {info}");
//            }

//            if (showOnScreen && debugTextInstance != null)
//            {
//#if UNITY_TEXTMESHPRO
//                if (debugTextInstance is TextMeshProUGUI tmpText)
//                {
//                    tmpText.text = info;
//                }
//#else
//                if (debugTextInstance is UnityEngine.UI.Text text)
//                {
//                    text.text = info;
//                }
//#endif
//            }
//        }

//        private void Update()
//        {
//            // 如果没有UI文本，使用OnGUI显示
//            if (showOnScreen && debugTextInstance == null)
//            {
//                if (mapManager != null && mapManager.SelectedTileCoordinate.HasValue)
//                {
//                    HexCoordinate coord = mapManager.SelectedTileCoordinate.Value;
//                    MapTileData tileData = mapManager.GetTileData(coord);
//                    Vector3 worldPos = coord.ToWorldPosition(mapManager.HexSize);

//                    string info = $"坐标: {coord}\n" +
//                                 $"世界位置: ({worldPos.x:F2}, {worldPos.y:F2})\n" + 
//                                 $"地块类型: {tileData?.tileType ?? TileType.Plain}";
                    
//                    // 这里可以添加OnGUI显示逻辑
//                }
//            }
//        }
//    }
//}
