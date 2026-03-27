using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GlobalConqueror.Models;
using System.Linq;
using GlobalConqueror.Controllers;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 港口管理器 - 管理所有港口数据
    /// </summary>
    public class PortManager : MonoBehaviour
    {
        public static PortManager instance;

        [Header("港口簇")]
        public GameObject ports;

        [Header("港口详情图标预制体")]
        public GameObject portView;

        [Header("港口等级列表图标(按从低到高顺序)")]
        public List<Sprite> portLevels;

        private readonly List<PortData> allPorts = new();
        private readonly Dictionary<string, List<PortData>> nationOwnPorts = new();

        public bool IsPortsInitialized { get; private set; } = false;

        public List<PortData> AllPorts => allPorts;

        public Dictionary<string, List<PortData>> NationOwnPorts => nationOwnPorts;

        public System.Action OnPortsInitialized;

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

        private void Start()
        {
            StartCoroutine(InitializeWhenMapReady());
        }

        /// <summary>
        /// 等待地图初始化完成后再初始化港口
        /// </summary>
        private System.Collections.IEnumerator InitializeWhenMapReady()
        {
            // 等待 NationManager 单例与国家初始化完成
            while (NationManager.instance == null || !NationManager.instance.IsNationsInitialized)
            {
                yield return null;
            }

            InitializePortMaps();
        }

        /// <summary>
        /// 初始化港口映射
        /// </summary>
        private void InitializePortMaps()
        {
            IsPortsInitialized = false;

            if (ports == null)
            {
                Debug.LogError("PortManager: 港口簇对象ports未赋值！");
                return;
            }

            PortMapping[] childPortsMapping = ports.GetComponentsInChildren<PortMapping>(includeInactive: false);
            int countIndex = 0;

            foreach (PortMapping portMapping in childPortsMapping)
            {
                if (!MapManager.instance.PortsTile.Contains(portMapping.PortPositionInt))
                {
                    Debug.LogWarning($"PortManager: 港口对象port{portMapping.PortPositionInt}位置未在Port地块上！");
                    continue;
                }

                PortData port = new(
                    countIndex,
                    portMapping.name,
                    portMapping.PortPositionInt,
                    NationManager.instance.NationsDic.TryGetValue(portMapping.nationName, out NationData nation) ? nation.nationId : -1,
                    portMapping.PortLevel
                    );

                allPorts.Add(port);

                if (nationOwnPorts.ContainsKey(portMapping.nationName))
                {
                    nationOwnPorts[portMapping.nationName].Add(port);
                }
                else
                {
                    nationOwnPorts.Add(portMapping.nationName, new List<PortData> { port });
                }

                MapManager.instance.SetTileOwner(portMapping.PortPositionInt, nation.nationId);
                countIndex++;
            }     

            // 初始化PortView
            foreach (var port in allPorts)
            {
                Vector3 location = MapManager.instance.Tilemap.CellToWorld(port.portLocation);
                GameObject portGo = Instantiate(portView, location, Quaternion.identity, this.transform);
                if (portGo.TryGetComponent<PortView>(out var view))
                {
                    view.Setup(port);
                }
                else
                {
                    Debug.LogWarning("PortManager: 港口详情预制体无PortView组件！");
                }
            }

            IsPortsInitialized = true;
            OnPortsInitialized?.Invoke();
            Debug.Log($"PortManager: 港口初始化完成，共加载 {allPorts.Count} 个港口");
        }

        /// <summary>
        /// 根据坐标获取该位置的港口（若有）
        /// </summary>
        public PortData GetPortAtPosition(Vector3Int position)
        {
            if (allPorts == null) return null;
            foreach (var port in allPorts)
            {
                if (port.portLocation == position)
                    return port;
            }
            return null;
        }

        /// <summary>
        /// 转移港口所有权
        /// </summary>
        public void TransferPortOwnership(PortData port, NationData newOwner)
        {
            if (port == null || NationManager.instance == null) return;

            int oldOwnerId = port.ownerNationId;

            string oldOwner = NationManager.instance.GetNation(oldOwnerId).nationName;

            if (nationOwnPorts.TryGetValue(oldOwner,out List<PortData> oldPorts))
            {
                oldPorts.Remove(port);
            }

            if (nationOwnPorts.TryGetValue(newOwner.nationName, out List<PortData> newPorts))
            {
                newPorts.Add(port);
                port.ownerNationId = newOwner.nationId;
            }
        }
    }
}
