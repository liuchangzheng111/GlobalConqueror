using GlobalConqueror.Models;
using UnityEngine;

namespace GlobalConqueror.Controllers
{
    public class CityLevelMapping : MonoBehaviour
    {
        [Header("城市各类等级")]
        public int cityLevel = 1;
        public int industryLevel = 0;
        public int airportLevel = 0;
        public int scienceLevel = 0;
        public int supplyLevel = 0;

        public CityKindsLevel CreateCityLevel()
        {
            return new CityKindsLevel(cityLevel, industryLevel, airportLevel, scienceLevel, supplyLevel);
        }

    }
}