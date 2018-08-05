using Harmony;
using QModInstaller;
using System;
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
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.TankBlock;
                wEffect.GetTankBlockRBody();
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
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterEffect>();
                if (wEffect != null && wEffect.effectType < WaterBuoyancy.EffectTypes.NormalProjectile)
                    return;
                wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
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
                    var wEffect2 = collider.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                    wEffect2.effectBase = __minstance;
                    wEffect2.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
                    wEffect2.GetRBody();
                }
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterEffect>();
                if (wEffect != null)
                    return;
                wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
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
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
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
            MissileDampener = 0.011f;

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
                ModConfig.ReadConfigJsonFile(QPatch._thisMod);
            GUI.DragWindow();
        }

        private void OnTriggerStay(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.ApplyForce(heartBeat);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.ApplyForceEnter(heartBeat);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.ApplyForceExit(heartBeat);
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

            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.2f, 1f, 1f, 0.45f);
            material.SetFloat("_Mode", 2f); material.SetFloat("_Metallic", 0.8f); material.SetFloat("_Glossiness", 0.8f); material.SetInt("_SrcBlend", 5); material.SetInt("_DstBlend", 10); material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON"); material.EnableKeyword("_ALPHABLEND_ON"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class TankEffect : MonoBehaviour
        {
            public Tank tank;
            public Vector3 SubmergeAdditivePos;
            public int SubmergeCount;
            public Vector3 Dampener;

            public void FixedUpdate()
            {
                tank.rbody.AddForceAtPosition(tank.rbody.velocity * -0.001f * Density, SubmergeAdditivePos / SubmergeCount);
                SubmergeAdditivePos = Vector3.zero;
                SubmergeCount = 0;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class WaterEffect : MonoBehaviour
        {
            public WaterEffect()
            {
                heartBeat = 0;
                isProjectile = false;
                renderers = this.GetComponentsInChildren<MeshRenderer>(true);
            }

            //public TankEffect watertank;
            public byte heartBeat;
            public EffectTypes effectType;
            public object effectBase;
            public bool isProjectile;
            public Rigidbody _rbody;
            public Vector3 initVelocity;
            public bool isFanJet;
            public MonoBehaviour componentEffect;
            public MeshRenderer[] renderers;
            private float Dampness = 0f;


            public void GetTankBlockRBody()
            {
                var tankblock = (effectBase as TankBlock);
                if (tankblock.IsAttached)
                {
                    _rbody = tankblock.tank.rbody;
                }
                else
                {
                    _rbody = tankblock.rbody;
                }
                if (_rbody == null)
                {
                    _rbody = tankblock.GetComponent<Rigidbody>();
                    if (_rbody == null)
                    {
                        _rbody = tankblock.GetComponentInParent<Rigidbody>();
                    }
                }
            }

            public void GetRBody()
            {
                switch (effectType)
                {
                    case EffectTypes.TankBlock:
                        GetTankBlockRBody();
                        break;

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

            public void ApplyMultiplierFanJet(TankBlock tankblock)
            {
                float num2 = (Height - componentEffect.transform.position.y + tankblock.BlockCellBounds.extents.y) / tankblock.BlockCellBounds.extents.y + 0.1f;
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
            //public void Update()
            //{
            //    foreach(MeshRenderer renderer in renderers)
            //    {
            //        try
            //        {
            //            renderer.material.SetFloat("_Glossiness", Dampness);
            //            renderer.material.SetFloat("_Metallic", Dampness);
            //        }
            //        catch
            //        {
            //        }
            //    }
            //    Dampness -= 0.01f;
            //    if (Dampness < 0f) Dampness = 0f;
            //}
            public void ApplyForce(byte HeartBeat)
            {
                Dampness += 0.07f;
                if (Dampness > 1f) Dampness = 1f;
                try
                {
                    if (HeartBeat == heartBeat)
                        return;
                    heartBeat = HeartBeat;
                    if (effectType == EffectTypes.TankBlock)
                    {
                        GetTankBlockRBody();
                        var tankblock = (effectBase as TankBlock);
                        Vector3 angularVelocity = _rbody.angularVelocity;
                        IntVector3[] intVector = tankblock.filledCells;
                        if (intVector == null || intVector.Length <= 1)
                        {
                            intVector = new IntVector3[] { IntVector3.zero };
                        }
                        int num = intVector.Length;
                        Vector3 thing = (_rbody.velocity.magnitude < 0.8f ? _rbody.velocity : _rbody.velocity.normalized * 0.8f) * -(Density / 8500f);
                        for (int i = 0; i < num; i++)
                        {
                            Vector3 localpos = new Vector3(intVector[i].x, intVector[i].y, intVector[i].z);
                            Vector3 vector;
                            if (num == 1)
                                vector = tankblock.centreOfMassWorld;
                            else
                                vector = transform.TransformPoint(localpos);
                            float num2 = Height - vector.y - tankblock.BlockCellBounds.extents.y;
                            num2 = num2 * Mathf.Abs(num2) + 0.25f;
                            if (num2 >= -0.5f)
                            {
                                float yEffector = _rbody.velocity.y * Density * 0.1f;
                                if (num2 > 1.5f)
                                {
                                    num2 = 1.5f;
                                }
                                else if (num2 < -0.1f)
                                {
                                    num2 = -0.1f;
                                }
                                else
                                {
                                    yEffector *= 4f;
                                }
                                _rbody.AddForceAtPosition(Vector3.up * (num2 * Density * 5f - yEffector), vector);
                                //if (tankblock.IsAttached)
                                //{
                                //    watertank.SubmergeAdditivePos += localpos;
                                //    watertank.SubmergeCount++;
                                //}
                            }
                        }
                        _rbody.AddForce(thing);
                        if (this.isFanJet)
                        {
                            ApplyMultiplierFanJet(tankblock);
                        }
                    }
                    else
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
                                num2 *= Mathf.Abs(num2);// - 1f;
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
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString());
                }
            }

            public bool ApplyForceEnter(byte HeartBeat)
            {
                try
                {
                    if (effectType < EffectTypes.LaserProjectile)
                    {
                        if (effectType == EffectTypes.TankBlock)
                        {
                            ((TankBlock)effectBase).GetComponent<MeshRenderer>().material.shader = Shader.Find("Specular");
                            return true;
                        }
                        return false;
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

                    return true;
                }
                catch (Exception e)
                {
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString());
                    return false;
                }
            }

            public bool ApplyForceExit(byte HeartBeat)
            {
                try
                {
                    if (effectType < EffectTypes.LaserProjectile)
                    {
                        if (effectType == EffectTypes.TankBlock && isFanJet)
                        {
                            ResetMultiplierFanJet();
                            return true;
                        }
                        return false;
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

                    return true;
                }
                catch (Exception e)
                {
                    Debug.Log("Exception: " + e.Message + "\n efectType: " + effectType.ToString());
                    return false;
                }
            }
        }

        public enum EffectTypes : byte
        {
            TankBlock = 0,
            ResourceChunk = 1,
            LaserProjectile = 3,
            MissileProjectile = 4,
            NormalProjectile = 2
        }
    }
}