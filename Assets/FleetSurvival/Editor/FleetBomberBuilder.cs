using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace FleetSurvival.Editor
{
    public static partial class FleetBuilder
    {
        static List<Mesh> FreezeSkinnedMeshes(GameObject model)
        {
            var meshes=new List<Mesh>();
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh{name=skin.sharedMesh.name+" flight pose"};skin.BakeMesh(mesh);
                var materials=skin.sharedMaterials;var body=skin.gameObject;
                UnityEngine.Object.DestroyImmediate(skin);
                body.AddComponent<MeshFilter>().sharedMesh=mesh;
                body.AddComponent<MeshRenderer>().sharedMaterials=materials;
                meshes.Add(mesh);
            }
            return meshes;
        }
        public static void PrepareBomberFleet()
        {
            PrepareModel("hyena_bomber.glb","Hyena",4);
            PrepareModel("btl-b_y-wing.glb","YWing",4.6f);
            const string pack="dread_naughts_star_wars.glb";
            PrepareModel(pack,"Munificent",10,"cis_munificent_ARM_4");
            PrepareModel(pack,"Providence",17,"cis_providence_ARM_10");
            PrepareModel(pack,"Recusant",13,"cis_recusant_ARM_13");
            PrepareModel(pack,"Lucrehulk",18,"cis_lucrehulk_ARM_7");
        }
        public static void PreviewBomberFleet()
        {
            EnsureResources();PrepareBomberFleet();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.42f,.47f,.56f);RenderSettings.skybox=null;
            var key=new GameObject("Fleet model key",typeof(Light)).GetComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(50,-30,0);
            var fill=new GameObject("Fleet model fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(-25,140,0);
            var camera=new GameObject("Bomber and CIS hull preview",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=19;camera.aspect=1.6f;camera.farClipPlane=250;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.028f,.045f);
            camera.transform.position=new Vector3(0,80,0);camera.transform.LookAt(Vector3.zero,Vector3.forward);
            var font=Resources.Load<TMP_FontAsset>("CommanderFont");
            string[] names={"YWing","Hyena","Munificent","Providence","Recusant","Lucrehulk"};
            string[] titles={"REPUBLIC / BTL-B Y-WING","CIS / HYENA BOMBER","MUNIFICENT","PROVIDENCE","RECUSANT","LUCREHULK"};
            for(int page=0;page<2;page++)
            {
                var models=new List<GameObject>();var labels=new List<GameObject>();
                int start=page==0?0:2,count=page==0?2:4;
                for(int n=0;n<count;n++)
                {
                    int i=start+n;var p=page==0?new Vector3(n==0?-14:14,0,0):new Vector3(n%2==0?-14:14,0,n<2?10:-10);
                    var prefab=Resources.Load<GameObject>("Ships/"+names[i]);if(prefab==null)throw new Exception("Missing preview model: "+names[i]);
                    var model=UnityEngine.Object.Instantiate(prefab);models.Add(model);model.transform.position=p;model.transform.localScale=Vector3.one*(page==0?3:.9f);
                    Label(titles[i],p+new Vector3(0,0,page==0?-10:-8),.6f,new Color(.86f,.94f,1),camera,font);
                    var arrow=new GameObject("Forward +Z",typeof(LineRenderer));models.Add(arrow);var line=arrow.GetComponent<LineRenderer>();line.sharedMaterial=ShipVisuals.Material(new Color(.1f,1,.45f),true);line.startWidth=line.endWidth=.07f;line.positionCount=5;line.SetPositions(new[]{p+new Vector3(10,3,-3),p+new Vector3(10,3,4),p+new Vector3(9.4f,3,2.8f),p+new Vector3(10,3,4),p+new Vector3(10.6f,3,2.8f)});
                }
                Label(page==0?"BOMBER MODELS / GREEN ARROWS SHOW FLIGHT DIRECTION":"SUPPLIED CIS CAPITAL SHIPS / NORMALIZED GAME SCALE",new Vector3(0,0,17),.55f,new Color(.5f,.75f,.9f),camera,font);
                SaveRender(camera,page==0?"Bomber-forward-preview.png":"CIS-capital-forward-preview.png");
                camera.transform.position=new Vector3(0,60,-35);camera.transform.LookAt(Vector3.zero);
                foreach(var label in UnityEngine.Object.FindObjectsOfType<TextMeshPro>()){label.transform.rotation=camera.transform.rotation;labels.Add(label.gameObject);}
                SaveRender(camera,page==0?"Bomber-models-preview.png":"CIS-capital-models-preview.png");
                foreach(var model in models)UnityEngine.Object.DestroyImmediate(model);foreach(var label in labels)UnityEngine.Object.DestroyImmediate(label);
                camera.transform.position=new Vector3(0,80,0);camera.transform.LookAt(Vector3.zero,Vector3.forward);
            }
        }
    }
}
