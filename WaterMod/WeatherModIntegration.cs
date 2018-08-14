using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaterMod
{
    class WeatherModIntegration
    {
        public static float RainWeight
        {
            get => (TTQMM_WeatherMod.RainMaker.isRaining ? TTQMM_WeatherMod.RainMaker.RainWeight : 0f);
        }
    }
}
