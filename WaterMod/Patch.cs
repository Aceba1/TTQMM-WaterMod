// https://github.com/fuqunaga/RapidGUI

using System.Reflection;
using HarmonyLib;
using ModHelper;
using UnityEngine;
using Nuterra.NativeOptions;
using System.IO;

namespace WaterMod
{
    public class QPatch
    {
        const string ModName = "Water Mod";
        public static bool ModExists(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }

        public static float WaterHeight
        {
            get => WaterBuoyancy.HeightCalc;
        }

        public static ModConfig _thisMod;

        public static KeyCode key;
        public static int key_int;

        internal static AssetBundle assetBundle;
        internal static string asm_path = Assembly.GetExecutingAssembly().Location.Replace("WaterMod.dll", "");
        internal static string assets_path = Path.Combine(asm_path, "Assets");
        internal static string HarmonyID = "aceba1.ttmm.revived.water";
        internal static Harmony harmony = new Harmony(HarmonyID);

        public static Material basic;
        public static Material fancy;

        public static void Main()
        {
            d.Log("Initializing WaterMod!");

            if (!WaterMod.Inited)
            {
                WaterMod.Inited = true;
                WaterMod.TTMMInited = true;
                ApplyPatch();
                SetupResources();
            }
        }

        public static void ApplyPatch()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void SetupResources()
        {
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(asm_path, "waterassets.assetbundle"));

            ModConfig thisMod = new ModConfig();

            thisMod.BindConfig<WaterParticleHandler>(null, "UseParticleEffects");

            WaterBuoyancy.Initiate();

            thisMod.BindConfig<QPatch>(null, "key_int");
            key = (KeyCode)key_int;
            thisMod.BindConfig<WaterBuoyancy>(null, "IsActive");
            thisMod.BindConfig<WaterBuoyancy>(null, "Height");
            thisMod.BindConfig<WaterBuoyancy>(null, "Density");
            thisMod.BindConfig<WaterBuoyancy>(null, "FanJetMultiplier");
            thisMod.BindConfig<WaterBuoyancy>(null, "ResourceBuoyancyMultiplier");
            thisMod.BindConfig<WaterBuoyancy>(null, "BulletDampener");
            thisMod.BindConfig<WaterBuoyancy>(null, "MissileDampener");
            thisMod.BindConfig<WaterBuoyancy>(null, "LaserFraction");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceSkinning");
            thisMod.BindConfig<WaterBuoyancy>(null, "SubmergedTankDampening");
            thisMod.BindConfig<WaterBuoyancy>(null, "SubmergedTankDampeningYAddition");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceTankDampening");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceTankDampeningYAddition");
            thisMod.BindConfig<WaterBuoyancy>(null, "SelectedLook");
            thisMod.BindConfig<WaterBuoyancy>(null, "AbyssDepth");

            _thisMod = thisMod;


            GUIMenu = new OptionKey("GUI Menu button", ModName, key);
            GUIMenu.onValueSaved.AddListener(() => { key_int = (int)(key = GUIMenu.SavedValue); WaterBuoyancy._inst.Invoke("Save", 0.5f); });

            IsWaterActive = new OptionToggle("Water Active", ModName, WaterBuoyancy.IsActive);
            IsWaterActive.onValueSaved.AddListener(() => { WaterBuoyancy.IsActive = IsWaterActive.SavedValue; WaterBuoyancy.SetState(); });
            UseParticleEffects = new OptionToggle("Particle effects Active", ModName, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => { WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue; });
            Height = new OptionRange("Height level", ModName, WaterBuoyancy.Height, -75f, 100f, 1f);
            Height.onValueSaved.AddListener(() => { WaterBuoyancy.Height = Height.SavedValue; });

