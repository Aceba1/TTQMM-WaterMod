// https://github.com/fuqunaga/RapidGUI

using System;
using System.Reflection;
using Harmony;
using QModManager.Utility;
using ModHelper.Config;
using UnityEngine;
using Nuterra.NativeOptions;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
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
        public static Material basic;
        public static Material fancy;

        public static void Main()
        {
            var harmony = HarmonyInstance.Create("aceba1.ttmm.revived.water");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            assetBundle = AssetBundle.LoadFromFile(Path.Combine(assets_path, "waterassets"));

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

            /*WaterBuoyancy._WeatherMod = ModExists("TTQMM WeatherMod");
            if (WaterBuoyancy._WeatherMod)
            {
                Debug.Log("Found WeatherMod!");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainWeightMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainDrainMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "FloodChangeClamp");
                thisMod.BindConfig<WaterBuoyancy>(null, "FloodHeightMultiplier");
            }*/

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

            /*if (WaterBuoyancy._WeatherMod)
            {
                var WeatherProperties = ModName + " - Weather mod";
                RainWeightMultiplier = new OptionRange("Rain Weight Multiplier", WeatherProperties, WaterBuoyancy.RainWeightMultiplier, 0, 0.25f, 0.01f);
                RainWeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = new OptionRange("Rain Drain Multiplier", WeatherProperties, WaterBuoyancy.RainDrainMultiplier, 0, 0.25f, 0.01f);
                RainDrainMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = new OptionRange("Flood rate Clamp", WeatherProperties, WaterBuoyancy.FloodChangeClamp, 0, 0.08f, 0.001f);
                FloodRateClamp.onValueSaved.AddListener(() => { WaterBuoyancy.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = new OptionRange("Flood Height Multiplier", WeatherProperties, WaterBuoyancy.FloodHeightMultiplier, 0, 50f, 1f);
                FloodHeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FloodHeightMultiplier = FloodHeightMultiplier.SavedValue; });
            }*/
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
            private static void Postfix(TankBlock __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterBlock>();
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
            private static void Postfix(Tank __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterTank>();
                wEffect.Subscribe(__instance);
            }
        }

        [HarmonyPatch(typeof(Projectile), "OnPool")]
        private class PatchProjectileSpawn
        {
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

        //[HarmonyPatch(typeof(MissileProjectile), "Fire")]
        //private class PatchMissile
        //{
        //    private static void Prefix(MissileProjectile __instance)
        //    {
        //        var wEffect = __instance.gameObject.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else if (wEffect.effectType >= WaterBuoyancy.EffectTypes.MissileProjectile)
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        //[HarmonyPatch(typeof(Projectile), "Fire")]
        //private class PatchProjectile
        //{
        //    private static void Prefix(Projectile __instance)
        //    {
        //        var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.NormalProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        //[HarmonyPatch(typeof(LaserProjectile), "Fire")]
        //private class PatchLaser
        //{
        //    private static void Prefix(LaserProjectile __instance)
        //    {
        //        var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else if (wEffect.effectType >= WaterBuoyancy.EffectTypes.LaserProjectile)
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.LaserProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnPool")]
        private class PatchResource
        {
            private static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.ResourceChunk;
                wEffect.GetRBody();
            }
        }
    }

    internal class WaterBuoyancy : MonoBehaviour
    {
        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);
        public static Texture2D CameraFilter;

        public static float Height = -25f,
            FanJetMultiplier = 1.75f,
            ResourceBuoyancyMultiplier = 1.2f,
            BulletDampener = 1E-06f,
            LaserFraction = 0.275f,
            MissileDampener = 0.012f,
            SurfaceSkinning = 0.25f,
            SubmergedTankDampening = 0.4f,
            SubmergedTankDampeningYAddition = 0f,
            SurfaceTankDampening = 0f,
            SurfaceTankDampeningYAddition = 1f,
            RainWeightMultiplier = 0.06f,
            RainDrainMultiplier = 0.06f,
            FloodChangeClamp = 0.002f,
            FloodHeightMultiplier = 15f,
            AbyssDepth = 50f;
            
        public static int SelectedLook = 0;

        private static float NetHeightSmooth = 0f;

        public void Save()
        {
            QPatch._thisMod.WriteConfigJsonFile();
        }

        public static float HeightCalc
        {
            get
            {
                if (ManGameMode.inst.IsCurrentModeMultiplayer())
                {
                    if (!ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                    {
                        return NetHeightSmooth;
                    }
                    else
                    {
                        return -1000f;
                    }
                }
                else
                {
                    return Height + (RainFlood * FloodHeightMultiplier);
                }
            }
        }

        public static float RainFlood = 0f;
        public static float Density = 8;
        public byte heartBeat;
        public static WaterBuoyancy _inst;
        public static GameObject folder;
        public static bool _WeatherMod;
        private bool ShowGUI = false;
        private WaterGUI waterGUI;
        public static GameObject surface;

        internal class WaterGUI : MonoBehaviour
        {
            private Rect Window = new Rect(0, 0, 100, 75);

            private void OnGUI()
            {
                try
                {
                    Window = GUI.Window(29587115, Window, GUIWindow, "Water Settings");
                }
                catch { }
            }

            private void GUIWindow(int ID)
            {
                GUILayout.Label("Height: " + Height.ToString());
                Height = GUILayout.HorizontalSlider(Height, -75f, 100f);
                try
                {
                    if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                    {
                        if (NetworkHandler.ServerWaterHeight != Height)
                        {
                            NetworkHandler.ServerWaterHeight = Height;
                            //ManNetwork.inst.SendToAllClients(WaterChange, new WaterChangeMessage() { Height = ServerWaterHeight }, ManNetwork.inst.MyPlayer.netId);
                            //Console.WriteLine("Sent new water height, changed to " + ServerWaterHeight.ToString());
                        }
                    }
                }
                catch { }
                GUI.DragWindow();
            }
        }

        public static bool IsActive = true;

        public static void SetState()
        {
            _inst.gameObject.SetActive(IsActive);
        }



        internal bool Heart = false;
        private void OnTriggerStay(Collider collider)
        {
            if (Heart != PistonHeart)
            {
                Heart = PistonHeart;
                return;
            }
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Stay(heartBeat);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ent(heartBeat);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ext(heartBeat);
            }
        }

        private void FixedUpdate()
        {
            heartBeat++;
        }

        bool CameraSubmerged = false;
        FogMode fM = RenderSettings.fogMode;
        TOD_FogType todFt;
        TOD_AmbientType todAt;
        float fDens = RenderSettings.fogDensity;
        Color todFogColor;
        Color fogColor = RenderSettings.fogColor;
        Color ambientLight = RenderSettings.ambientLight;

        static Color underwaterColor = new Color(0, 0.2404828f, 1f, 0.5f);

        Gradient dayFogColors;
        Gradient nightFogColors;
        Gradient dayLightColors;
        Gradient nightLightColors;
        Gradient daySkyColors;
        Gradient nightSkyColors;
        Gradient underWaterSkyColors = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            },

            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(underwaterColor, 0f),
                new GradientColorKey(underwaterColor, 1f),
            }
        };


        private void Update()
        {
            if (!IsActive)
            {
                gameObject.SetActive(false);
                folder.transform.position = Vector3.down * 2000f;
                return;
            }
            try
            {
                var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;
                if (Camera.main.transform.position.y < HeightCalc)
                {
                    if (!CameraSubmerged)
                    {
                        CameraSubmerged = true;
                        fogColor = RenderSettings.fogColor;
                        ambientLight = RenderSettings.ambientLight;

                        RenderSettings.fogDensity = 5f;
                        RenderSettings.fog = true;
                        RenderSettings.fogMode = FogMode.Linear;
                        RenderSettings.fogStartDistance = 0f;
                        RenderSettings.fogEndDistance = 40f;

                        todFogColor = sky.FogColor;
                        todFt = sky.Fog.Mode;
                        todAt = sky.Ambient.Mode;
                        dayFogColors = sky.Day.FogColor;
                        nightFogColors = sky.Night.FogColor;
                        daySkyColors = sky.Day.SkyColor;
                        nightSkyColors = sky.Night.SkyColor;
                        dayLightColors = sky.Day.LightColor;
                        nightLightColors = sky.Night.LightColor;
                    }
                }
                else if (CameraSubmerged)
                {
                    CameraSubmerged = false;
                    RenderSettings.fogMode = fM;
                    RenderSettings.fogDensity = fDens;
                    sky.Fog.Mode = todFt;

                    sky.Ambient.Mode = todAt;
                    RenderSettings.fogColor = fogColor;
                    RenderSettings.ambientLight = ambientLight;

                    sky.Day.FogColor = dayFogColors;
                    sky.Night.FogColor = nightFogColors;
                    sky.Day.SkyColor = daySkyColors;
                    sky.Night.SkyColor = nightSkyColors;
                    sky.Day.LightColor = dayLightColors;
                    sky.Night.LightColor = nightLightColors;
                    sky.m_UseTerraTechBiomeData = true;
                }

                if(CameraSubmerged)
                {
                    sky.m_UseTerraTechBiomeData = false;
                    sky.Fog.Mode = TOD_FogType.None;
                    sky.Ambient.Mode = TOD_AmbientType.None;

                    var multiplier = Mathf.Approximately(AbyssDepth, 0) ? 1 : 1 - (Mathf.Max(HeightCalc - Camera.main.transform.position.y, 0) / AbyssDepth);
                    var abyssColor = underwaterColor * multiplier;//Color.Lerp(underwaterColor, Color.black, multiplier);
                    abyssColor.a = 1f;
                    RenderSettings.fogColor = abyssColor;
                    RenderSettings.ambientLight = abyssColor;
                    RenderSettings.ambientGroundColor = abyssColor;
                    RenderSettings.ambientIntensity = 1 - multiplier;

                    underWaterSkyColors.colorKeys[0].color = underWaterSkyColors.colorKeys[1].color = abyssColor;

                    sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underWaterSkyColors;
                }

                ManNetwork mp = ManNetwork.inst;
                bool flag = false;
                if (mp != null && mp.IsMultiplayer())
                {
                    flag = true;
                }

                if (Input.GetKeyDown(QPatch.key) && !Input.GetKey(KeyCode.LeftShift))
                {
                    if (flag)
                    {
                        try
                        {
                            if (!ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                            {
                                if (ManNetwork.IsHost)
                                {
                                    ShowGUI = !ShowGUI;
                                    waterGUI.gameObject.SetActive(ShowGUI);
                                }
                                else
                                {
                                    Console.WriteLine("Tried to change water, but is a client!");
                                }
                            }
                            else
                            {
                                ManUI.inst.ShowErrorPopup("You cannot use water in this gamemode");
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        ShowGUI = !ShowGUI;
                        waterGUI.gameObject.SetActive(ShowGUI);
                        if (!ShowGUI)
                        {
                            QPatch._thisMod.WriteConfigJsonFile();
                        }
                    }

                }
                if (flag && !ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                {
                    NetHeightSmooth = NetHeightSmooth * 0.9f + NetworkHandler.ServerWaterHeight * 0.1f;
                }
                folder.transform.position = new Vector3(Singleton.camera.transform.position.x, HeightCalc, Singleton.camera.transform.position.z);
                /*if (_WeatherMod && !flag)
                {
                    float dTime = Time.deltaTime;
                    float newHeight = RainFlood;
                    //newHeight += WeatherMod.RainWeight * RainWeightMultiplier * dTime;
                    newHeight *= 1f - RainDrainMultiplier * dTime;
                    RainFlood += Mathf.Clamp(newHeight - RainFlood, -FloodChangeClamp * dTime, FloodChangeClamp * dTime);
                }*/
            }
            catch { }
        }

        internal static bool PistonHeart = false;

        internal static void WorldShift()
        {
            PistonHeart = !PistonHeart;
        }

        public static List<WaterLook> waterLooks = new List<WaterLook>();

        public static void Initiate()
        {
            ManWorldTreadmill.inst.OnBeforeWorldOriginMove.Subscribe(WorldShift);
            try
            {
                CameraFilter = new Texture2D(32, 32);
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        CameraFilter.SetPixel(i, j, new Color(
                            0.75f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.015f,
                            0.8f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.01f,
                            0.9f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.02f,
                            0.28f));
                    }
                }
                CameraFilter.Apply();

                var tempGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Mesh plane = Instantiate(tempGO.GetComponent<MeshFilter>().mesh);

                var shader = Shader.Find("Standard");
                if (!shader)
                {
                    IEnumerable<Shader> shaders = Resources.FindObjectsOfTypeAll<Shader>();
                    shaders = shaders.Where(s => s.name == "Standard");
                    shader = shaders.ElementAt(1);
                }
                var defaultWater = new Material(shader)
                {
                    renderQueue = 3000,
                    color = new Color(0.2f, 0.8f, 0.75f, 0.4f)
                };
                defaultWater.SetFloat("_Mode", 2f);
                defaultWater.SetFloat("_Metallic", 0.6f);
                defaultWater.SetFloat("_Glossiness", 0.9f);
                defaultWater.SetInt("_SrcBlend", 5);
                defaultWater.SetInt("_DstBlend", 10);
                defaultWater.SetInt("_ZWrite", 0);
                defaultWater.DisableKeyword("_ALPHATEST_ON");
                defaultWater.EnableKeyword("_ALPHABLEND_ON");
                defaultWater.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                waterLooks.Add(new WaterLook()
                {
                    name = "Default",
                    material = defaultWater,
                    mesh = plane
                });


                Material fancyWavelessWater = new Material(QPatch.assetBundle.LoadAllAssets<Shader>().First(s => s.name == "Shader Forge/CartoonWaterWaveless"));
                fancyWavelessWater.SetFloat("_UseWorldCoordinates", 1f);
                fancyWavelessWater.SetFloat("_RippleDensity", 0.25f);
                fancyWavelessWater.SetFloat("_RippleCutoff", 3.5f);

                waterLooks.Add(new WaterLook()
                {
                    name = "Fancy (waveless)",
                    material = fancyWavelessWater,
                    mesh = plane
                });


                Material fancyWater = new Material(QPatch.assetBundle.LoadAsset<Shader>("CartoonWater"));
                fancyWater.SetFloat("_UseWorldCoordinates", 1f);
                fancyWater.SetFloat("_RippleDensity", 0.25f);
                fancyWater.SetFloat("_RippleCutoff", 3.5f);
                fancyWater.SetFloat("_WaveAmplitude", 5f);
                fancyWater.SetFloat("_Tessellation", 7.5f);

                Mesh fancyMesh = new Mesh();
                fancyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                fancyMesh = OBJParser.MeshFromFile("Assets/plane.obj", fancyMesh);

                waterLooks.Add(new WaterLook()
                {
                    name = "Fancy",
                    material = fancyWater,
                    mesh = fancyMesh
                });
                

                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                WaterBuoyancy.folder = folder;

                GameObject Surface = tempGO;
                Destroy(Surface.GetComponent<MeshCollider>());
                Transform component = Surface.transform; component.parent = folder.transform;
                Surface.GetComponent<Renderer>().material = defaultWater;

                WaterBuoyancy.surface = Surface;

                component.localScale = new Vector3(2048f, 0.075f, 2048f);

                GameObject PhysicsTrigger = new GameObject("PhysicsTrigger");
                Transform PhysicsTriggerTransform = PhysicsTrigger.transform; PhysicsTriggerTransform.parent = folder.transform;
                PhysicsTriggerTransform.localScale = new Vector3(2048f, 2048f, 2048f); PhysicsTriggerTransform.localPosition = new Vector3(0f, -1024f, 0f);
                PhysicsTrigger.AddComponent<BoxCollider>().isTrigger = true;

                _inst = PhysicsTrigger.AddComponent<WaterBuoyancy>();

                int waterlayer = LayerMask.NameToLayer("Water");
                for (int i = 0; i < 32; i++)
                {
                    if (i != waterlayer)
                    {
                        Physics.IgnoreLayerCollision(waterlayer, i, true);
                    }
                }

                GameObject PhysicsCollider = new GameObject("WaterCollider");
                PhysicsCollider.layer = waterlayer;
                Transform PhysicsColliderTransform = PhysicsCollider.transform; PhysicsColliderTransform.parent = folder.transform;
                PhysicsColliderTransform.localScale = new Vector3(2048f, 2048f, 2048f); PhysicsColliderTransform.localPosition = new Vector3(0f, -1024f, 0f);
                PhysicsCollider.AddComponent<BoxCollider>();
                _inst.waterGUI = new GameObject().AddComponent<WaterGUI>();
                _inst.waterGUI.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                WaterParticleHandler.Initialize();
                SurfacePool.Initiate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void UpdateLook(WaterLook waterLook)
        {
            surface.GetComponent<Renderer>().material = waterLook.material;
            surface.GetComponent<MeshFilter>().mesh = waterLook.mesh;
        }

        public class WaterEffect : MonoBehaviour
        {
            public virtual void Ent(byte HeartBeat)
            {
            }

            public virtual void Ext(byte HeartBeat)
            {
            }

            public virtual void Stay(byte HeartBeat)
            {
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class WaterTank : MonoBehaviour
        {
            public Tank tank;
            public Vector3 SubmergeAdditivePos = Vector3.zero;
            public int SubmergeCount = 0;
            public Vector3 SurfaceAdditivePos = Vector3.zero;
            public int SurfaceCount = 0;

            public void Subscribe(Tank tank)
            {
                tank.AttachEvent.Subscribe(AddBlock);
                tank.DetachEvent.Subscribe(RemoveBlock);
                this.tank = tank;
            }

            public void AddGeneralBuoyancy(Vector3 position)
            {
                SubmergeAdditivePos += position;
                SubmergeCount++;
            }

            public void AddSurface(Vector3 position)
            {
                SurfaceAdditivePos += position;
                SurfaceCount++;
            }

            public void AddBlock(TankBlock tankblock, Tank tank)
            {
                tankblock.GetComponent<WaterBlock>().watertank = this;
            }

            public void RemoveBlock(TankBlock tankblock, Tank tank)
            {
                tankblock.GetComponent<WaterBlock>().watertank = null;
            }

            public void FixedUpdate()
            {
                if (SubmergeCount != 0)
                {
                    tank.rbody.AddForceAtPosition(Vector3.up * (Density * 7.5f) * SubmergeCount, SubmergeAdditivePos / SubmergeCount);
                    SubmergeAdditivePos = Vector3.zero;
                    tank.rbody.AddForce(-(tank.rbody.velocity * SubmergedTankDampening + (Vector3.up * (tank.rbody.velocity.y * SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                    SubmergeCount = 0;
                }
                if (SurfaceCount != 0)
                {
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * SurfaceTankDampening + (Vector3.up * (tank.rbody.velocity.y * SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                    SurfaceAdditivePos = Vector3.zero;
                    SurfaceCount = 0;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class WaterBlock : WaterEffect
        {
            public WaterTank watertank;
            public bool isFanJet;
            public MonoBehaviour componentEffect;
            public Vector3 initVelocity;
            public SurfacePool.Item surface = null;
            bool surfaceExist = false;
            private byte heartBeat = 0;
            public TankBlock TankBlock;

            private void Surface()
            {
                if (WaterParticleHandler.UseParticleEffects)
                {
                    if (surface == null || !surface.Using)
                    {
                        surface = SurfacePool.GetFromPool();
                    }

                    if (surface == null)
                    {
                        return;
                    }
                    var e = TankBlock.centreOfMassWorld;
                    surface.UpdatePos(new Vector3(e.x, HeightCalc, e.z));
                    surfaceExist = true;
                }
            }

            public void TryRemoveSurface()
            {
                if (surfaceExist && surface != null)
                {
                    SurfacePool.ReturnToPool(surface);
                    surface = null;
                    surfaceExist = false;
                }
            }

            public void ApplyMultiplierFanJet()
            {
                float num2 = (HeightCalc - componentEffect.transform.position.y + TankBlock.BlockCellBounds.extents.y) / TankBlock.BlockCellBounds.extents.y + 0.1f;
                if (num2 > 0.1f)
                {
                    if (num2 > 1f)
                    {
                        num2 = 1f;
                    }
                    FanJet component = (componentEffect as FanJet);
                    component.force = initVelocity.x * (num2 * FanJetMultiplier + 1);
                    component.backForce = initVelocity.y * (num2 * FanJetMultiplier + 1);
                }
            }

            public void ResetMultiplierFanJet()
            {
                FanJet component = (componentEffect as FanJet);
                component.force = initVelocity.x;
                component.backForce = initVelocity.y;
            }

            public override void Stay(byte HeartBeat)
            {
                if (heartBeat == HeartBeat)
                {
                    return;
                }

                heartBeat = HeartBeat;
                try
                {
                    if (TankBlock.tank != null)
                    {
                        if (watertank == null || watertank.tank != TankBlock.tank)
                        {
                            watertank = TankBlock.tank.GetComponent<WaterTank>();
                        }
                        ApplyConnectedForce();
                        return;
                    }
                    if (TankBlock.rbody == null)
                    {
                        TryRemoveSurface();
                    }
                    else
                    {
                        ApplySeparateForce();
                    }
                }
                catch
                {
                    Debug.Log((watertank == null ? "WaterTank is null..." + (TankBlock.tank == null ? " And so is the tank" : "The tank is not") : "WaterTank exists") + (TankBlock.rbody == null ? "\nTankBlock Rigidbody is null" : "\nWhat?") + (TankBlock.IsAttached ? "\nThe block appears to be attached" : "\nThe block is not attached"));
                }
            }

            public void ApplySeparateForce()
            {
                //Vector3 vector = TankBlock.centreOfMassWorld;
                float Submerge = HeightCalc - TankBlock.centreOfMassWorld.y - TankBlock.BlockCellBounds.extents.y;
                Submerge = Submerge * Mathf.Abs(Submerge) + SurfaceSkinning;
                if (Submerge > 1.5f)
                {
                    TryRemoveSurface();
                    Submerge = 1.5f;
                }
                else if (Submerge < -0.2f)
                {
                    Submerge = -0.2f;
                }
                else
                {
                    Surface();
                }
                TankBlock.rbody.AddForce(Vector3.up * (Submerge * Density * 5f));
            }

            public void ApplyConnectedForce()
            {
                IntVector3[] intVector = TankBlock.filledCells;
                int CellCount = intVector.Length;
                if (CellCount == 1)
                {
                    ApplyConnectedForce_Internal(TankBlock.centreOfMassWorld);
                }
                else
                {
                    for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
                    {
                        ApplyConnectedForce_Internal(transform.TransformPoint(intVector[CellIndex].x, intVector[CellIndex].y, intVector[CellIndex].z));
                    }
                }
                if (this.isFanJet)
                {
                    ApplyMultiplierFanJet();
                }
            }

            private void ApplyConnectedForce_Internal(Vector3 vector)
            {
                float Submerge = HeightCalc - vector.y;
                Submerge = Submerge * Mathf.Abs(Submerge) + SurfaceSkinning;
                if (Submerge >= -0.5f)
                {
                    if (Submerge > 1.5f)
                    {
                        watertank.AddGeneralBuoyancy(vector);
                        TryRemoveSurface();
                        return;
                    }
                    else if (Submerge < -0.2f)
                    {
                        Submerge = -0.2f;
                    }
                    else
                    {
                        Surface();
                        watertank.AddSurface(vector);
                    }
                    watertank.tank.rbody.AddForceAtPosition(Vector3.up * (Submerge * Density * 5f), vector);
                }
            }

            public override void Ent(byte HeartBeat)
            {
                Surface();
                try
                {
                    var val = TankBlock.centreOfMassWorld;
                    WaterParticleHandler.SplashAtPos(new Vector3(val.x, HeightCalc, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
                }
                catch { }
            }

            public override void Ext(byte HeartBeat)
            {
                TryRemoveSurface();
                try
                {
                    if (isFanJet)
                    {
                        ResetMultiplierFanJet();
                    }

                    var val = TankBlock.centreOfMassWorld;
                    WaterParticleHandler.SplashAtPos(new Vector3(val.x, HeightCalc, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
                }
                catch { }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class WaterObj : WaterEffect
        {
            public WaterObj()
            {
                set = HeightCalc > this.transform.position.y + 1;
            }

            //public TankEffect watertank;
            public byte heartBeat = 0;

            public EffectTypes effectType;
            public Component effectBase;
            public bool isProjectile = false;
            public Rigidbody _rbody;
            public Vector3 initVelocity;
            private bool set;

            public void GetRBody()
            {
                switch (effectType)
                {
                    case EffectTypes.ResourceChunk:
                        _rbody = ((ResourcePickup)effectBase).rbody;
                        break;

                    case EffectTypes.LaserProjectile:
                    case EffectTypes.MissileProjectile:
                    case EffectTypes.NormalProjectile:
                        _rbody = ((Projectile)effectBase).GetComponent<Rigidbody>();
                        if (_rbody == null)
                        {
                            _rbody = (effectBase as Projectile).GetComponentInParent<Rigidbody>();
                            if (_rbody == null)
                            {
                                _rbody = (effectBase as Projectile).GetComponentInChildren<Rigidbody>();
                            }
                        }
                        break;
                }
            }

            public override void Stay(byte HeartBeat)
            {
                try
                {
                    if (HeartBeat == heartBeat)
                    {
                        return;
                    }

                    heartBeat = HeartBeat;
                    {
                        switch (effectType)
                        {
                            case EffectTypes.NormalProjectile:
                                _rbody.velocity *= 1f - (Density * BulletDampener);
                                break;

                            case EffectTypes.MissileProjectile:
                                _rbody.velocity *= 1f - (Density * MissileDampener);
                                break;

                            case EffectTypes.ResourceChunk:
                                float num2 = HeightCalc - _rbody.position.y;
                                num2 = num2 * Mathf.Abs(num2) + SurfaceSkinning;
                                if (num2 >= -0.5f)
                                {
                                    if (num2 > 1.5f)
                                    {
                                        num2 = 1.5f;
                                    }

                                    if (num2 < -0.1f)
                                    {
                                        num2 = -0.1f;
                                    }
                                    _rbody.AddForce(Vector3.up * Density * num2 * ResourceBuoyancyMultiplier, ForceMode.Force);
                                    _rbody.velocity -= (_rbody.velocity * _rbody.velocity.magnitude * (1f - Density / 10000f)) * 0.0025f;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    bool flag = _rbody == null;
                    Debug.Log("Exception in Stay: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                    if (flag)
                    {
                        GetRBody();
                    }
                }
            }

            public override void Ent(byte HeartBeat)
            {
                try
                {
                    if (set)
                    {
                        WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, HeightCalc, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);
                    }
                    else
                    {
                        set = true;
                    }
                    if (effectType < EffectTypes.LaserProjectile)
                    {
                        return;
                    }
                    float destroyMultiplier = 4f;
                    if (effectType == EffectTypes.MissileProjectile)
                    {
                        destroyMultiplier += 1f;
                        var managedEvent = ((MissileProjectile)this.effectBase).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                        if (managedEvent.TimeRemaining != 0)
                        {
                            managedEvent.Reset(managedEvent.TimeRemaining * 4f);
                        }
                        //((MissileProjectile)this.effectBase).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                        _rbody.useGravity = false;
                    }
                    else
                    {
                        initVelocity = _rbody.velocity;
                        _rbody.velocity = initVelocity * (1f / (Density * LaserFraction + 1f));
                    }
                    var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                    managedEvent2.Reset(managedEvent2.TimeRemaining * destroyMultiplier);

                    //(this.effectBase as LaserProjectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                    return;
                }
                catch (Exception e)
                {
                    bool flag = _rbody == null;
                    Debug.Log("Exception in Ent: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                    if (flag)
                    {
                        GetRBody();
                    }
                }
            }

            public override void Ext(byte HeartBeat)
            {
                try
                {
                    if (!set)
                    {
                        set = true;
                    }
                    WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, HeightCalc, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);

                    if (effectType < EffectTypes.LaserProjectile)
                    {
                        return;
                    }
                    float destroyMultiplier = 4f;
                    if (effectType == EffectTypes.MissileProjectile)
                    {
                        destroyMultiplier += 1f;
                        var managedEvent = (this.effectBase as MissileProjectile).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                        if (managedEvent.TimeRemaining == 0f)
                        {
                            _rbody.useGravity = true;
                        }
                        else
                        {
                            managedEvent.Reset(managedEvent.TimeRemaining * .25f);
                        }
                        //(this.effectBase as MissileProjectile).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                    }
                    else
                    {
                        _rbody.velocity = initVelocity * (Density * 0.025f * LaserFraction + 1f);
                    }
                    var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                    managedEvent2.Reset(managedEvent2.TimeRemaining / destroyMultiplier);
                    //(this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                    return;
                }
                catch (Exception e)
                {
                    bool flag = _rbody == null;
                    Debug.Log("Exception in Ext: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                    if (flag)
                    {
                        GetRBody();
                    }
                    return;
                }
            }
        }

        public enum EffectTypes : byte
        {
            ResourceChunk = 0,
            LaserProjectile = 2,
            MissileProjectile = 3,
            NormalProjectile = 1
        }

        public struct WaterLook
        {
            public string name;
            public Mesh mesh;
            public Material material;

            public override string ToString()
            {
                return this.name;
            }
        }
    }
}