using Harmony;
using QModManager.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ModHelper.Config;

namespace WaterMod
{
    public class QPatch
    {
        public static ModConfig _thisMod;

        public static void Main()
        {
            var harmony = HarmonyInstance.Create("aceba1.ttmm.revived.water");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            WaterBuoyancy.Initiate();

            ModConfig thisMod = new ModConfig();
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
            _thisMod = thisMod;
        }
    }

    internal class Patches
    {
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnSpawn")]
        private class Patch1
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

        [HarmonyPatch(typeof(Tank))]
        [HarmonyPatch("OnSpawn")]
        private class Patch5
        {
            private static void Postfix(Tank __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterTank>();
                wEffect.Subscribe(__instance);
            }
        }

        //[HarmonyPatch(typeof(TankBlock))]
        //[HarmonyPatch("OnAttach")]
        //private class Patch1_1
        //{
        //    private static void Postfix(TankBlock __instance)
        //    {
        //        var wEffect = __instance.gameObject.GetComponent<WaterBuoyancy.WaterEffect>();
        //        wEffect.watertank = __instance.tank.GetComponent<WaterBuoyancy.TankEffect>();
        //    }
        //}

        //         [HarmonyPatch(typeof(TankBlock))]
        //         [HarmonyPatch("OnDetach")]
        //         class Patch1_2
        //         {
        //             static void Postfix(TankBlock __instance)
        //             {
        //                 var wEffect = __instance.gameObject.GetComponent<WaterBuoyancy.WaterEffect>();
        //                 if (wEffect != null)
        //                     wEffect.GetTankBlockRBody();
        //             }
        //         }

        [HarmonyPatch(typeof(LaserProjectile))]
        [HarmonyPatch("OnSpawn")]
        private class Patch2
        {
            private static void Postfix(LaserProjectile __instance)
            {
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
                if (wEffect != null && wEffect.effectType < WaterBuoyancy.EffectTypes.NormalProjectile)
                    return;
                wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.LaserProjectile;
                wEffect.GetRBody();
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch("OnSpawn")]
        private class Patch3
        {
            private static void Postfix(Projectile __instance)
            {
                var __minstance = __instance.GetComponent<MissileProjectile>();
                if (__minstance != null)
                {
                    Collider collider = __minstance.GetComponentInChildren<Collider>();
                    var wEffect2 = collider.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                    wEffect2.effectBase = __minstance;
                    wEffect2.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
                    wEffect2.GetRBody();
                }
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
                if (wEffect != null)
                    return;
                wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.NormalProjectile;
                wEffect.GetRBody();
            }
        }

        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnSpawn")]
        private class Patch4
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
            MissileDampener = 0.011f,
            SurfaceSkinning = 0.75f,
            SubmergedTankDampening = 0.2f,
            SubmergedTankDampeningYAddition = 0f,
            SurfaceTankDampening = 0.1f,
            SurfaceTankDampeningYAddition = 1f;

        public static int Density = 8;
        public byte heartBeat;
        public static WaterBuoyancy _inst;
        public GameObject folder;
        private bool ShowGUI = false;
        private Rect Window = new Rect(0, 0, 100, 100);

        private void OnGUI()
        {
            if (Camera.main.transform.position.y < folder.transform.position.y)
            {
                GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), CameraFilter, ScaleMode.ScaleAndCrop);
            }
            if (ShowGUI)
            {
                Window = GUI.Window(0, Window, GUIWindow, "WaterSettings");
            }
        }

        private void GUIWindow(int ID)
        {
            GUI.Label(new Rect(0, 20, 100, 20), "Water Height");
            Height = GUI.HorizontalSlider(new Rect(0, 40, 100, 15), Height, -75f, 100f);

            if (GUI.Button(new Rect(0, 60, 100, 20), "Save"))
                QPatch._thisMod.WriteConfigJsonFile();
            if (GUI.Button(new Rect(0, 80, 100, 20), "Reload"))
                QPatch._thisMod.ReadConfigJsonFile();
            GUI.DragWindow();
        }