            var WaterProperties = ModName + " - Water properties";
            Density = new OptionRange("Density", WaterProperties, WaterBuoyancy.Density, -16, 16, 0.25f);
            Density.onValueSaved.AddListener(() => { WaterBuoyancy.Density = Density.SavedValue; });
            FanJetMultiplier = new OptionRange("Fan jet Multiplier", WaterProperties, WaterBuoyancy.FanJetMultiplier, 0f, 4f, .05f);
            FanJetMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FanJetMultiplier = FanJetMultiplier.SavedValue; });
            ResourceBuoyancy = new OptionRange("Resource Buoyancy", WaterProperties, WaterBuoyancy.ResourceBuoyancyMultiplier, 0f, 4f, .05f);
            ResourceBuoyancy.onValueSaved.AddListener(() => { WaterBuoyancy.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue; });
            BulletDampener = new OptionRange("Bullet Dampening", WaterProperties, WaterBuoyancy.BulletDampener, 0f, 1E-4f, 1E-8f);
            BulletDampener.onValueSaved.AddListener(() => { WaterBuoyancy.BulletDampener = BulletDampener.SavedValue; });
            MissileDampener = new OptionRange("Missile Dampening", WaterProperties, WaterBuoyancy.MissileDampener, 0f, 0.1f, 0.003f);
            MissileDampener.onValueSaved.AddListener(() => { WaterBuoyancy.MissileDampener = MissileDampener.SavedValue; });
            LaserFraction = new OptionRange("Laser Slowdown", WaterProperties, WaterBuoyancy.LaserFraction, 0f, 0.5f, 0.025f);
            LaserFraction.onValueSaved.AddListener(() => { WaterBuoyancy.LaserFraction = LaserFraction.SavedValue; });
            SurfaceSkinning = new OptionRange("Surface Skinning", WaterProperties, WaterBuoyancy.SurfaceSkinning, -0.5f, 0.5f, 0.05f);
            SurfaceSkinning.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceSkinning = SurfaceSkinning.SavedValue; });
            SubmergedTankDampening = new OptionRange("Submerged Tank Dampening", WaterProperties, WaterBuoyancy.SubmergedTankDampening, 0f, 2f, 0.05f);
            SubmergedTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampening = SubmergedTankDampening.SavedValue; });
            SubmergedTankDampeningY = new OptionRange("Submerged Tank Dampening Y addition", WaterProperties, WaterBuoyancy.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f);
            SubmergedTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue; });
            SurfaceTankDampening = new OptionRange("Surface Tank Dampening", WaterProperties, WaterBuoyancy.SurfaceTankDampening, 0f, 2f, 0.05f);
            SurfaceTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampening = SurfaceTankDampening.SavedValue; });
            SurfaceTankDampeningY = new OptionRange("Surface Tank Dampening Y addition", WaterProperties, WaterBuoyancy.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f);
            SurfaceTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue; });

            var WaterLook = ModName + " - Water look";
            var waterLook = new OptionList<WaterBuoyancy.WaterLook>("Water look", WaterLook, WaterBuoyancy.waterLooks, WaterBuoyancy.SelectedLook);
            waterLook.onValueSaved.AddListener(() =>
            {
                WaterBuoyancy.UpdateLook(waterLook.Selected);
                WaterBuoyancy.SelectedLook = waterLook.SavedValue;
            });
            WaterBuoyancy.UpdateLook(WaterBuoyancy.waterLooks[WaterBuoyancy.SelectedLook]);

            var waterAbyssDepth = new OptionRange("Abyss depth", WaterLook, WaterBuoyancy.AbyssDepth);
            waterAbyssDepth.onValueSaved.AddListener(() => { WaterBuoyancy.AbyssDepth = waterAbyssDepth.SavedValue; });
        }

        public static OptionKey GUIMenu;
        public static OptionToggle IsWaterActive;
        public static OptionToggle UseParticleEffects;
        public static OptionRange Height;
        public static OptionRange Density;
        public static OptionRange FanJetMultiplier;
        public static OptionRange ResourceBuoyancy;
        public static OptionRange BulletDampener;
        public static OptionRange MissileDampener;
        public static OptionRange LaserFraction;
        public static OptionRange SurfaceSkinning;
        public static OptionRange SubmergedTankDampening;
        public static OptionRange SubmergedTankDampeningY;
        public static OptionRange SurfaceTankDampening;
        public static OptionRange SurfaceTankDampeningY;

        public static OptionRange RainWeightMultiplier;
        public static OptionRange RainDrainMultiplier;
        public static OptionRange FloodRateClamp;
        public static OptionRange FloodHeightMultiplier;

        public static OptionToggle Reset;
    }

    internal class Patches
    {
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnPool")]
        private class PatchBlock
        {
            [HarmonyPostfix]
            private static void Postfix(TankBlock __instance)
            {
                WaterBuoyancy.WaterBlock wEffect = __instance.gameObject.GetComponent<WaterBuoyancy.WaterBlock>();
                if (wEffect == null)
                {
                    wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterBlock>();
                }
                wEffect.TankBlock = __instance;
                if (__instance.BlockCategory == BlockCategories.Flight)
                {
                    var component = __instance.GetComponentInChildren<FanJet>();
                    if (component != null)
                    {
                        wEffect.isFanJet = true;
                        wEffect.componentEffect = component;
                        wEffect.initVelocity = new Vector3(component.force, component.backForce, 0f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnRecycle")]
        private class TankBlockRecycle
        {
            [HarmonyPostfix]
            private static void Postfix(TankBlock __instance)
            {
                try
                {
                    __instance.gameObject.GetComponent<WaterBuoyancy.WaterBlock>().TryRemoveSurface();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Tank))]
        [HarmonyPatch("OnPool")]
        private class PatchTank
        {
            [HarmonyPostfix]
            private static void Postfix(Tank __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterTank>();
                wEffect.Subscribe(__instance);
            }
        }

        [HarmonyPatch(typeof(Projectile), "OnPool")]
        private class PatchProjectileSpawn
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                wEffect.effectBase = __instance;

                if (__instance is MissileProjectile missile)
                {
                    wEffect.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
                }
                else if (__instance is LaserProjectile laser)
                {
                    wEffect.effectType = WaterBuoyancy.EffectTypes.LaserProjectile;
                }
                else
                {
                    wEffect.effectType = WaterBuoyancy.EffectTypes.NormalProjectile;
                }
                wEffect._rbody = __instance.rbody;
            }
        }

        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnPool")]
        private class PatchResource
        {
            [HarmonyPostfix]
            private static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.ResourceChunk;
                wEffect.GetRBody();
            }
        }
    }
}