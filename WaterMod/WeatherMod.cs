using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TTQMM_WeatherMod;

namespace WaterMod
{
    class WeatherMod
    {
        public static float RainWeight { get => (RainMaker.isRaining ? RainMaker.RainWeight : 0f); }
    }
}
