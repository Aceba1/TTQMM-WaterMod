using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace WaterMod
{
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
            QPatch.Height.Value = Height;
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

                if (CameraSubmerged)
                {
                    sky.m_UseTerraTechBiomeData = false;
                    sky.Fog.Mode = TOD_FogType.None;
                    sky.Ambient.Mode = TOD_AmbientType.None;

                    var multiplier = Mathf.Approximately(AbyssDepth, 0) ? 1 : 1 - (Mathf.Max(HeightCalc - Camera.main.transform.position.y, 0) / AbyssDepth);
                    var abyssColor = underwaterColor * multiplier;
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
                            this.Save();
                        }
                    }

                }
                if (flag && !ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                {
                    NetHeightSmooth = NetHeightSmooth * 0.9f + NetworkHandler.ServerWaterHeight * 0.1f;
                }
                folder.transform.position = new Vector3(Singleton.camera.transform.position.x, HeightCalc, Singleton.camera.transform.position.z);
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
            d.Log("Initializing water bouyancy");
            Singleton.Manager<ManWorldTreadmill>.inst.OnBeforeWorldOriginMove.Subscribe(WorldShift);
            try
            {
                d.Log("Creating Camera Filter");
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

                d.Log("Creating water plane");
                var tempGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Mesh plane = Instantiate(tempGO.GetComponent<MeshFilter>().mesh);

                d.Log("Locating shader");
                var shader = Shader.Find("Standard");
                if (!shader)
                {
                    IEnumerable<Shader> shaders = Resources.FindObjectsOfTypeAll<Shader>();
                    shaders = shaders.Where(s => s.name == "Standard");
                    if (shaders.Count() > 1)
                    {
                        shader = shaders.ElementAt(1);
                    }
                    else if (shaders.Count() == 1)
                    {
                        shader = shaders.ElementAt(0);
                    }
                    else
                    {
                        d.LogError("FAILED to find a shader");
                    }
                }

                d.Log("Creating default water material");
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

                d.Log("Creating fancyWavelessWater material");
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

                d.Log("Creating fancyWater material");
                Material fancyWater = new Material(QPatch.assetBundle.LoadAsset<Shader>("CartoonWater"));
                fancyWater.SetFloat("_UseWorldCoordinates", 1f);
                fancyWater.SetFloat("_RippleDensity", 0.25f);
                fancyWater.SetFloat("_RippleCutoff", 3.5f);
                fancyWater.SetFloat("_WaveAmplitude", 5f);
                fancyWater.SetFloat("_Tessellation", 7.5f);

                d.Log("Creating fancy Mesh");
                Mesh fancyMesh = new Mesh();
                fancyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                if (WaterMod.TTMMInited)
                {
                    fancyMesh = OBJParser.MeshFromFile("Assets/plane.obj", fancyMesh);
                }
                else
                {
                    ModContainer container = Singleton.Manager<ManMods>.inst.FindMod("WaterMod");
                    foreach (UnityEngine.Object obj in container.Contents.FindAllAssets("plane.obj"))
                    {
                        if (obj != null)
                        {
                            if (obj is Mesh)
                                fancyMesh = (Mesh)obj;
                            else if (obj is GameObject)
                                fancyMesh = ((GameObject)obj).GetComponentInChildren<MeshFilter>().sharedMesh;
                        }
                    }
                }

                waterLooks.Add(new WaterLook()
                {
                    name = "Fancy",
                    material = fancyWater,
                    mesh = fancyMesh
                });

                d.Log("Creating WaterObject for physics");
                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                WaterBuoyancy.folder = folder;

                GameObject Surface = tempGO;
                Destroy(Surface.GetComponent<MeshCollider>());
                Transform component = Surface.transform;
                component.parent = folder.transform;
                Surface.GetComponent<Renderer>().material = defaultWater;

                WaterBuoyancy.surface = Surface;

                component.localScale = new Vector3(2048f, 0.075f, 2048f);

                d.Log("Adding physics trigger");
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

                d.Log("Adding water collider");
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
            private WaterBlock wBlock;

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
                wBlock = tankblock.GetComponent<WaterBlock>();
                if (wBlock == null)
                {
                    wBlock = tankblock.gameObject.AddComponent<WaterBlock>();
                }
                wBlock.watertank = this;
            }

            public void RemoveBlock(TankBlock tankblock, Tank tank)
            {
                wBlock.watertank = null;
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
                    if (TankBlock == null)
                    {
                        TankBlock = base.GetComponentInParent<TankBlock>();
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
                if (this.heartBeat == HeartBeat)
                {
                    return;
                }

                this.heartBeat = HeartBeat;
                try
                {
                    if (TankBlock == null)
                    {
                        TankBlock = base.GetComponentInParent<TankBlock>();
                    }

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
