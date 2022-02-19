using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterMod
{
    public class WaterMod : ModBase
    {
        internal static bool Inited = false;
        internal static bool TTMMInited = false;

        public override bool HasEarlyInit()
        {
            return true;
        }

        public void ManagedEarlyInit()
        {
            if (!Inited)
            {
                QPatch.SetupResources();
                QPatch.ApplyPatch();
                Inited = true;
            }
        }

        public override void EarlyInit()
        {
            this.ManagedEarlyInit();
        }

        public override void DeInit()
        {
        }

        public override void Init()
        {
        }
    }
}
