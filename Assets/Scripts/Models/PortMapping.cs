using GlobalConqueror.Models;
using UnityEngine;
using GlobalConqueror.Managers;

namespace GlobalConqueror.Controllers
{
    public class PortMapping : MonoBehaviour
    {
        [Header("港口等级")]
        public int PortLevel = 1;

        [Header("所属国家")]
        public string nationName = "";

        public Vector3 PortPosition => this.transform.position;
        public Vector3Int PortPositionInt => MapManager.instance != null ? MapManager.instance.Tilemap.WorldToCell(PortPosition) : Vector3Int.zero;
    
    }
}