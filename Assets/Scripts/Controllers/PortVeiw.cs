using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace GlobalConqueror.Controllers
{
    public class PortView : MonoBehaviour
    {
        [Header("港口名称")]
        [SerializeField] private TextMeshProUGUI portName;

        [Header("港口等级")]
        [SerializeField] private TextMeshProUGUI portLevel;

        [Header("所属国家国旗")]
        [SerializeField] private Image OwnerNationFlag;

        private PortData _boundPort;

        private void Awake()
        {
            if (OwnerNationFlag != null)
            {
                OwnerNationFlag.enabled = false;
                OwnerNationFlag.sprite = null;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(InitializeWhenPortReady());
        }

        private System.Collections.IEnumerator InitializeWhenPortReady()
        {
            while (UnitManager.instance == null || !UnitManager.instance.initialUnitsSpawned)
            {
                yield return null;
            }

            UnitManager.instance.OnPortCaptured += (unitData, portData) => Refresh(portData);
        }

        private void OnDisable()
        {
            if (UnitManager.instance != null)
            {
                UnitManager.instance.OnPortCaptured -= (unitData, portData) => Refresh(portData);
            }
        }

        /// <summary>
        /// 绑定港口数据并刷新显示
        /// </summary>
        public void Setup(PortData port)
        {
            _boundPort = port;

            ResetUI();

            if (port == null)
            {
                Debug.LogWarning($"PortView: 绑定空单位数据，UI已重置（物体：{gameObject.name}）");
                return;
            }

            Refresh(port);
        }

        /// <summary>
        /// 重置所有UI
        /// </summary>
        private void ResetUI()
        {
            if (OwnerNationFlag != null)
            {
                OwnerNationFlag.enabled = false;
                OwnerNationFlag.sprite = null;
            }
            if (portName != null)
            {
                portName.enabled = false;
                portName.text = "";
            }
            if (portLevel != null)
            {
                portLevel.enabled = false;
                portLevel.text = "";
            }
        }

        /// <summary>
        /// 刷新UI
        /// </summary>
        public void Refresh(PortData port)
        {
            if (_boundPort == null)
            {
                Debug.LogWarning("PortView: _boundPort数据丢失！");
                ResetUI();
                return;
            }

            if (port != _boundPort) return;

            portName.enabled = true;
            portName.text = _boundPort.portName;

            portLevel.enabled = true;
            portLevel.text = _boundPort.GetPortLevelString();


            OwnerNationFlag.enabled = NationManager.instance != null;
            if (OwnerNationFlag.enabled)
            {
                OwnerNationFlag.sprite = NationManager.instance.GetNation(_boundPort.ownerNationId).nationFlag;
            }
        }
    }
}