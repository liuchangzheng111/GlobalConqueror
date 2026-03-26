using GlobalConqueror.Managers;
using GlobalConqueror.Models;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace GlobalConqueror.Controllers
{
    public class CityView : MonoBehaviour
    {
        [Header("城市名称")]
        [SerializeField] private TextMeshProUGUI cityName;

        [Header("城市等级")]
        [SerializeField] private TextMeshProUGUI cityLevel;

        [Header("各类等级")]
        [SerializeField] private Image industryImage;
        [SerializeField] private Image airportImage;
        [SerializeField] private Image scienceImage;
        [SerializeField] private Image supplyImage;

        private CityData _boundCity;

        private void Awake()
        {
            if (industryImage != null)
            {
                industryImage.enabled = false;
            }
            if (airportImage != null)
            {
                airportImage.enabled = false;
            }
            if (scienceImage != null)
            {
                scienceImage.enabled = false;
            }
            if (supplyImage != null)
            {
                supplyImage.enabled = false;
            }
        }

        /// <summary>
        /// 绑定城市数据并刷新显示
        /// </summary>
        public void Setup(CityData city)
        {
            _boundCity = city;

            ResetUI();

            if (city == null)
            {
                Debug.LogWarning($"CityView: 绑定空单位数据，UI已重置（物体：{gameObject.name}）");
                return;
            }

            Refresh();
        }

        /// <summary>
        /// 重置所有UI
        /// </summary>
        private void ResetUI()
        {
            if (industryImage != null)
            {
                industryImage.enabled = false;
                industryImage.sprite = null;
            }
            if (airportImage != null)
            {
                airportImage.enabled = false;
                airportImage.sprite = null;
            }
            if (scienceImage != null)
            {
                scienceImage.enabled = false;
                scienceImage.sprite = null;
            }
            if (supplyImage != null)
            {
                supplyImage.enabled = false;
                supplyImage.sprite = null;
            }
            if (cityName != null)
            {
                cityName.enabled = false;
                cityName.text = "";
            }
            if (cityLevel != null)
            {
                cityLevel.enabled = false;
                cityLevel.text = "";
            }
        }

        /// <summary>
        /// 刷新UI
        /// </summary>
        public void Refresh()
        {

            if (_boundCity == null)
            {
                Debug.LogWarning("CityView: _boundCity数据丢失！");
                ResetUI();
                return;
            }

            cityName.enabled = true;
            cityName.text = _boundCity.cityName;

            cityLevel.enabled = true;
            cityLevel.text = _boundCity.cityKindsLevel.GetCityLevelString();
   
            if (CityManager.instance != null)
            {
                if (CityManager.instance.industry.Count >= 4)
                {
                    if (_boundCity.cityKindsLevel.industryLevel > 0)
                    {
                        industryImage.enabled = true;
                        industryImage.sprite = CityManager.instance.industry[_boundCity.cityKindsLevel.industryLevel - 1];
                    }
                }
                if (CityManager.instance.airport.Count >= 4)
                {
                    if (_boundCity.cityKindsLevel.airportLevel > 0)
                    {
                        airportImage.enabled = true;
                        airportImage.sprite = CityManager.instance.airport[_boundCity.cityKindsLevel.airportLevel - 1];
                    }
                }
                if (CityManager.instance.science.Count >= 4)
                {
                    if (_boundCity.cityKindsLevel.scienceLevel > 0)
                    {
                        scienceImage.enabled = true;
                        scienceImage.sprite = CityManager.instance.science[_boundCity.cityKindsLevel.scienceLevel - 1];
                    }
                }
                if (CityManager.instance.supply.Count >= 4)
                {
                    if (_boundCity.cityKindsLevel.supplyLevel > 0)
                    {
                        supplyImage.enabled = true;
                        supplyImage.sprite = CityManager.instance.supply[_boundCity.cityKindsLevel.supplyLevel - 1];
                    }
                }
            }
        }
    }
}