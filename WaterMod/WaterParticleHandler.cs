using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaterMod
{
    public static class WaterParticleHandler
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

            blurredMat = new Material(shader);
            blurredMat.mainTexture = blurredSprite;
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
        }
        private static void CreateSurface()
        {
            var ps = oSurface.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 2.5f;
            m.playOnAwake = true; //change later
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
        }

        public static void SplashAtPos(Vector3 pos, float Speed, float radius)
        {
            var emitparams = new ParticleSystem.EmitParams();
            emitparams.position = pos;
            float sp = Mathf.Clamp(Mathf.Abs(Speed) * 0.25f, 0.1f, 8f);
            float sqp = Mathf.Sqrt(sp);
            emitparams.startLifetime = 0.1f + sqp * 0.4f;
            emitparams.startSize3D = new Vector3(sqp + radius, sp, 1f);
            FXSplash.Emit(emitparams, 1);
        }
    }
}