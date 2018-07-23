using System;
using Harmony;
using System.Reflection;
using UnityEngine;
using QModInstaller;

namespace WaterMod
{
    class QPatch
    {
        public static void Main()
        {
            var harmony = HarmonyInstance.Create("aceba1.ttmm.revive.water");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            WaterBuoyancy.Initiate();
        }
    }

    class Patches
    {
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnSpawn")]
        class Patch1
        {
            static void Postfix(TankBlock __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.TankBlock;
                wEffect.rbody = __instance.rbody;
            }
        }
        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch("OnSpawn")]
        class Patch2
        {
            static void Postfix(Projectile __instance)
            {
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterEffect>();
                if (wEffect != null)
                    return;
                wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.NormalProjectile;
                wEffect.rbody = __instance.rbody;
            }
        }
        [HarmonyPatch(typeof(LaserProjectile))]
        [HarmonyPatch("OnSpawn")]
        class Patch3
        {
            static void Postfix(LaserProjectile __instance)
            {
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterEffect>();
                if (wEffect == null)
                    wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.LaserProjectile;
                wEffect.rbody = __instance.rbody;
            }
        }
        [HarmonyPatch(typeof(MissileProjectile))]
        [HarmonyPatch("OnSpawn")]
        class Patch4
        {
            static void Postfix(MissileProjectile __instance)
            {
                var wEffect = __instance.GetComponent<WaterBuoyancy.WaterEffect>();
                if (wEffect == null)
                    wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
                wEffect.rbody = __instance.rbody;
            }
        }
        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnSpawn")]
        class Patch5
        {
            static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterEffect>();
                wEffect.effectBase = __instance;
                wEffect.effectType = WaterBuoyancy.EffectTypes.ResourceChunk;
                wEffect.rbody = __instance.rbody;
            }
        }
    }

    class WaterBuoyancy : MonoBehaviour
    {
        public static Texture2D CameraFilter;
        public static float Height = 25f;
        public static float Density = 8f;
        public byte heartBeat;
        public static WaterBuoyancy _inst;
        public GameObject folder;

        private void OnGUI()
        {
            if (Camera.main.transform.position.y < folder.transform.position.y)
            {
                GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), CameraFilter, ScaleMode.ScaleAndCrop);
            }
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
            folder.transform.position = new Vector3(Singleton.camera.transform.position.x, folder.transform.position.y, Singleton.camera.transform.position.z);
        }

        public static void Initiate()
        {
            CameraFilter = new Texture2D(32, 32);
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    CameraFilter.SetPixel(i, j, new Color(
                        0.7f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.02f,
                        0.8f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.01f,
                        1f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.03f,
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

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(gameObject.GetComponent<CapsuleCollider>());
            Transform component = gameObject.GetComponent<Transform>(); component.SetParent(folder.transform);
            component.localScale = new Vector3(2048f, 0.075f, 2048f); component.localPosition = new Vector3(0f, 0f, 0f);
            gameObject.GetComponent<Renderer>().material = material;

            GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(gameObject2.GetComponent<Renderer>());
            Transform component2 = gameObject2.GetComponent<Transform>(); component2.SetParent(folder.transform);
            component2.localScale = new Vector3(2048f, 1024f, 2048f); component2.localPosition = new Vector3(0f, -512f, 0f);
            gameObject2.GetComponent<BoxCollider>().isTrigger = true;

            WaterBuoyancy._inst = gameObject2.AddComponent<WaterBuoyancy>();
            WaterBuoyancy._inst.folder = folder;
        }

        //////////////////////////////////////////////////////////////////////////

        public class WaterEffect : MonoBehaviour
        {
            public byte heartBeat;
            public EffectTypes effectType;
            public Rigidbody rbody;
            public object effectBase;
            public bool isProjectile;
            public void ApplyForce(byte HeartBeat)
            {
                if (HeartBeat == heartBeat)
                    return;
                heartBeat = HeartBeat;
                if (effectType == EffectTypes.TankBlock)
                {
                    Vector3 angularVelocity = rbody.angularVelocity;
                    IntVector3[] intVector = (effectBase as TankBlock).filledCells;
                    int num = intVector.Length;
                    Vector3 thing = (rbody.velocity.magnitude < 0.8f ? rbody.velocity : rbody.velocity.normalized * 0.8f) * (Density / 40000f);
                    for (int i = 0; i < num; i++)
                    {
                        Vector3 vector = transform.TransformPoint(intVector[i].x, intVector[i].y, intVector[i].z);
                        float num2 = Height - vector.y - (effectBase as TankBlock).BlockCellBounds.extents.y;
                        num2 = num2 * num2 - 1f;
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
                            rbody.AddForceAtPosition(Vector3.up * (num2 * Density * 5f - rbody.velocity.y * num2 * 0.2f), vector);
                            Vector3 force = thing * num2 / num;
                            rbody.AddForce(force);

                        }
                    }
                }
                {
                    switch (effectType)
                    {
                        case EffectTypes.NormalProjectile:
                        case EffectTypes.MissileProjectile:
                            rbody.velocity += (rbody.velocity - rbody.velocity * rbody.velocity.magnitude * (1f - Density / 20000f)) * 0.0025f;
                            break;
                        case EffectTypes.ResourceChunk:
                            float num2 = Height - rbody.position.y;
                            num2 = num2 * num2 - 1f;
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
                                rbody.AddForce(Vector3.up * Density * num2, ForceMode.Force);
                                rbody.velocity += (rbody.velocity - rbody.velocity * rbody.velocity.magnitude * (1f - Density / 20000f)) * 0.0025f;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            public bool ApplyForceEnter(byte HeartBeat)
            {
                if (heartBeat == HeartBeat)
                    return false;
                heartBeat = HeartBeat;
                if (isProjectile)
                {
                    switch (effectType)
                    {
                        case EffectTypes.MissileProjectile:
                            var managedEvent = (this.effectBase as MissileProjectile).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                            managedEvent.Reset(managedEvent.TimeRemaining * 8f);
                            (this.effectBase as MissileProjectile).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                            break;
                        case EffectTypes.LaserProjectile:
                            var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                            managedEvent2.Reset(managedEvent2.TimeRemaining * 8f);
                            (this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent",managedEvent2);
                            rbody.velocity = rbody.velocity * 0.3f;
                            break;
                    }
                }
                return true;
            }
            public bool ApplyForceExit(byte HeartBeat)
            {
                if (heartBeat == HeartBeat)
                    return false;
                heartBeat = HeartBeat;
                if (isProjectile)
                {
                    switch (effectType)
                    {
                        case EffectTypes.MissileProjectile:
                            var managedEvent = (this.effectBase as MissileProjectile).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                            managedEvent.Reset(managedEvent.TimeRemaining * .125f);
                            (this.effectBase as MissileProjectile).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                            break;
                        case EffectTypes.LaserProjectile:
                            var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                            managedEvent2.Reset(managedEvent2.TimeRemaining * .125f);
                            (this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);
                            rbody.velocity = rbody.velocity * 4f;
                            break;
                    }
                }
                return true;
            }
        }
        public enum EffectTypes : byte
        {
            TankBlock = 3,
            ResourceChunk = 4,
            LaserProjectile = 1,
            MissileProjectile = 2,
            NormalProjectile = 0
        }
    }
}