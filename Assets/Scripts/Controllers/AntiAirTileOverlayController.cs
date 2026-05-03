using System.Collections;
using System.Collections.Generic;
using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalConqueror.Controllers
{
    public class AntiAirTileOverlayController : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new(-0.35f, -0.35f, 0f);
        [SerializeField] private int sortingOrder = 10;

        private readonly Dictionary<Vector3Int, SpriteRenderer> _icons = new();

        private void OnEnable()
        {
            StartCoroutine(BindWhenReady());
        }

        private IEnumerator BindWhenReady()
        {
            while (MapManager.instance == null || !MapManager.instance.InitializeMapCompleted || AntiAirManager.instance == null)
            {
                yield return null;
            }

            MapManager.instance.OnTileDataChanged += OnTileDataChanged;
            RebuildAll();
        }

        private void OnDisable()
        {
            if (MapManager.instance != null)
            {
                MapManager.instance.OnTileDataChanged -= OnTileDataChanged;
            }
        }

        /// <summary>
        /// 地块数据变化事件
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="data"></param>
        private void OnTileDataChanged(Vector3Int cell, MapTileData data)
        {
            UpdateIcon(cell, data);
        }

        /// <summary>
        /// 重建所有图标
        /// </summary>
        private void RebuildAll()
        {
            foreach (var kv in _icons)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            _icons.Clear();

            foreach (var kv in MapManager.instance.TileDataMap)
            {
                UpdateIcon(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// 更新图标
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="data"></param>
        private void UpdateIcon(Vector3Int cell, MapTileData data)
        {
            if (data == null) return;

            if (data.antiAir == null)
            {
                if (_icons.TryGetValue(cell, out var existing) && existing != null)
                {
                    Destroy(existing.gameObject);
                }
                _icons.Remove(cell);
                return;
            }

            Sprite icon = AntiAirManager.instance != null ? AntiAirManager.instance.GetAntiAirIcon(data.antiAir) : null;
            if (icon == null)
            {
                // 没配置图标就不显示
                if (_icons.TryGetValue(cell, out var existing) && existing != null)
                {
                    Destroy(existing.gameObject);
                }
                _icons.Remove(cell);
                return;
            }

            if (!_icons.TryGetValue(cell, out var sr) || sr == null)
            {
                var go = new GameObject($"AAIcon_{cell.x}_{cell.y}_{cell.z}");
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;
                sr.sortingLayerName = "Unit";
                _icons[cell] = sr;
            }

            Vector3 world = MapManager.instance.Tilemap.GetCellCenterWorld(cell);
            sr.transform.position = world + worldOffset;
            sr.sprite = icon;
        }
    }
}