        private void OnTriggerStay(Collider collider)
        {
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                ShowGUI = !ShowGUI;
            }
            folder.transform.position = new Vector3(Singleton.camera.transform.position.x, Height, Singleton.camera.transform.position.z);
        }

        public static void Initiate()
        {
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
                            0.9f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.025f,
                            0.26f));
                    }
                }
                CameraFilter.Apply();

                Material material = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.2f, 1f, 1f, 0.45f),
                    renderQueue = 3000
                };
                material.SetFloat("_Mode", 2f); material.SetFloat("_Metallic", 0.8f); material.SetFloat("_Glossiness", 0.8f); material.SetInt("_SrcBlend", 5); material.SetInt("_DstBlend", 10); material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON"); material.EnableKeyword("_ALPHABLEND_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(gameObject.GetComponent<CapsuleCollider>());
                Transform component = gameObject.GetComponent<Transform>(); component.SetParent(folder.transform);
                component.localScale = new Vector3(2048f, 0.075f, 2048f); component.localPosition = new Vector3(0f, 0f, 0f);
                gameObject.GetComponent<Renderer>().material = material;

                GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(gameObject2.GetComponent<Renderer>());
                Transform component2 = gameObject2.GetComponent<Transform>(); component2.SetParent(folder.transform);
                component2.localScale = new Vector3(2048f, 1024f, 2048f); component2.localPosition = new Vector3(0f, -512f, 0f);
                gameObject2.GetComponent<BoxCollider>().isTrigger = true;

                WaterBuoyancy._inst = gameObject2.AddComponent<WaterBuoyancy>();
                WaterBuoyancy._inst.folder = folder;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                WaterParticleHandler.Initialize();
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
                tank.AttachEvent.Subscribe(RemoveBlock);
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
                    tank.rbody.AddForce(-(tank.rbody.velocity * SubmergedTankDampening + (Vector3.up * (tank.rbody.velocity.y * SubmergedTankDampeningYAddition))) * (float)SubmergeCount, ForceMode.Force);
                    SubmergeCount = 0;
                }
                if (SurfaceCount != 0)
                {

                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * SurfaceTankDampening + (Vector3.up * (tank.rbody.velocity.y * SurfaceTankDampeningYAddition))) * (float)SurfaceCount, SurfaceAdditivePos, ForceMode.Force);
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
            public GameObject surface;
            byte heartBeat = 0;
            public TankBlock TankBlock;

            private void Surface()
            {
                if (surface == null)
                {
                    surface = GameObject.Instantiate(WaterParticleHandler.oSurface);
                    surface.transform.parent = TankBlock.trans;
                    surface.GetComponent<ParticleSystem>().Play();
                }
                var e = TankBlock.centreOfMassWorld;
                surface.transform.position = new Vector3(e.x, Height, e.z);
            }
            private void TryRemoveSurface()
            {
                if (surface != null)
                {
                    surface.GetComponent<ParticleSystem>().Stop();
                    GameObject.Destroy(surface, 2.5f);
                    surface = null;
                }
            }

            

            public void ApplyMultiplierFanJet()
            {
                float num2 = (Height - componentEffect.transform.position.y + TankBlock.BlockCellBounds.extents.y) / TankBlock.BlockCellBounds.extents.y + 0.1f;
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
                    return;
                heartBeat = HeartBeat;
                try
                {
                    if (TankBlock.rbody == null)
                    {
                        if (watertank == null || watertank.tank != TankBlock.tank)
                        {
                            watertank = TankBlock.tank.GetComponent<WaterTank>();
                        }
                        ApplyConnectedForce();
                        return;
                    }
                }
                catch (Exception ee)
                {
                    Debug.LogException(ee);
                    Debug.Log(watertank == null ? "WaterTank is null..."+ (TankBlock.tank == null ? " And so is the tank" : "The tank is not") : "WaterTank exists");
                }
                try
                {
                    ApplySeparateForce();
                }
                catch(Exception ee)
                {
                    Debug.LogException(ee);
                    Debug.Log(TankBlock.rbody == null ? "TankBlock Rigidbody is null" : "What?");
                }
            }

            public void ApplySeparateForce()
            {
                Vector3 vector = TankBlock.centreOfMassWorld;
                float Submerge = Height - TankBlock.centreOfMassWorld.y - TankBlock.BlockCellBounds.extents.y;
                Submerge = Submerge * Mathf.Abs(Submerge) + SurfaceSkinning;
                if (Submerge > 1.5f)
                {
                    TryRemoveSurface();
                    Submerge = 1.5f;
                }
                else if (Submerge < -0.1f)
                {
                    Submerge = -0.1f;
                }
                else
                {
                    Surface();
                    
                }
                TankBlock.rbody.AddForceAtPosition(Vector3.up * (Submerge * Density * 5f), vector);
            }

            public void ApplyConnectedForce()
            {
                Rigidbody _rbody = watertank.tank.rbody;
                IntVector3[] intVector = TankBlock.filledCells;
                int CellCount = intVector.Length;
                if (CellCount == 1)
                {
                    intVector[0] = TankBlock.CentreOfMass;
                }

                for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
                {
                    Vector3 vector = transform.TransformPoint(intVector[CellIndex].x, intVector[CellIndex].y, intVector[CellIndex].z);
                    float Submerge = Height - vector.y - TankBlock.BlockCellBounds.extents.y;
                    Submerge = Submerge * Mathf.Abs(Submerge) + SurfaceSkinning;
                    if (Submerge >= -0.5f)
                    {
                        if (Submerge > 1.5f)
                        {
                            watertank.AddGeneralBuoyancy(vector);
                            TryRemoveSurface();
                            continue;
                        }
                        else if (Submerge < -0.1f)
                        {
                            Submerge = -0.1f;
                        }
                        else
                        {
                            Surface();
                            watertank.AddSurface(vector);
                        }
                        _rbody.AddForceAtPosition(Vector3.up * (Submerge * Density * 5f), vector);
                    }
                }
                if (this.isFanJet)
                {
                    ApplyMultiplierFanJet();
                }
            }
            public override void Ent(byte HeartBeat)
            {
                Surface();
                try
                {
                    var val = TankBlock.centreOfMassWorld;
                    WaterParticleHandler.SplashAtPos(new Vector3(val.x, Height, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
                }
                catch { }
            }
            public override void Ext(byte HeartBeat)
            {
                TryRemoveSurface();
                try
                {
                    if (isFanJet)
                        ResetMultiplierFanJet();
                    var val = TankBlock.centreOfMassWorld;
                    WaterParticleHandler.SplashAtPos(new Vector3(val.x, Height, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
                }
                catch { }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class WaterObj : WaterEffect
        {
            //public TankEffect watertank;
            public byte heartBeat = 0;
            public EffectTypes effectType;
            public Component effectBase;
            public bool isProjectile = false;
            public Rigidbody _rbody;
            public Vector3 initVelocity;

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
                        return;
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
                                float num2 = Height - _rbody.position.y;
                                num2 =num2 * Mathf.Abs(num2) + SurfaceSkinning;
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
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString() + (_rbody == null ? "\nRigidbody is null!" : ""));
                }
            }

            public override void Ent(byte HeartBeat)
            {
                WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, Height, effectBase.transform.position.z), _rbody.velocity.y, 0.5f);
                try
                {
                    if (effectType < EffectTypes.LaserProjectile)
                    {
                        return;
                    }
                    float destroyMultiplier = 4f;
                    if (effectType == EffectTypes.MissileProjectile)
                    {
                        destroyMultiplier += 1f;
                        var managedEvent = ((MissileProjectile)this.effectBase).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                        if (managedEvent.TimeRemaining != 0) managedEvent.Reset(managedEvent.TimeRemaining * 4f);
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
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString() + (_rbody == null ? "\nRigidbody is null!" : ""));
                    return;
                }
            }

            public override void Ext(byte HeartBeat)
            {
                WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, Height, effectBase.transform.position.z), _rbody.velocity.y, 0.5f);
                try
                {
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
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString() + (_rbody == null ? "\nRigidbody is null!":""));
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