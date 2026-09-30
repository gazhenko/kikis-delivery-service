using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Koriko.Core;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Koriko.Editor
{
    public static class SceneBuilder
    {
        public const string ScenePath="Assets/Koriko/Scenes/BakeryToHarbor.unity";
        const string Art="Assets/Koriko/Art/",Generated="Assets/Koriko/Generated/";
        static readonly Color[] palette={
            new Color(.89f,.82f,.67f),new Color(.74f,.52f,.45f),new Color(.77f,.61f,.37f),new Color(.56f,.67f,.52f),
            new Color(.55f,.27f,.18f),new Color(.28f,.37f,.43f),new Color(.32f,.25f,.17f),new Color(.67f,.64f,.53f),
            new Color(.46f,.46f,.39f),new Color(.40f,.53f,.26f),new Color(.23f,.36f,.20f),new Color(.43f,.31f,.18f),
            new Color(.95f,.91f,.81f),new Color(.10f,.14f,.23f),new Color(.68f,.11f,.13f),new Color(.79f,.62f,.29f)
        };
        static readonly Dictionary<string,Color> colors=new Dictionary<string,Color>{
            {"Ink",new Color(.075f,.089f,.112f)},{"Skin",new Color(.95f,.73f,.54f)},{"Hair",new Color(.105f,.073f,.064f)},
            {"Dress",new Color(.095f,.10f,.17f)},{"Bow",new Color(.66f,.09f,.105f)},{"BowShade",new Color(.57f,.12f,.18f)},{"Shoe",new Color(.65f,.22f,.12f)},
            {"White",new Color(.97f,.91f,.74f)},{"Eye",new Color(.26f,.12f,.075f)},{"Sea",new Color(.24f,.53f,.57f)},
            {"Foam",new Color(.7f,.84f,.79f)},{"Leaf",new Color(.26f,.41f,.20f)},{"LeafLight",new Color(.44f,.56f,.28f)},
            {"Flower",new Color(.83f,.42f,.42f)},{"Lavender",new Color(.53f,.48f,.63f)},{"Gold",new Color(.88f,.65f,.24f)},
            {"Glass",new Color(.19f,.32f,.35f)},{"Distant",new Color(.34f,.48f,.49f)},{"Cloud",new Color(.92f,.94f,.86f)}
        };
        [InitializeOnLoadMethod]
        static void FirstImport()
        {
            EditorApplication.update+=Bootstrap;
        }
        static void Bootstrap()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=Bootstrap;
            if(!File.Exists(ScenePath)&&AssetDatabase.LoadAssetAtPath<GameObject>(Art+"KorikoNeighborhood.fbx"))Build();
        }
        public static void Ensure(){if(!File.Exists(ScenePath))Build();else EditorSceneManager.OpenScene(ScenePath);}

        [MenuItem("Koriko/Rebuild prototype scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Generated);Directory.CreateDirectory("Assets/Koriko/Scenes");
            AssetDatabase.Refresh();
            ConfigureInputAndLayers();
            var renderer=LoadOrCreate<UniversalRendererData>(Generated+"PaintedRenderer.asset");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Generated+"PaintedPipeline.asset");
            if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Generated+"PaintedPipeline.asset");}
            pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.supportsHDR=false;pipeline.shadowDistance=85;pipeline.shadowCascadeCount=2;
            var pipelineSettings=new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            pipelineSettings.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
            pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.vSyncCount=1;
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.companyName="Koriko";PlayerSettings.productName="Kiki’s Delivery Service";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=false;PlayerSettings.resizableWindow=true;
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"GameIcon.png");if(icon)PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,new[]{icon});
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var materials=MakeMaterials();
            var worldAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"KorikoNeighborhood.fbx");
            if(!worldAsset)throw new InvalidOperationException("Missing authored neighborhood FBX. Run Tools/build_art.py in Blender.");
            var world=(GameObject)PrefabUtility.InstantiatePrefab(worldAsset);world.name="Koriko neighborhood";
            NormalizeWorld(world.transform);
            ApplyMaterials(world,materials);
            int colliders=0;
            foreach(var mesh in world.GetComponentsInChildren<MeshFilter>())
            {
                mesh.gameObject.layer=8;
                if(mesh.name.StartsWith("COL_",StringComparison.Ordinal))
                {
                    var visible=mesh.GetComponent<MeshRenderer>();if(visible)visible.enabled=false;
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;colliders++;
                }
                else
                {
                    // The painted paving and cargo deck sit above the broad
                    // terrain/blockout colliders. Use their real visible mesh
                    // as the walking surface so feet cannot disappear into it.
                    if(new[]{"Streets__Paint_8","Streets__Paint_7","Landscape__Paint_7","Airship__Paint_6"}.Contains(mesh.name))
                    {mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;colliders++;}
                    // Flight uses simple building volumes. The camera also
                    // needs the visible awnings, overhanging roofs and cornices
                    // so its orbit cannot sit inside decorative architecture.
                    if(mesh.name.StartsWith("Street_",StringComparison.Ordinal))
                    {
                        var cameraShell=new GameObject("Camera facade surface");
                        cameraShell.transform.SetParent(mesh.transform,false);cameraShell.layer=10;
                        cameraShell.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
                    }
                    // Moored boats ride the swell at runtime, so they cannot be statically batched.
                    bool afloat=mesh.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("Boat_",StringComparison.Ordinal));
                    if(!afloat)GameObjectUtility.SetStaticEditorFlags(mesh.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
                    // Broad painted floors receive building/tree shadows. Casting them back
                    // onto their own near-coplanar triangles exposes a distracting seam grid.
                    if(mesh.name=="Landscape__Paint_9"||mesh.name=="Streets__Paint_8"||mesh.name=="Streets__Paint_7")
                        mesh.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
            }
            if(colliders<40)throw new InvalidOperationException("Authored collision meshes are missing.");
            var sky=Material("PaintedSky",Shader.Find("Koriko/PaintedSky"));
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedSky.png"));RenderSettings.skybox=sky;
            var camera=new GameObject("Main camera").AddComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.1f;camera.farClipPlane=800;camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=52;
            camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var brain=camera.gameObject.AddComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.LateUpdate;
            var orbit=new GameObject("Camera orbit target").transform;orbit.position=new Vector3(-113,1.6f,9);orbit.rotation=Quaternion.Euler(12,85,0);
            var follow=new GameObject("Kiki follow camera").AddComponent<CinemachineCamera>();follow.Follow=orbit;follow.Lens.FieldOfView=52;follow.Lens.NearClipPlane=.1f;follow.Lens.FarClipPlane=800;follow.Priority=10;
            var title=new GameObject("Title flyover camera").AddComponent<CinemachineCamera>();title.Lens.FieldOfView=46;title.Lens.NearClipPlane=.3f;title.Lens.FarClipPlane=900;title.Priority=20;
            var body=follow.gameObject.AddComponent<CinemachineThirdPersonFollow>();body.CameraDistance=7.5f;body.ShoulderOffset=new Vector3(.55f,.3f,0);body.VerticalArmLength=.45f;body.CameraSide=.65f;body.Damping=new Vector3(.12f,.22f,.15f);
            body.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings{Enabled=true,CollisionFilter=(1<<8)|(1<<10),IgnoreTag="Player",CameraRadius=.27f,DampingIntoCollision=.08f,DampingFromCollision=.5f};
            var player=new GameObject("Kiki");player.tag="Player";player.layer=9;player.transform.position=new Vector3(-113,.18f,9);
            var controller=player.AddComponent<CharacterController>();controller.radius=.42f;controller.height=2.1f;controller.center=new Vector3(0,1.1f,0);controller.stepOffset=.25f;controller.skinWidth=.045f;
            var motor=player.AddComponent<FlightMotor>();motor.CameraOrbit=orbit;
            var visual=new GameObject("Flight pose").transform;visual.SetParent(player.transform,false);visual.localRotation=Quaternion.Euler(0,85,0);motor.Visual=visual;
            var riderAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"KikiAndJiji.fbx");if(!riderAsset)throw new InvalidOperationException("Missing Kiki/Jiji model.");
            var rider=(GameObject)PrefabUtility.InstantiatePrefab(riderAsset);
            AlignAnchors(rider.transform,new[]{"RiderAnchor_Origin","RiderAnchor_Right","RiderAnchor_Up","RiderAnchor_Forward"},new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward});
            rider.transform.SetParent(visual,false);
            var riderNodes=rider.GetComponentsInChildren<Transform>();
            foreach(string part in new[]{"LeftForearm","RightForearm","LeftHand","RightHand","LeftGrip","RightGrip","CarryGrip","BroomGroundTip","LeftKnee","RightKnee","LeftEyePivot","RightEyePivot","Left sole","Right sole","LeftBowLoop","RightBowLoop","JijiHead","JijiTail","LeftJijiEar","RightJijiEar","LeftBrow","RightBrow"})
                if(!riderNodes.Any(t=>t.name==part))throw new InvalidOperationException("Character articulation is missing: "+part);
            if(!rider.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>r.sharedMesh.GetBlendShapeName(i).EndsWith("Flight cloth"))))
                throw new InvalidOperationException("Character flight cloth shape did not survive FBX import.");
            foreach(string shape in new[]{"Hair stream","Hair left","Hair right","Hem left","Hem right","Walk left","Walk right","Left hand relaxed","Gaze left","Gaze right"})
                if(!rider.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>r.sharedMesh.GetBlendShapeName(i).EndsWith(shape))))
                    throw new InvalidOperationException("Character secondary animation shape did not import: "+shape);
            var characterMaterials=MakeCharacterMaterials();
            ApplyMaterials(rider,characterMaterials);AddCharacterInk(rider);
            foreach(var t in rider.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
            rider.AddComponent<RiderPerformance>().Motor=motor;
            var props=rider.AddComponent<RiderProps>();
            var sun=new GameObject("Painted afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.shadowBias=.08f;sun.shadowNormalBias=.3f;
            var lightCycle=new GameObject("Continuous daylight").AddComponent<Daylight>();lightCycle.Sun=sun;lightCycle.Camera=camera;
            var appObject=new GameObject("Delivery service");var input=appObject.AddComponent<FlightInput>();var app=appObject.AddComponent<GameApp>();app.Motor=motor;app.Input=input;app.Lighting=lightCycle;
            var canvasObject=new GameObject("Painted interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;
            var hud=canvasObject.AddComponent<GameHud>();hud.Font=AssetDatabase.LoadAssetAtPath<Font>(Art+"Fonts/AlegreyaSans-Regular.ttf");hud.Bold=AssetDatabase.LoadAssetAtPath<Font>(Art+"Fonts/AlegreyaSans-Bold.ttf");hud.Icons=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ItemIcons.png");app.Hud=hud;
            hud.Paper=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedFilmSurfaces.png");
            var events=new GameObject("Keyboard and controller UI",typeof(EventSystem));events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            hud.Portraits=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Portraits.png");
            // Birds share the cel paint but stay outside the rider's pose and smear frame.
            var cel=Shader.Find("Koriko/CharacterCel");
            Material Detached(string name,Color lit,Color shade)
            {
                var m=Material(name,cel);m.SetColor("_Color",lit);m.SetColor("_ShadowTint",shade);m.SetColor("_LightTint",lit);
                m.SetFloat("_Softness",.005f);m.SetFloat("_Face",0);m.SetFloat("_Rim",0);m.SetFloat("_Cloth",0);m.SetFloat("_Detached",1);return m;
            }
            var flock=new GameObject("Town crows").AddComponent<CrowFlock>();flock.App=app;flock.Material=Detached("Cel_Crow",new Color(.075f,.09f,.12f),new Color(.05f,.055f,.08f));
            var glow=Shader.Find("Koriko/Glow");if(!glow)throw new InvalidOperationException("Lamplight shader did not import.");
            var lampLight=Material("LampLight",glow);lampLight.SetColor("_Color",new Color(1,.72f,.40f));lampLight.SetFloat("_Intensity",1.15f);
            var lanternLight=Material("LanternLight",glow);lanternLight.SetColor("_Color",new Color(1,.80f,.48f));lanternLight.SetFloat("_Intensity",1.1f);
            var puff=Material("PaintedPuff",Shader.Find("Koriko/Puff"));puff.SetColor("_ShadowTint",new Color(.74f,.76f,.84f));
            var ring=Material("CourtRing",Shader.Find("Koriko/CourtRing"));ring.SetColor("_Color",new Color(.96f,.72f,.30f));
            var life=appObject.AddComponent<TownLife>();life.App=app;life.Puff=puff;life.Lamps=lampLight;life.Ring=ring;
            life.GullBody=Detached("Cel_GullBody",new Color(.97f,.96f,.92f),new Color(.70f,.74f,.84f));
            life.GullWing=Detached("Cel_GullWing",new Color(.74f,.78f,.82f),new Color(.52f,.56f,.66f));
            life.GullTip=Detached("Cel_GullTip",new Color(.13f,.13f,.16f),new Color(.08f,.08f,.10f));
            life.GullBeak=Detached("Cel_GullBeak",new Color(.95f,.72f,.25f),new Color(.74f,.50f,.18f));
            var sound=appObject.AddComponent<Soundscape>();sound.App=app;sound.Life=life;app.Sound=sound;app.Life=life;
            props.App=app;props.Life=life;props.Paper=characterMaterials["Parcel"];props.String=characterMaterials["ParcelString"];
            props.FragilePaper=characterMaterials["FragilePaper"];props.FragileRibbon=characterMaterials["Bow"];props.Crate=characterMaterials["Crate"];props.Strap=characterMaterials["CrateStrap"];
            props.Canvas=characterMaterials["Canvas"];props.Rope=characterMaterials["Rope"];props.Brass=characterMaterials["Gold"];props.LanternGlass=characterMaterials["LanternGlass"];
            props.Outline=Material("CharacterOutline_Satchel",Shader.Find("Koriko/Ink"));props.LanternHalo=lanternLight;
            var director=appObject.AddComponent<CameraDirector>();director.App=app;director.Follow=follow;director.Title=title;director.Body=body;director.Brain=brain;
            // Save the fog into the scene. With automatic fog stripping, a scene without fog
            // strips every fog variant from the player and the runtime fog never renders.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=95;RenderSettings.fogEndDistance=640;RenderSettings.fogColor=new Color(.63f,.76f,.79f);
            Physics.SyncTransforms();
            foreach(var d in Catalog.Destinations)
            {
                var start=new Vector3((float)d.Landing.x,(float)d.Landing.y+2,(float)d.Landing.z);
                if(!Physics.Raycast(start,Vector3.down,out var hit,4,1<<8))throw new InvalidOperationException("No landing floor at "+d.Id);
                if(Mathf.Abs(hit.point.y-(float)d.Landing.y)>.075f)throw new InvalidOperationException("Landing floor height mismatch at "+d.Id+": "+hit.point.y+"; expected visible court near "+d.Landing.y);
            }
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log($"KORIKO_SCENE_READY: {colliders} collision meshes; six landing courts; third-person flight; continuous delivery simulation.");
        }

        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;}
        static void ConfigureInputAndLayers()
        {
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(8).stringValue="World";layers.GetArrayElementAtIndex(9).stringValue="Rider";layers.GetArrayElementAtIndex(10).stringValue="Camera surface";tags.ApplyModifiedPropertiesWithoutUndo();
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();}
        }
        static Dictionary<string,Material> MakeMaterials()
        {
            var shader=Shader.Find("Koriko/Painted");if(!shader)throw new InvalidOperationException("The painted shader did not import.");
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"EnvironmentSurfaces-v2.png");
            var originalAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedFilmSurfaces.png");
            if(!atlas||!originalAtlas)throw new InvalidOperationException("Missing painted environment surface atlas.");
            // Inspected pixel boundaries: generated rows are not exactly equal height.
            int[] surfaceRows={0,314,628,941,1254};
            var result=new Dictionary<string,Material>();
            for(int i=0;i<16;i++)
            {
                var m=Material("Paint_"+i,shader);m.SetTexture("_BaseMap",i<12?atlas:originalAtlas);m.SetColor("_Color",palette[i]);
                int top=surfaceRows[i/4],bottom=surfaceRows[i/4+1];
                m.SetVector("_AtlasRect",i<12?new Vector4(i%4*.25f+5/1254f,1-bottom/1254f+5/1254f,.25f-10/1254f,(bottom-top-10)/1254f):new Vector4(i%4*.25f+.008f,(3-i/4)*.25f+.008f,.234f,.234f));
                m.SetFloat("_TextureWeight",i==9?.35f:i==10?.48f:i<3?.70f:i==8?.55f:.70f);
                m.SetFloat("_Wind",i==10?.6f:0);m.SetFloat("_PaintScale",1);m.SetFloat("_VertexPaint",1);
                m.SetFloat("_NormalFlatten",i==10?.65f:0);m.SetFloat("_ShadowStrength",i==10?.35f:.68f);
                m.SetFloat("_Softness",i==10?.20f:.12f);
                // Roof planes are single sheets; draw their undersides so street-level eaves are not open to the sky.
                m.SetFloat("_Cull",i==4||i==5?(float)CullMode.Off:(float)CullMode.Back);
                m.SetColor("_ShadowTint",new Color(.63f,.73f,.80f));m.SetColor("_LightTint",new Color(1.08f,1.04f,.94f));
                result.Add(m.name,m);
            }
            foreach(var entry in colors)
            {
                var m=Material(entry.Key,entry.Key=="Sea"?Shader.Find("Koriko/PaintedSea"):shader);m.SetColor("_Color",entry.Value);
                if(entry.Key!="Sea")
                {
                    m.SetFloat("_TextureWeight",0);m.SetFloat("_Softness",.12f);m.SetFloat("_PaintScale",1);m.SetFloat("_VertexPaint",1);
                    m.SetFloat("_ShadowStrength",.6f);m.SetFloat("_NormalFlatten",entry.Key.StartsWith("Leaf")?.45f:0);
                    m.SetColor("_ShadowTint",new Color(.64f,.71f,.79f));m.SetColor("_LightTint",new Color(1.05f,1.01f,.92f));
                    m.SetFloat("_Emission",entry.Key=="Glass"?.85f:0);
                }
                result.Add(entry.Key,m);
            }
            for(int cell=12;cell<=13;cell++)
            {
                string name=cell==12?"Environment_Brick":"Environment_Copper";
                var m=Material(name,shader);m.SetTexture("_BaseMap",atlas);
                m.SetColor("_Color",cell==12?new Color(.63f,.37f,.28f):new Color(.28f,.45f,.40f));
                m.SetVector("_AtlasRect",new Vector4(cell%4*.25f+5/1254f,5/1254f,.25f-10/1254f,(1254-941-10)/1254f));
                m.SetFloat("_TextureWeight",.72f);m.SetFloat("_PaintScale",1);m.SetFloat("_VertexPaint",1);
                m.SetFloat("_Softness",.12f);m.SetFloat("_ShadowStrength",.68f);
                m.SetColor("_ShadowTint",new Color(.63f,.73f,.80f));m.SetColor("_LightTint",new Color(1.08f,1.04f,.94f));
                result.Add(name,m);
            }
            var shopAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ShopPaintings.png");
            if(!shopAtlas)throw new InvalidOperationException("Missing shop paintings atlas.");
            int[] boundaries={0,332,674,940,1254};
            for(int i=0;i<8;i++)
            {
                int top=boundaries[i/2],bottom=boundaries[i/2+1];
                var m=Material("Shop_"+i,shader);m.SetTexture("_BaseMap",shopAtlas);
                m.SetVector("_AtlasRect",new Vector4(i%2*.5f+4/1254f,1-bottom/1254f+4/1254f,.5f-8/1254f,(bottom-top-8)/1254f));
                m.SetFloat("_TextureWeight",1);m.SetFloat("_PaintScale",1);m.SetFloat("_VertexPaint",1);
                m.SetFloat("_Softness",.16f);m.SetFloat("_ShadowStrength",.3f);m.SetFloat("_Emission",i<4?.08f:0);
                m.SetColor("_ShadowTint",new Color(.82f,.85f,.88f));m.SetColor("_LightTint",new Color(1.04f,1.02f,.98f));
                result.Add(m.name,m);
            }
            return result;
        }
        static Dictionary<string,Material> MakeCharacterMaterials()
        {
            var shader=Shader.Find("Koriko/CharacterCel");if(!shader)throw new InvalidOperationException("Character cel shader did not import.");
            var result=new Dictionary<string,Material>();
            // Film palette, studied from the official trailer: auburn hair, a dark purple smock,
            // pale cream skin with pink blush, an orange-tan satchel, red bow and flats, black Jiji.
            var characterPaint=new Dictionary<string,Color>(colors){
                ["Skin"]=new Color(.99f,.89f,.81f),["SkinShade"]=new Color(.91f,.74f,.69f),["Hair"]=new Color(.32f,.17f,.13f),
                ["Dress"]=new Color(.23f,.19f,.31f),["Bow"]=new Color(.86f,.08f,.12f),
                ["BowShade"]=new Color(.58f,.04f,.09f),["Ink"]=new Color(.06f,.045f,.055f),
                ["Eye"]=new Color(.06f,.045f,.055f),["Iris"]=new Color(.25f,.13f,.10f),["White"]=new Color(.99f,.98f,.94f),
                ["Shoe"]=new Color(.66f,.20f,.14f),["Blush"]=new Color(.98f,.68f,.70f),
                ["HairShade"]=new Color(.14f,.07f,.06f),["DressShade"]=new Color(.12f,.10f,.18f),
                ["Lip"]=new Color(.55f,.28f,.26f),["Sole"]=new Color(.19f,.15f,.15f),
                ["Satchel"]=new Color(.94f,.62f,.36f),
                // Carried freight and the broom lantern.
                ["Parcel"]=new Color(.80f,.63f,.42f),["ParcelString"]=new Color(.62f,.13f,.13f),["FragilePaper"]=new Color(.95f,.92f,.84f),
                ["Crate"]=new Color(.62f,.45f,.27f),["CrateStrap"]=new Color(.26f,.20f,.15f),["Canvas"]=new Color(.85f,.79f,.63f),
                ["Rope"]=new Color(.63f,.53f,.37f),["LanternGlass"]=new Color(1,.90f,.62f),
                // Jiji is black with a cool sheen, distinct from Kiki's warm auburn hair.
                ["Jiji"]=new Color(.10f,.105f,.15f),["JijiInner"]=new Color(.44f,.33f,.46f)
            };
            // One hard shadow tone per paint, chosen like a cel painter's shadow colour.
            var shadows=new Dictionary<string,Color>{
                ["Skin"]=new Color(.91f,.74f,.69f),["Hair"]=new Color(.15f,.08f,.07f),["Dress"]=new Color(.13f,.11f,.20f),
                ["Bow"]=new Color(.60f,.04f,.08f),["Satchel"]=new Color(.80f,.45f,.27f),["Shoe"]=new Color(.42f,.10f,.08f),
                ["Jiji"]=new Color(.045f,.048f,.07f),["Sole"]=new Color(.12f,.09f,.09f)
            };
            // Painted marks and shadow shapes stay a single flat colour in every light.
            var flat=new HashSet<string>{"SkinShade","Eye","Iris","White","Blush","Lip","HairShade","DressShade","BowShade","Ink","JijiInner"};
            foreach(var entry in characterPaint.Concat(Enumerable.Range(0,16).Select(i=>new KeyValuePair<string,Color>("Paint_"+i,palette[i]))))
            {
                Color lit=entry.Value;
                Color shadow=shadows.TryGetValue(entry.Key,out var chosen)?chosen:flat.Contains(entry.Key)?lit:Color.Lerp(lit,new Color(.23f,.23f,.38f),.35f)*.76f;
                var m=Material("Cel_"+entry.Key,shader);m.SetColor("_Color",lit);m.SetColor("_ShadowTint",shadow);
                m.SetColor("_LightTint",Color.Lerp(lit,new Color(.88f,.84f,.77f),.20f));
                m.SetFloat("_Softness",.005f);m.SetFloat("_Face",entry.Key=="Skin"?.85f:0);
                // Jiji keeps only a thin cool sheen on the lit edge of his form, as drawn in the film.
                m.SetFloat("_Rim",entry.Key=="Jiji"?.35f:0);
                if(entry.Key=="Jiji")m.SetColor("_LightTint",new Color(.24f,.26f,.37f));
                m.SetFloat("_Detached",0);
                m.SetFloat("_Cloth",entry.Key=="Dress"||entry.Key=="DressShade"?1:0);
                result.Add(entry.Key,m);
            }
            return result;
        }
        static Material Material(string name,Shader shader)
        {
            string path=Generated+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
            m.enableInstancing=true;return m;
        }
        static void ApplyMaterials(GameObject root,Dictionary<string,Material> materials)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var originals=renderer.sharedMaterials;var replacements=new Material[originals.Length];
                for(int i=0;i<originals.Length;i++)
                {
                    string name=originals[i]?originals[i].name.Split('.')[0]:"";
                    if(!materials.TryGetValue(name,out replacements[i]))throw new InvalidOperationException("Unmapped art material: "+name+" on "+renderer.name);
                }
                renderer.sharedMaterials=replacements;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
        }
        // Coloured trace lines, as in the film's cels: reddish-brown around skin, deep plum
        // around the smock, near-black around hair and Jiji, warm brown around the broom.
        static readonly Dictionary<string,Color> inkColors=new Dictionary<string,Color>{
            ["Skin"]=new Color(.42f,.21f,.17f),["SkinShade"]=new Color(.42f,.21f,.17f),["Hair"]=new Color(.10f,.045f,.04f),
            ["Dress"]=new Color(.075f,.05f,.10f),["Bow"]=new Color(.34f,.02f,.05f),["Satchel"]=new Color(.44f,.21f,.10f),
            ["Shoe"]=new Color(.26f,.06f,.04f),["Jiji"]=new Color(.02f,.02f,.035f),["Paint_15"]=new Color(.42f,.28f,.09f),["Paint_6"]=new Color(.20f,.12f,.07f)
        };
        static void AddCharacterInk(GameObject root)
        {
            var shader=Shader.Find("Koriko/Ink");
            var inks=new Dictionary<string,Material>();
            Material Ink(string paint)
            {
                string key=inkColors.ContainsKey(paint)?paint:"Default";bool cloth=paint=="Dress";
                string name=key=="Default"?"CharacterOutline":"CharacterOutline_"+key;
                if(inks.TryGetValue(name,out var existing))return existing;
                var m=Material(name,shader);m.SetFloat("_Thickness",2.7f);m.SetFloat("_Cloth",cloth?1:0);
                m.SetColor("_Color",inkColors.TryGetValue(key,out var c)?c:new Color(.10f,.07f,.10f));
                inks.Add(name,m);return m;
            }
            // Every shape carries a line, as a drawn cel does. Painted marks are lines themselves,
            // and joint spheres inside a limb would draw rings, so they are left out.
            string[] skip={"Eye white","Brown iris","Pupil","glint","blush","eyelid","lash","lid mark","eyebrow","smile","mouth","Nose mark","Ear inner line","Hair part line","Fringe shadow","gathered fold","Dress fold","Satchel button","Broom straw","drawn straw","Broom binding","Jiji eye","Jiji pupil","Jiji nose","Jiji mouth","Jiji inner ear","Jiji muzzle","sole","kneecap","elbow","upper arm","thigh","RiderAnchor"};
            bool Draw(Renderer r)=>r.sharedMaterials.Length>0&&!skip.Any(k=>r.name.IndexOf(k,StringComparison.OrdinalIgnoreCase)>=0);
            string Paint(Renderer r)=>r.sharedMaterials[0]?r.sharedMaterials[0].name.Replace("Cel_",""):"";
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>().ToArray())
            {
                var source=filter.GetComponent<MeshRenderer>();if(!source||!Draw(source))continue;
                var outline=new GameObject("Ink edge");outline.transform.SetParent(filter.transform,false);outline.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var r=outline.AddComponent<MeshRenderer>();r.sharedMaterial=Ink(Paint(source));r.shadowCastingMode=ShadowCastingMode.Off;
            }
            foreach(var source in root.GetComponentsInChildren<SkinnedMeshRenderer>().ToArray())
            {
                if(!Draw(source))continue;
                var outline=new GameObject("Ink edge");outline.transform.SetParent(source.transform,false);
                var r=outline.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=source.sharedMesh;r.bones=source.bones;r.rootBone=source.rootBone;r.localBounds=source.localBounds;
                r.sharedMaterial=Ink(Paint(source));r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        static void NormalizeWorld(Transform root)
        {
            string[] ids={"bakery","clock","harbor","airship"};var target=new Vector3[4];
            for(int i=0;i<4;i++){var p=Catalog.FindDestination(ids[i]).Landing;target[i]=new Vector3((float)p.x,(float)p.y,(float)p.z);}
            AlignAnchors(root,ids.Select(id=>"Anchor_"+id).ToArray(),target);
        }
        static void AlignAnchors(Transform root,string[] names,Vector3[] target)
        {
            var source=new Vector3[4];
            var nodes=root.GetComponentsInChildren<Transform>();
            for(int i=0;i<4;i++)
            {
                var node=nodes.FirstOrDefault(t=>t.name==names[i]);if(!node)throw new InvalidOperationException("Missing import anchor "+names[i]);source[i]=node.position;
            }
            Matrix4x4 Basis(Vector3[] p)
            {
                var m=Matrix4x4.identity;m.SetColumn(0,p[1]-p[0]);m.SetColumn(1,p[2]-p[0]);m.SetColumn(2,p[3]-p[0]);m.SetColumn(3,new Vector4(p[0].x,p[0].y,p[0].z,1));return m;
            }
            var corrected=Basis(target)*Basis(source).inverse*root.localToWorldMatrix;
            Vector3 x=corrected.GetColumn(0),y=corrected.GetColumn(1),z=corrected.GetColumn(2);
            root.position=corrected.GetColumn(3);root.rotation=Quaternion.LookRotation(z.normalized,y.normalized);root.localScale=new Vector3(x.magnitude*(corrected.determinant<0?-1:1),y.magnitude,z.magnitude);
            for(int i=0;i<4;i++)
            {
                if(Vector3.Distance(nodes.First(t=>t.name==names[i]).position,target[i])>.025f)throw new InvalidOperationException("FBX anchor alignment failed at "+names[i]);
            }
        }
    }
}
