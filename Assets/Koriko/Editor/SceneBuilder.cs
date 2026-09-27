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
            {"Dress",new Color(.095f,.10f,.17f)},{"Bow",new Color(.66f,.09f,.105f)},{"Shoe",new Color(.65f,.22f,.12f)},
            {"White",new Color(.97f,.91f,.74f)},{"Eye",new Color(.26f,.12f,.075f)},{"Sea",new Color(.14f,.39f,.45f)},
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
                else GameObjectUtility.SetStaticEditorFlags(mesh.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
            }
            if(colliders<40)throw new InvalidOperationException("Authored collision meshes are missing.");
            var sky=Material("PaintedSky",Shader.Find("Koriko/PaintedSky"));
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedSky.png"));RenderSettings.skybox=sky;
            var camera=new GameObject("Main camera").AddComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.1f;camera.farClipPlane=800;camera.clearFlags=CameraClearFlags.Skybox;camera.fieldOfView=52;
            camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var brain=camera.gameObject.AddComponent<CinemachineBrain>();brain.UpdateMethod=CinemachineBrain.UpdateMethods.LateUpdate;
            var orbit=new GameObject("Camera orbit target").transform;orbit.position=new Vector3(-113,1.6f,9);orbit.rotation=Quaternion.Euler(12,85,0);
            var follow=new GameObject("Kiki follow camera").AddComponent<CinemachineCamera>();follow.Follow=orbit;follow.Lens.FieldOfView=52;follow.Lens.NearClipPlane=.1f;follow.Lens.FarClipPlane=800;
            var body=follow.gameObject.AddComponent<CinemachineThirdPersonFollow>();body.CameraDistance=7.5f;body.ShoulderOffset=new Vector3(.55f,.3f,0);body.VerticalArmLength=.45f;body.CameraSide=.65f;body.Damping=new Vector3(.12f,.22f,.15f);
            body.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings{Enabled=true,CollisionFilter=1<<8,IgnoreTag="Player",CameraRadius=.27f,DampingIntoCollision=.08f,DampingFromCollision=.5f};
            var player=new GameObject("Kiki");player.tag="Player";player.layer=9;player.transform.position=new Vector3(-113,.18f,9);
            var controller=player.AddComponent<CharacterController>();controller.radius=.42f;controller.height=2.1f;controller.center=new Vector3(0,1.1f,0);controller.stepOffset=.25f;controller.skinWidth=.045f;
            var motor=player.AddComponent<FlightMotor>();motor.CameraOrbit=orbit;
            var visual=new GameObject("Flight pose").transform;visual.SetParent(player.transform,false);visual.localRotation=Quaternion.Euler(0,85,0);motor.Visual=visual;
            var riderAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"KikiAndJiji.fbx");if(!riderAsset)throw new InvalidOperationException("Missing Kiki/Jiji model.");
            var rider=(GameObject)PrefabUtility.InstantiatePrefab(riderAsset);
            AlignAnchors(rider.transform,new[]{"RiderAnchor_Origin","RiderAnchor_Right","RiderAnchor_Up","RiderAnchor_Forward"},new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward});
            rider.transform.SetParent(visual,false);
            ApplyMaterials(rider,materials);AddCharacterInk(rider);
            foreach(var t in rider.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
            rider.AddComponent<RiderPerformance>().Motor=motor;
            var sun=new GameObject("Painted afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.shadowBias=.08f;sun.shadowNormalBias=.3f;
            var lightCycle=new GameObject("Continuous daylight").AddComponent<Daylight>();lightCycle.Sun=sun;lightCycle.Camera=camera;
            var appObject=new GameObject("Delivery service");var input=appObject.AddComponent<FlightInput>();var app=appObject.AddComponent<GameApp>();app.Motor=motor;app.Input=input;app.Lighting=lightCycle;
            var canvasObject=new GameObject("Painted interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;
            var hud=canvasObject.AddComponent<GameHud>();hud.Font=AssetDatabase.LoadAssetAtPath<Font>(Art+"Fonts/AlegreyaSans-Regular.ttf");hud.Bold=AssetDatabase.LoadAssetAtPath<Font>(Art+"Fonts/AlegreyaSans-Bold.ttf");hud.Icons=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ItemIcons.png");app.Hud=hud;
            hud.Paper=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedSurfaces.png");
            var events=new GameObject("Keyboard and controller UI",typeof(EventSystem));events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var flock=new GameObject("Town crows").AddComponent<CrowFlock>();flock.App=app;flock.Material=materials["Ink"];
            Physics.SyncTransforms();
            foreach(var d in Catalog.Destinations)
            {
                var start=new Vector3((float)d.Landing.x,(float)d.Landing.y+2,(float)d.Landing.z);
                if(!Physics.Raycast(start,Vector3.down,out var hit,4,1<<8))throw new InvalidOperationException("No landing floor at "+d.Id);
                if(Mathf.Abs(hit.point.y-(float)d.Landing.y)>1)throw new InvalidOperationException("Landing floor height mismatch at "+d.Id+": "+hit.point.y);
            }
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log($"KORIKO_SCENE_READY: {colliders} collision meshes; six landing courts; third-person flight; continuous delivery simulation.");
        }

        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;}
        static void ConfigureInputAndLayers()
        {
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(8).stringValue="World";layers.GetArrayElementAtIndex(9).stringValue="Rider";tags.ApplyModifiedPropertiesWithoutUndo();
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();}
        }
        static Dictionary<string,Material> MakeMaterials()
        {
            var shader=Shader.Find("Koriko/Painted");if(!shader)throw new InvalidOperationException("The painted shader did not import.");
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"PaintedSurfaces.png");
            var result=new Dictionary<string,Material>();
            for(int i=0;i<16;i++)
            {
                var m=Material("Paint_"+i,shader);m.SetTexture("_BaseMap",atlas);m.SetColor("_Color",palette[i]);m.SetVector("_AtlasRect",new Vector4(i%4*.25f+.002f,(3-i/4)*.25f+.002f,.246f,.246f));m.SetFloat("_TextureWeight",i==9||i==10?.4f:.65f);m.SetFloat("_Wind",i==10?1:0);result.Add(m.name,m);
            }
            foreach(var entry in colors)
            {
                var m=Material(entry.Key,shader);m.SetColor("_Color",entry.Value);m.SetFloat("_TextureWeight",0);m.SetFloat("_Softness",entry.Key=="Skin"||entry.Key=="Dress"?.035f:.12f);m.SetFloat("_Emission",entry.Key=="Glass"?.65f:0);result.Add(entry.Key,m);
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
        static void AddCharacterInk(GameObject root)
        {
            var material=Material("CharacterOutline",Shader.Find("Koriko/Ink"));
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>().ToArray())
            {
                if(!new[]{"Dress","Face","Bob hair","Bow loop"}.Any(n=>filter.name.StartsWith(n,StringComparison.Ordinal)))continue;
                var outline=new GameObject("Ink edge");outline.transform.SetParent(filter.transform,false);outline.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var r=outline.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
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
