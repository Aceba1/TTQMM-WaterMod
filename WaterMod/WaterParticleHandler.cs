using System.Collections.Generic;
using UnityEngine;

namespace WaterMod
{
    public class WaterParticleHandler
    {
        public static Texture2D blurredSprite;
        public static Material blurredMat;
        public static Material filledMat;
        public static Material spriteMaterial;
        private static GameObject FXFolder;
        public static GameObject oSplash;
        public static GameObject oSurface;
        public static ParticleSystem FXSplash;
        public static ParticleSystem FXSurface;

        public static bool UseParticleEffects = true;

        public static ParticleSystem.MinMaxGradient WaterGradient = new ParticleSystem.MinMaxGradient(
                new Gradient()
                {
                    alphaKeys = new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.3f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                    },
                    colorKeys = new GradientColorKey[] {
                    new GradientColorKey(new Color(0.561f, 0.937f, 0.875f), 0.5f),
                    new GradientColorKey(new Color(0f, 0.69f, 1f), 1f)
                    },
                    mode = GradientMode.Blend
                });

        public static void Initialize()
        {
            FXFolder = new GameObject("WaterModFX");

            oSplash = new GameObject("Splash");
            oSurface = new GameObject("Surface");

            oSplash.transform.parent = FXFolder.transform;
            oSurface.transform.parent = FXFolder.transform;
            CreateBlurredSprite();
            Debug.Log("Created Sprite 1");
            CreateSpriteMaterial();
            Debug.Log("Created Sprite material");
            CreateSplash();
            Debug.Log("Created ParticleSystem Splash");
            CreateSurface();
            Debug.Log("Created ParticleSystem Surface");
        }

        private static void CreateSpriteMaterial()
        {
            foreach (var Shader in GameObject.FindObjectsOfType<Shader>())
            {
                Debug.Log(Shader.name);
            }
            var shader = Shader.Find("Particles/Additive");
            spriteMaterial = new Material(shader);

            blurredMat = new Material(shader)
            {
                mainTexture = blurredSprite
            };
        }

        private static void CreateBlurredSprite()
        {
            int radius = 8;
            blurredSprite = new Texture2D(radius * 2, radius * 2);
            for (int y = 0; y < radius * 2; y++)
            {
                for (int x = 0; x < radius * 2; x++)
                {
                    blurredSprite.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.8f - Mathf.Sqrt((y - radius) * (y - radius) + (x - radius) * (x - radius)) / radius)));
                }
            }
            blurredSprite.Apply();
        }

        private static void CreateSplash()
        {
            var ps = oSplash.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = .8f;
            m.startSize3D = true;
            m.playOnAwake = false;
            m.maxParticles = 500;
            m.startSpeed = 0f;
            m.loop = false;

            var e = ps.emission;
            e.rateOverTime = 16f;

            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = 0.2f;
            s.rotation = Vector3.right * 90f;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = WaterGradient;

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.separateAxes = true;
            sz.x = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
            sz.z = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
            var ac = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f); ac.AddKey(new Keyframe(0.5f, 1f));
            sz.y = new ParticleSystem.MinMaxCurve(6f, ac);

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            r.material = blurredMat;
            r.maxParticleSize = 20f;

            FXSplash = ps;
            ps.Stop();
        }

        private static void CreateSurface()
        {
            var ps = oSurface.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 2.5f;
            m.playOnAwake = false; //change later
            m.maxParticles = 500;
            m.startSpeed = 0f;
            m.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            var e = ps.emission;
            e.rateOverTime = 1f;
            e.rateOverDistance = 0.5f;

            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = 0.2f;
            s.rotation = Vector3.right * 90f;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = WaterGradient;

            var o = ps.sizeOverLifetime;
            o.enabled = true;
            o.size = new ParticleSystem.MinMaxCurve(16f, AnimationCurve.Linear(0f, 0.05f, 1f, 1f));

            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.y = 0.25f;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.material = blurredMat;
            r.maxParticleSize = 20f;

            FXSurface = ps;
            ps.Stop();
            oSurface.AddComponent<SurfacePool.Item>();
        }

        public static void SplashAtPos(Vector3 pos, float Speed, float radius)
        {
            if (!UseParticleEffects)
                return;
            float sp = Mathf.Clamp(Mathf.Abs(Speed) * 0.25f, 0.1f, 8f);
            float sqp = Mathf.Sqrt(sp);
            var emitparams = new ParticleSystem.EmitParams
            {
                position = pos,
                startLifetime = 0.1f + sqp * 0.4f,
                startSize3D = new Vector3(sqp + radius, sp, 1f)
            };
            FXSplash.Emit(emitparams, 1);
        }
    }

    public class SurfacePool
    {
        public static int SurfaceEffectStartPoolSize = 800;
        public static bool CanGrow = true;
        private static List<GameObject> FreeList;
        public static int Count { get; private set; }
        public static int Available { get; set; }

        public static void Initiate()
        {
            Count = 0;
            Available = 0;
            FreeList = new List<GameObject>();
            if (!WaterParticleHandler.UseParticleEffects)
            {
                return;
            }
            for (int i = 0; i < SurfaceEffectStartPoolSize; i++)
            {
                FreeList.Add(CreateNew());
            }
            Available = SurfaceEffectStartPoolSize;
        }

        public static GameObject GetFromPool()
        {
            GameObject ps;
            if (Available != 0)
            {
                Available--;
                ps = FreeList[Available];
                ps.GetComponent<Item>().StartUsing();
                FreeList.RemoveAt(Available);
                return ps;
            }
            ps = CreateNew(true);
            Debug.Log("Overflow! Added 1 to pool: " + Count.ToString());
            ps.GetComponent<ParticleSystem>().Play();
            return ps;
        }

        public static void ReturnToPool(GameObject surface, float seconds)
        {
            surface.GetComponent<ParticleSystem>().Stop();
            SurfacePool.Available++;
            SurfacePool.FreeList.Add(surface);
            surface.GetComponent<Item>().SetDestroy(seconds);
        }

        private static GameObject CreateNew(bool SetActive = false)
        {
            var s = GameObject.Instantiate(WaterParticleHandler.oSurface);
            s.SetActive(SetActive);
            Count++;
            return s;
        }

        public class Item : MonoBehaviour
        {
            public bool Using = true;

            public void SetDestroy(float seconds)
            {
                Using = false;
                Invoke("Destroy", seconds);
            }

            private void Destroy()
            {
                if (!Using)
                    gameObject.SetActive(false);
            }

            public void StartUsing()
            {
                Using = true;
                gameObject.SetActive(true);
                gameObject.GetComponent<ParticleSystem>().Play();
            }
        }
    }
}