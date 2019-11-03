using System;
using System.Reflection;
using Harmony;
using QModManager.Utility;
using ModHelper.Config;
using UnityEngine;
using Nuterra.NativeOptions;
using UnityEngine.Events;

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

        public static void Main()
        {
            var harmony = HarmonyInstance.Create("aceba1.ttmm.revived.water");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

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
            WaterBuoyancy._WeatherMod = ModExists("TTQMM WeatherMod");
            if (WaterBuoyancy._WeatherMod)
            {
                Debug.Log("Found WeatherMod!");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainWeightMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainDrainMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "FloodChangeClamp");
                thisMod.BindConfig<WaterBuoyancy>(null, "FloodHeightMultiplier");
            }
            _thisMod = thisMod;


            GUIMenu = new OptionKey("GUI Menu button", ModName, key);
            GUIMenu.onValueSaved.AddListener(() => { key_int = (int)(key = GUIMenu.SavedValue); WaterBuoyancy._inst.Invoke("Save", 0.5f); });

            IsWaterActive = new OptionToggle("Water Active", ModName, WaterBuoyancy.IsActive);
            IsWaterActive.onValueSaved.AddListener(() => { WaterBuoyancy.IsActive = IsWaterActive.SavedValue; WaterBuoyancy.SetState(); });
            UseParticleEffects = new OptionToggle("Particle effects Active", ModName, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => { WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue; });
            Height = new OptionRange("Height level", ModName, WaterBuoyancy.Height, -75f, 100f, 1f);
            Height.onValueSaved.AddListener(() => { WaterBuoyancy.Height = Height.SavedValue; });
            Density = new OptionRange("| Density", ModName, WaterBuoyancy.Density, -16, 16, 0.25f);
            Density.onValueSaved.AddListener(() => { WaterBuoyancy.Density = Density.SavedValue; });
            FanJetMultiplier = new OptionRange("| Fan jet Multiplier", ModName, WaterBuoyancy.FanJetMultiplier, 0f, 4f, .05f);
            FanJetMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FanJetMultiplier = FanJetMultiplier.SavedValue; });
            ResourceBuoyancy = new OptionRange("| Resource Buoyancy", ModName, WaterBuoyancy.ResourceBuoyancyMultiplier, 0f, 4f, .05f);
            ResourceBuoyancy.onValueSaved.AddListener(() => { WaterBuoyancy.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue; });
            BulletDampener = new OptionRange("| Bullet Dampening", ModName, WaterBuoyancy.BulletDampener, 0f, 1E-4f, 1E-8f);
            BulletDampener.onValueSaved.AddListener(() => { WaterBuoyancy.BulletDampener = BulletDampener.SavedValue; });
            MissileDampener = new OptionRange("| Missile Dampening", ModName, WaterBuoyancy.MissileDampener, 0f, 0.1f, 0.003f);
            MissileDampener.onValueSaved.AddListener(() => { WaterBuoyancy.MissileDampener = MissileDampener.SavedValue; });
            LaserFraction = new OptionRange("| Laser Slowdown", ModName, WaterBuoyancy.LaserFraction, 0f, 0.5f, 0.025f);
            LaserFraction.onValueSaved.AddListener(() => { WaterBuoyancy.LaserFraction = LaserFraction.SavedValue; });
            SurfaceSkinning = new OptionRange("| Surface Skinning", ModName, WaterBuoyancy.SurfaceSkinning, -0.5f, 0.5f, 0.05f);
            SurfaceSkinning.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceSkinning = SurfaceSkinning.SavedValue; });
            SubmergedTankDampening = new OptionRange("| Submerged Tank Dampening", ModName, WaterBuoyancy.SubmergedTankDampening, 0f, 2f, 0.05f);
            SubmergedTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampening = SubmergedTankDampening.SavedValue; });
            SubmergedTankDampeningY = new OptionRange("| Submerged Tank Dampening Y addition", ModName, WaterBuoyancy.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f);
            SubmergedTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue; });
            SurfaceTankDampening = new OptionRange("| Surface Tank Dampening", ModName, WaterBuoyancy.SurfaceTankDampening, 0f, 2f, 0.05f);
            SurfaceTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampening = SurfaceTankDampening.SavedValue; });
            SurfaceTankDampeningY = new OptionRange("| Surface Tank Dampening Y addition", ModName, WaterBuoyancy.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f);
            SurfaceTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue; });
            if (WaterBuoyancy._WeatherMod)
            {
                RainWeightMultiplier = new OptionRange("WeatherMod | Rain Weight Multiplier", ModName, WaterBuoyancy.RainWeightMultiplier, 0, 0.25f, 0.01f);
                RainWeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = new OptionRange("WeatherMod | Rain Drain Multiplier", ModName, WaterBuoyancy.RainDrainMultiplier, 0, 0.25f, 0.01f);
                RainDrainMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = new OptionRange("WeatherMod | Flood rate Clamp", ModName, WaterBuoyancy.FloodChangeClamp, 0, 0.08f, 0.001f);
                FloodRateClamp.onValueSaved.AddListener(() => { WaterBuoyancy.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = new OptionRange("WeatherMod | Flood Height Multiplier", ModName, WaterBuoyancy.FloodHeightMultiplier, 0, 50f, 1f);
                FloodHeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FloodHeightMultiplier = FloodHeightMultiplier.SavedValue; });
            }
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
            FloodHeightMultiplier = 15f;

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
                if (Camera.main.transform.position.y < folder.transform.position.y)
                {
                    if (!CameraSubmerged)
                    {
                        CameraSubmerged = true;
                    }
                    else
                    {
                        ManTimeOfDay.inst.EnableSkyDome(true);
                    }
                    ManTimeOfDay.inst.EnableSkyDome(false);
                    //RenderSettings.skybox.color = new Color(0.2f, 0.5f, 0.6f, 0.91f);
                    RenderSettings.fogColor = new Color(0.3f, 0.5f, 0.65f, 0.3f);
                    RenderSettings.ambientLight = new Color(0.3f, 0.5f, 0.6f, 0.8f);
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = 0f;
                    RenderSettings.fogEndDistance = 100f;
                }
                else if (CameraSubmerged)
                {
                    CameraSubmerged = false;
                    RenderSettings.fogMode = fM;
                    ManTimeOfDay.inst.EnableSkyDome(true);
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
                if (_WeatherMod && !flag)
                {
                    float dTime = Time.deltaTime;
                    float newHeight = RainFlood;
                    newHeight += WeatherMod.RainWeight * RainWeightMultiplier * dTime;
                    newHeight *= 1f - RainDrainMultiplier * dTime;
                    RainFlood += Mathf.Clamp(newHeight - RainFlood, -FloodChangeClamp * dTime, FloodChangeClamp * dTime);
                }
            }
            catch { }
        }

        internal static bool PistonHeart = false;

        internal static void WorldShift()
        {
            PistonHeart = !PistonHeart;
        }

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

                Material material = null;
                /*
                Material[] search = Resources.FindObjectsOfTypeAll<Material>();
                for (int i = 0; i < search.Length; i++)
                {
                    if (search[i].name.StartsWith("Mat_Pickup_Resource_RodiusCapsule"))
                    {
                        material = search[i];
                        break;
                    }
                }
                */
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"))
                    {
                        renderQueue = 3000,
                        color = new Color(0.2f, 0.8f, 0.75f, 0.4f)
                    };
                    material.SetFloat("_Mode", 2f); material.SetFloat("_Metallic", 0.6f); material.SetFloat("_Glossiness", 0.9f); material.SetInt("_SrcBlend", 5); material.SetInt("_DstBlend", 10); material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON"); material.EnableKeyword("_ALPHABLEND_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }

                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                WaterBuoyancy.folder = folder;

                GameObject Surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(Surface.GetComponent<CapsuleCollider>());
                Transform component = Surface.transform; component.parent = folder.transform;
                component.localScale = new Vector3(2048f, 0.075f, 2048f);
                Surface.GetComponent<Renderer>().material = material;

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
    }
}