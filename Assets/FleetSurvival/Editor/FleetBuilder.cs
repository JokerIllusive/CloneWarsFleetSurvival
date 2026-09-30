using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace FleetSurvival.Editor
{
    public static class FleetBuilder
    {
        public static void PreviewSuppliedModel()
        {
            EnsureResources();
            PrepareSuppliedModel();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            const string assetPath="Assets/FleetSurvival/Models/star_wars_the_clone_wars_munificent_s7_style.glb";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if(asset==null) throw new Exception("Munificent GLB did not import as a Unity model.");
            var instance=UnityEngine.Object.Instantiate(asset); instance.name="Munificent imported model";
            var renderers=instance.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) throw new Exception("Imported model has no renderers.");
            Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Debug.Log("IMPORTED_MODEL_BOUNDS "+bounds+" renderers="+renderers.Length);
            RenderSettings.ambientLight=new Color(.5f,.52f,.58f); RenderSettings.skybox=null;
            var key=new GameObject("Preview key",typeof(Light)).GetComponent<Light>();
            key.type=LightType.Directional; key.intensity=1.6f; key.color=new Color(1,.93f,.84f); key.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("Preview fill",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Directional; fill.intensity=1; fill.color=new Color(.55f,.7f,1); fill.transform.rotation=Quaternion.Euler(-25,135,0);
            var camera=new GameObject("Model preview camera",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.022f,.03f,.047f);
            float radius=bounds.extents.magnitude;
            camera.orthographic=true; camera.orthographicSize=radius*.82f; camera.aspect=1.6f;
            camera.transform.position=bounds.center+new Vector3(1,.75f,-1).normalized*radius*3;
            camera.transform.LookAt(bounds.center); camera.nearClipPlane=.01f; camera.farClipPlane=radius*10;
            var target=new RenderTexture(1920,1200,24); camera.targetTexture=target; camera.Render();
            RenderTexture.active=target;
            var image=new Texture2D(1920,1200,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1200),0,0); image.Apply();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Munificent-model-preview.png"));
            File.WriteAllBytes(output,image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("SUPPLIED_MODEL_PREVIEW_SAVED "+output);
        }

        public static void PrepareSuppliedModel()
        {
            PrepareModel("star_wars_the_clone_wars_munificent_s7_style.glb","Munificent",10);
            PrepareModel("republic_venator_star_destroyer.glb","Venator",17);
            PrepareModel("acclamator_class_star_destroyer.glb","Acclamator",10);
            PrepareModel("star_wars_the_clone_wars_providence.glb","Providence",17);
            PrepareModel("star_wars_the_clone_wars_recusant_s3e2_style.glb","Recusant",13);
            PrepareModel("v-19_torrent_-_star_wars_-_clone_wars.glb","V19Torrent",3.6f);
            PrepareModel("vulture_droid.glb","Vulture",3.5f);
            PrepareModel("arc-170_starfighter.glb","ARC170",4.2f);
            PrepareModel("arquitens-class_light_cruiser.glb","Arquitens",8.5f);
            PrepareModel("venator_class_star_destroyer.glb","VenatorDetailed",17);
            PrepareModel("star_wars_battlefront_2_cis_lucrehulk.glb","Lucrehulk",18);
        }
        public static void PreviewCompleteFleet()
        {
            EnsureResources(); PrepareSuppliedModel();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.22f,.28f,.36f); RenderSettings.skybox=null;
            var key=new GameObject("Key",typeof(Light)).GetComponent<Light>(); key.type=LightType.Directional; key.intensity=1;
            key.color=new Color(1,.94f,.9f); key.transform.rotation=Quaternion.Euler(50,-30,0);
            var fill=new GameObject("Fill",typeof(Light)).GetComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.65f;
            fill.color=new Color(.65f,.8f,1); fill.transform.rotation=Quaternion.Euler(-25,145,0);
            var cisFill=new GameObject("Dark material fill",typeof(Light)).GetComponent<Light>(); cisFill.type=LightType.Directional;
            cisFill.intensity=2.1f; cisFill.cullingMask=1<<8; cisFill.transform.rotation=Quaternion.Euler(65,-40,0);
            var camera=new GameObject("Complete fleet camera",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.035f,.055f); camera.orthographic=true;
            camera.orthographicSize=55; camera.aspect=1920f/2200; camera.transform.position=new Vector3(0,180,-120); camera.transform.LookAt(Vector3.zero); camera.farClipPlane=400;
            var font=Resources.Load<TMP_FontAsset>("CommanderFont");
            Label("CLONE WARS / YOUR SHIP ROSTER",new Vector3(0,0,61),1.25f,new Color(.86f,.94f,1),camera,font);
            string[] names={"Venator","VenatorDetailed","Acclamator","Arquitens","ARC170","V19Torrent","Providence","Munificent","Recusant","Lucrehulk","Vulture"};
            string[] titles={"VENATOR / FIRST MODEL","VENATOR / DETAILED MODEL","ACCLAMATOR","ARQUITENS","ARC-170","V-19 TORRENT","PROVIDENCE","MUNIFICENT","RECUSANT","LUCREHULK","VULTURE DROID"};
            for(int i=0;i<names.Length;i++)
            {
                float x=-27+(i%3)*27, z=40-(i/3)*29;
                var ship=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Ships/"+names[i])); ship.transform.position=new Vector3(x,0,z);
                if(i==3 || i==7) ship.transform.localScale=Vector3.one*1.3f;
                if(i==4 || i==5 || i==10) ship.transform.localScale=Vector3.one*3;
                ship.transform.rotation=Quaternion.Euler(0,-28,0);
                if(i>=6 && i!=10) foreach(var part in ship.GetComponentsInChildren<Transform>()) part.gameObject.layer=8;
                Label(titles[i],new Vector3(x,0,z-12),.65f,new Color(.88f,.94f,1),camera,font);
            }
            var target=new RenderTexture(1920,2200,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1920,2200,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,2200),0,0); image.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Complete-fleet-preview.png")),image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("COMPLETE_FLEET_PREVIEW_READY");
        }
        public static void PreviewForwardDirections()
        {
            EnsureResources();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.5f,.55f,.6f); RenderSettings.skybox=null;
            var key=new GameObject("Direction preview key",typeof(Light)).GetComponent<Light>(); key.type=LightType.Directional; key.intensity=1.5f; key.transform.rotation=Quaternion.Euler(65,-30,0);
            var camera=new GameObject("Top-down forward preview",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.015f,.02f,.03f); camera.orthographic=true;
            camera.orthographicSize=55; camera.aspect=.8f; camera.transform.position=new Vector3(0,180,0); camera.transform.LookAt(Vector3.zero,Vector3.forward); camera.farClipPlane=400;
            string[] names={"Venator","VenatorDetailed","Acclamator","Arquitens","ARC170","V19Torrent","Providence","Munificent","Recusant","Lucrehulk","Vulture"};
            var font=Resources.Load<TMP_FontAsset>("CommanderFont");
            for(int i=0;i<names.Length;i++)
            {
                Vector3 position=new Vector3(-28+(i%3)*28,0,38-(i/3)*25);
                var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Ships/"+names[i])); model.transform.position=position;
                if(i==3 || i==7) model.transform.localScale=Vector3.one*1.4f;
                if(i==4 || i==5 || i==10) model.transform.localScale=Vector3.one*2.6f;
                var arrow=new GameObject("Forward +Z",typeof(LineRenderer)).GetComponent<LineRenderer>();
                arrow.sharedMaterial=ShipVisuals.Material(new Color(.1f,1,.4f),true); arrow.startWidth=arrow.endWidth=.13f; arrow.positionCount=5;
                arrow.SetPositions(new[]{position+new Vector3(10,3,-6),position+new Vector3(10,3,6),position+new Vector3(9,3,4),position+new Vector3(10,3,6),position+new Vector3(11,3,4)});
                Label(names[i]+" / green arrow = forward",position+new Vector3(0,0,-11),.48f,Color.white,camera,font);
            }
            var target=new RenderTexture(1920,2400,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1920,2400,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,2400),0,0); image.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Ship-forward-preview.png")),image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
        }
        static void PrepareModel(string file,string name,float length)
        {
            string source="Assets/FleetSurvival/Models/"+file;
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if(asset==null) return;
            Directory.CreateDirectory("Assets/FleetSurvival/Resources/Ships"); AssetDatabase.Refresh();
            var root=new GameObject(name);
            var model=UnityEngine.Object.Instantiate(asset,root.transform); model.name="Original textured model";
            var renderers=model.GetComponentsInChildren<Renderer>();
            Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if(bounds.size.x>bounds.size.z && bounds.size.x>bounds.size.y) model.transform.localRotation=Quaternion.Euler(0,90,0)*model.transform.localRotation;
            else if(bounds.size.y>bounds.size.z) model.transform.localRotation=Quaternion.Euler(90,0,0)*model.transform.localRotation;
            // These assets have different authored noses, including sideways fighter meshes.
            float forwardYaw=name=="Venator" || name=="Lucrehulk"?180:name=="ARC170" || name=="V19Torrent"?270:0;
            model.transform.localRotation=Quaternion.Euler(0,forwardYaw,0)*model.transform.localRotation;
            bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Debug.Log("MODEL_READY "+name+" sourceBounds="+bounds+" renderers="+renderers.Length);
            float factor=length/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            model.transform.localScale*=factor;
            model.transform.localPosition=-bounds.center*factor+Vector3.up*.6f;
            HullSectionBaker.Bake(root,name);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/FleetSurvival/Resources/Ships/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        }
        public static void PreviewImportedFleet()
        {
            EnsureResources(); PrepareSuppliedModel();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.2f,.24f,.3f); RenderSettings.skybox=null;
            var key=new GameObject("Key",typeof(Light)).GetComponent<Light>();
            key.type=LightType.Directional; key.intensity=.85f; key.color=new Color(1,.95f,.9f); key.transform.rotation=Quaternion.Euler(48,-30,0);
            var fill=new GameObject("Fill",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Directional; fill.intensity=.5f; fill.color=new Color(.6f,.75f,1); fill.transform.rotation=Quaternion.Euler(-30,140,0);
            var cisKey=new GameObject("CIS material fill",typeof(Light)).GetComponent<Light>();
            cisKey.type=LightType.Directional; cisKey.intensity=2.2f; cisKey.color=new Color(.95f,.96f,1);
            cisKey.cullingMask=1<<8; cisKey.transform.rotation=Quaternion.Euler(65,-40,0);
            var camera=new GameObject("Gallery camera",typeof(Camera)).GetComponent<Camera>();
            camera.backgroundColor=new Color(.025f,.037f,.06f); camera.clearFlags=CameraClearFlags.SolidColor;
            camera.orthographic=true; camera.orthographicSize=28; camera.aspect=1.6f;
            camera.transform.position=new Vector3(0,90,-70); camera.transform.LookAt(Vector3.zero); camera.farClipPlane=250;
            var font=Resources.Load<TMP_FontAsset>("CommanderFont");
            Label("CLONE WARS / IMPORTED FLEET",new Vector3(0,0,31),1.75f,new Color(.85f,.92f,1),camera,font);
            Label("YOUR MODELS  /  RENDERED IN UNITY",new Vector3(0,0,27),.75f,new Color(.42f,.6f,.75f),camera,font);
            string[] names={"Venator","Acclamator","Providence","Munificent","Recusant"};
            string[] roles={"REPUBLIC FLAGSHIP","REPUBLIC CRUISER","CIS FLAGSHIP","CIS FRIGATE","CIS DESTROYER"};
            for(int i=0;i<5;i++)
            {
                float x=i<3 ? -25+i*25 : -14+(i-3)*28, z=i<3 ? 11 : -15;
                var prefab=Resources.Load<GameObject>("Ships/"+names[i]);
                if(prefab==null) throw new Exception("Missing normalized ship: "+names[i]);
                var ship=UnityEngine.Object.Instantiate(prefab); ship.transform.position=new Vector3(x,0,z);
                if(i>=2) foreach(var child in ship.GetComponentsInChildren<Transform>()) child.gameObject.layer=8;
                if(i==1 || i==3) ship.transform.localScale=Vector3.one*1.4f;
                ship.transform.rotation=Quaternion.Euler(0,-25,0);
                Label(names[i].ToUpperInvariant(),new Vector3(x,0,z-12),1.05f,new Color(.88f,.93f,1),camera,font);
                Label(roles[i],new Vector3(x,0,z-15),.65f,i<2?FleetRules.Color(Faction.Republic):FleetRules.Color(Faction.CIS),camera,font);
            }
            SaveRender(camera,"Imported-fleet-preview.png");
        }
        static void SaveRender(Camera camera,string file)
        {
            var target=new RenderTexture(1920,1200,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1920,1200,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,1200),0,0); image.Apply();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../"+file)); File.WriteAllBytes(output,image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("FLEET_PREVIEW_SAVED "+output);
        }
        public static void ImportTextResources()
        {
            AssetDatabase.importPackageCompleted += package => { Debug.Log("TMP_RESOURCES_READY"); EditorApplication.Exit(0); };
            TMP_PackageResourceImporter.ImportResources(true,false,false);
        }
        public static void PreviewModels()
        {
            EnsureResources();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.35f,.43f,.55f); RenderSettings.skybox=null;
            var key=new GameObject("Key",typeof(Light)).GetComponent<Light>();
            key.type=LightType.Directional; key.intensity=1.35f; key.color=new Color(.83f,.9f,1); key.transform.rotation=Quaternion.Euler(45,-30,0);
            var fill=new GameObject("Fill",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Directional; fill.intensity=.7f; fill.color=new Color(.3f,.5f,1); fill.transform.rotation=Quaternion.Euler(-20,120,0);
            var camera=new GameObject("Preview camera",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.018f,.03f,.06f);
            camera.orthographic=true; camera.orthographicSize=27.5f; camera.aspect=1.6f;
            camera.transform.position=new Vector3(0,80,-55); camera.transform.LookAt(Vector3.zero);
            camera.nearClipPlane=.1f; camera.farClipPlane=200;
            var font=Resources.Load<TMP_FontAsset>("CommanderFont");
            string[] republic={"VENATOR","ACCLAMATOR","ARC-170"};
            string[] cis={"LUCREHULK","MUNIFICENT","VULTURE DROID"};
            Label("CLONE WARS / FLEET SURVIVAL",new Vector3(0,0,29),2f,new Color(.85f,.9f,1),camera,font);
            Label("PROTOTYPE SHIP MODELS  •  ACTUAL UNITY GEOMETRY",new Vector3(0,0,24),.8f,new Color(.4f,.55f,.7f),camera,font);
            for(int row=0;row<2;row++)
            {
                var faction=row==0?Faction.Republic:Faction.CIS;
                float z=row==0?11:-13;
                Label(row==0?"REPUBLIC":"CIS / SEPARATISTS",new Vector3(-27,0,z+(row==0?10:8)),.95f,FleetRules.Color(faction),camera,font);
                for(int col=0;col<3;col++)
                {
                    float x=-25+col*25;
                    var root=new GameObject(FleetRules.Stats(faction,(ShipClass)col).Name);
                    root.transform.position=new Vector3(x,0,z);
                    // Preview normalizes presentation sizes so the small fighter details are visible.
                    if(col==1) root.transform.localScale=Vector3.one*1.3f;
                    if(col==2) root.transform.localScale=Vector3.one*2.4f;
                    ShipVisuals.Build(root.transform,faction,(ShipClass)col);
                    Label(row==0?republic[col]:cis[col],new Vector3(x,0,z-11),1.15f,new Color(.85f,.9f,1),camera,font);
                    Label(col==0?"FLAGSHIP":col==1?"CRUISER / FRIGATE":"FIGHTER SQUADRON",new Vector3(x,0,z-13.5f),.7f,new Color(.4f,.55f,.7f),camera,font);
                }
            }
            var target=new RenderTexture(1920,1200,24); camera.targetTexture=target; camera.Render();
            RenderTexture.active=target;
            var image=new Texture2D(1920,1200,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1200),0,0); image.Apply();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Ship-model-preview.png"));
            File.WriteAllBytes(output,image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("MODEL_PREVIEW_SAVED "+output);
        }

        static void Label(string text,Vector3 position,float size,Color color,Camera camera,TMP_FontAsset font)
        {
            var label=new GameObject(text,typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.position=position; label.transform.rotation=camera.transform.rotation;
            label.text=text; label.font=font; label.fontSize=size*12; label.color=color;
            label.alignment=TextAlignmentOptions.Center; label.rectTransform.sizeDelta=new Vector2(75,5);
        }

        public static void EnsureResources()
        {
            if(!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                throw new Exception("Import TMP resources with FleetBuilder.ImportTextResources before rendering.");
            Directory.CreateDirectory("Assets/FleetSurvival/Resources"); AssetDatabase.Refresh();
            EnsureMaterial("FleetHull","Standard",Color.white);
            EnsureMaterial("FleetGlow","Unlit/Color",Color.white);
            EnsureMaterial("FleetParticle","FleetSurvival/SoftParticle",Color.white);
            EnsureMaterial("FleetHologram","FleetSurvival/Hologram",new Color(.1f,.7f,1,.18f));
            EnsureMaterial("FleetParticle","FleetSurvival/SoftParticle",Color.white);
            EnsureMaterial("FleetStars","Particles/Standard Unlit",new Color(.7f,.8f,1));
            ConfigureEnvironment();
            if(Resources.Load<TMP_FontAsset>("CommanderFont")==null)
            {
                AssetDatabase.CopyAsset("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset","Assets/FleetSurvival/Resources/CommanderFont.asset");
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureMaterial(string name,string shader,Color color)
        {
            string path="Assets/FleetSurvival/Resources/"+name+".mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null) return;
            var material=new Material(Shader.Find(shader)); material.color=color;
            AssetDatabase.CreateAsset(material,path); AssetDatabase.SaveAssets();
        }
        static void ConfigureEnvironment()
        {
            const string folder="Assets/FleetSurvival/Resources/Environment/";
            foreach(string file in new[]{"PlanetSurface","PlanetLights","PlanetClouds","SpaceNebula"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+file+".png");
                if(importer==null) throw new Exception("Missing orbital environment texture: "+file);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=file=="PlanetSurface" || file=="SpaceNebula";
                importer.alphaIsTransparency=false;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
                importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
                importer.textureCompression=file=="SpaceNebula"?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            EnvironmentMaterial("FleetSpaceSky","FleetSurvival/SpaceSky",new[]{"_MainTex","SpaceNebula"});
            EnvironmentMaterial("FleetPlanet","FleetSurvival/PlanetSurface",new[]{"_MainTex","PlanetSurface","_LightsTex","PlanetLights","_CloudTex","PlanetClouds"});
            EnvironmentMaterial("FleetPlanetClouds","FleetSurvival/PlanetClouds",new[]{"_MainTex","PlanetClouds"});
            EnvironmentMaterial("FleetPlanetAtmosphere","FleetSurvival/PlanetAtmosphere",new string[0]);
            AssetDatabase.SaveAssets();
        }
        static void EnvironmentMaterial(string name,string shaderName,string[] textures)
        {
            string path="Assets/FleetSurvival/Resources/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find(shaderName);if(shader==null) throw new Exception("Missing environment shader: "+shaderName);
            if(material==null) { material=new Material(shader);AssetDatabase.CreateAsset(material,path); } else material.shader=shader;
            for(int i=0;i<textures.Length;i+=2) material.SetTexture(textures[i],AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FleetSurvival/Resources/Environment/"+textures[i+1]+".png"));
            EditorUtility.SetDirty(material);
        }

        public static void Build()
        {
            EnsureResources();
            ConfigureAudioImports();
            PrepareSuppliedModel();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Fleet Survival",typeof(FleetGame));
            string scene="Assets/FleetSurvival/FleetSurvival.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene,true)};
            PlayerSettings.companyName="FleetPrototype"; PlayerSettings.productName="Clone Wars Fleet Survival";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Windows/CloneWarsFleetSurvival.exe"));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            Debug.Log("FLEET_BUILD_RESULT "+report.summary.result);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows game build failed.");
        }
        public static void BuildAndPreview()
        { Build(); PreviewForwardDirections(); PreviewAudio(); }
        static void ConfigureAudioImports()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/FleetSurvival/Resources/Audio"}))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.forceToMono=true; importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;
                settings.preloadAudioData=true;
                settings.loadType=AudioClipLoadType.DecompressOnLoad; settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate; settings.sampleRateOverride=44100;
                importer.defaultSampleSettings=settings; importer.SaveAndReimport();
            }
        }
        public static void PreviewAudio()
        {
            var samples=new System.Collections.Generic.List<float>();
            foreach(var name in new[]{"VenatorCannon01","MunificentCannon01","ARC170Cannon01","VultureCannon01","VWingCannon01","HyperspaceCharge","HyperspaceExit","Explosion"})
            {
                var clip=FleetSound.Get(name); var data=new float[clip.samples]; clip.GetData(data,0);
                samples.AddRange(data); samples.AddRange(new float[22050]);
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Audio-preview.wav"));
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+samples.Count*2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(44100); writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Count*2);
                foreach(var value in samples) writer.Write((short)(Mathf.Clamp(value,-1,1)*32767));
            }
        }
    }
}
