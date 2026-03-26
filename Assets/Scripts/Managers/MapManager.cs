using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 地图管理器 - 管理所有地块数据和地图相关逻辑
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        static public MapManager instance;

        [Header("地图设置（用于随机生成）")]
        [SerializeField] private float hexSize = 1f; // 六边形大小

        [Header("Tilemap设置")]
        [SerializeField] private Tilemap sourceTilemap; // 源Tilemap（如果为空则使用随机生成）
        [SerializeField] private TilemapRenderer sourceTilemapRenderer; // 源Tilemap（如果为空则使用随机生成）
        [SerializeField] private bool useTilemapAsSource = true; // 是否从Tilemap初始化
        [SerializeField] private bool autoFindTilemap = true; // 自动查找场景中的Tilemap

        // 自定义Tile类型映射（可选）
        [SerializeField] private List<TileTypeMapping> customTileMappings = new List<TileTypeMapping>();

        // 地块数据字典：坐标 -> 地块数据
        private Dictionary<Vector3Int, MapTileData> tileDataMap = new Dictionary<Vector3Int, MapTileData>();

        // 城市地块列表
        private List<Vector3Int> citiesTile = new List<Vector3Int>();

        // 港口地块列表
        private List<Vector3Int> portsTile = new List<Vector3Int>();

        // 当前选中的地块
        private Vector3Int? selectedTileCoordinate = null;

        // 地图是否初始化完成
        [HideInInspector]
        public bool InitializeMapCompleted = false;

        // 事件：地块被选中
        public System.Action<Vector3Int> OnTileSelected;      

        private Vector3 cellSize;

        public Vector3 CellSize => cellSize;

        public Vector3Int? SelectedTileCoordinate => selectedTileCoordinate;
        
        public Bounds MapBounds => sourceTilemapRenderer.bounds;

        public Tilemap Tilemap => sourceTilemap;

        public Dictionary<Vector3Int, MapTileData> TileDataMap => tileDataMap;

        public List<Vector3Int> CitiesTile => citiesTile;

        public List<Vector3Int> PortsTile => portsTile;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 此脚本为在默认时间前进行编译，即其他所有脚本在编译前此脚本就已经编译，即第一编译
        private void Start()
        {
            InitializeMap();
        }

        /// <summary>
        /// 初始化地图数据
        /// </summary>
        public void InitializeMap()
        {
            tileDataMap.Clear();

            // 如果启用从Tilemap初始化，优先使用Tilemap
            if (useTilemapAsSource)
            {
                if (InitializeFromTilemap())
                {
                    Debug.Log($"从Tilemap初始化地图完成，共 {tileDataMap.Count} 个地块");
                }
                else
                {
                    Debug.LogWarning("无法从Tilemap初始化，使用随机生成模式");            
                    InitializeRandomMap();
                }
            }
            InitializeMapCompleted = true;
        }

        /// <summary>
        /// 从Tilemap初始化地图数据
        /// </summary>
        private bool InitializeFromTilemap()
        {
            // 查找Tilemap
            if (sourceTilemap == null && autoFindTilemap)
            {
                sourceTilemap = FindObjectOfType<Tilemap>();
            }

            if (sourceTilemap == null)
            {
                Debug.LogWarning("MapManager: 未找到Tilemap组件！");
                return false;
            }

            Grid grid = sourceTilemap.layoutGrid;
            if (grid == null)
            {
                Debug.LogWarning("MapManager: Tilemap没有关联的Grid组件！");
                return false;
            }

            // 获取Grid的单元格大小
            cellSize = grid.cellSize;

            hexSize = Mathf.Max(cellSize.x, cellSize.y) * 0.5f; // 六边形大小估算

            // 创建自定义映射字典
            Dictionary<TileBase, TileType> customMapping = new Dictionary<TileBase, TileType>();
            foreach (var mapping in customTileMappings)
            {
                if (mapping.tile != null)
                {
                    customMapping[mapping.tile] = mapping.tileType;
                }
            }

            // 遍历Tilemap中的所有tile
            BoundsInt bounds = sourceTilemap.cellBounds;
            int tileCount = 0;

            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                TileBase tile = sourceTilemap.GetTile(pos);
                if (tile != null)
                {
                    // 获取Tile类型
                    TileType tileType = TileTypeMapping.GetTileTypeFromCustomMapping(tile, customMapping);

                    // 创建地块数据
                    MapTileData tileData = new MapTileData(tile, tileType);
                    tileDataMap[pos] = tileData;
                    if(tileType == TileType.City)
                    {
                        citiesTile.Add(pos);
                    }
                    else if (tileType == TileType.Port)
                    {
                        portsTile.Add(pos);
                    }
                    tileCount++;
                }
            }

            Debug.Log($"从Tilemap读取了 {tileCount} 个地块");
            return tileCount > 0;
        }

        /// <summary>
        /// 随机生成地图
        /// </summary>
        private void InitializeRandomMap()
        {
            // TOEXPAND:

            Debug.Log($"随机地图初始化完成，共 {tileDataMap.Count} 个地块");
        }

        /// <summary>
        /// 获取默认地块类型（可以根据坐标生成不同地形）
        /// </summary>
        private TileType GetDefaultTileType(Vector3Int position)
        {
            // 简单的随机地形生成（可以根据需要修改）
            float noise = Mathf.PerlinNoise(position.x * 0.1f, position.y * 0.1f);
            
            if (noise < 0.3f)
                return TileType.Water;
            else if (noise < 0.5f)
                return TileType.Plain;
            else if (noise < 0.7f)
                return TileType.Forest;
            else
                return TileType.Mountain;
        }

        /// <summary>
        /// 获取地块数据
        /// </summary>
        public MapTileData GetTileData(Vector3Int coordinate)
        {
            tileDataMap.TryGetValue(coordinate, out MapTileData tileData);
            return tileData;
        }

        /// <summary>
        /// 设置地块所属国家（用于自动识别单位归属、领土等）
        /// </summary>
        public void SetTileOwner(Vector3Int coordinate, int ownerId)
        {
            if (tileDataMap.TryGetValue(coordinate, out MapTileData data))
            {
                data.ownerId = ownerId;
            }
        }

        /// <summary>
        /// 检查坐标是否在地图范围内
        /// </summary>
        public bool IsCoordinateValid(Vector3Int coordinate)
        {
            return tileDataMap.ContainsKey(coordinate);
        }

        /// <summary>
        /// 设置选中的地块
        /// </summary>
        public void SetSelectedTile(Vector3Int? coordinate)
        {
            selectedTileCoordinate = coordinate;
            if (coordinate.HasValue)
            {
                OnTileSelected?.Invoke(coordinate.Value);
            }
        }

        /// <summary>
        /// 获取所有地块坐标
        /// </summary>
        public IEnumerable<Vector3Int> GetAllCoordinates()
        {
            return tileDataMap.Keys;
        }
    }
}
