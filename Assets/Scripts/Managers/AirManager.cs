using GlobalConqueror.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalConqueror.Managers
{
    /// <summary>
    /// 空军管理器 - 管理空军及其攻击、空投
    /// </summary>
    public class AirManager : MonoBehaviour
    {
        public static AirManager instance;

        [Header("空军列表")]
        [SerializeField] private List<AirMissionConfig> availableAircrafts = new();

        [HideInInspector]
        public bool initialUnitsSpawned = false;

        public List<AirMissionConfig> AvailableAircrafts => availableAircrafts;

        public System.Action<AirMissionConfig> OnAircraftSpawned;
        public System.Action<AirMissionConfig, UnitData> OnAircraftAttacked;

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
            StartCoroutine(InitializeUnitsWhenMapReady());
        }

        private IEnumerator InitializeUnitsWhenMapReady()
        {
            while (CityManager.instance == null || !CityManager.instance.IsCityTilemapInitialized)
            {
                yield return null;
            }
            while (UnitManager.instance == null || !UnitManager.instance.initialUnitsSpawned)
            {
                yield return null;
            }


        }

        private void OnDisable()
        {

        }



    }
}