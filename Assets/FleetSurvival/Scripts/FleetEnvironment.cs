using UnityEngine;
using UnityEngine.Rendering;

namespace FleetSurvival
{
    public sealed class FleetEnvironment : MonoBehaviour
    {
        FleetGame game;
        Transform planet;
        float cloudOffset;
        public void Initialize(FleetGame owner,Transform sector,Vector3 sunlight)
        {
            game=owner; transform.SetParent(sector,false);
            Shader.SetGlobalVector("_FleetSunDirection",sunlight.normalized);
            Shader.SetGlobalFloat("_FleetCloudOffset",0);
            RenderSettings.skybox=Resources.Load<Material>("FleetSpaceSky");
            game.ViewCamera.clearFlags=CameraClearFlags.Skybox;
            var mesh=CreateSphere(192,96);
            planet=Layer("Outer Rim ocean planet",transform,mesh,"FleetPlanet",66);
            planet.localPosition=new Vector3(130,-110,85);
            planet.localRotation=Quaternion.Euler(12,28,-18);
            Layer("Planet weather layer",planet,mesh,"FleetPlanetClouds",1.008f);
            Layer("Scattering atmosphere",planet,mesh,"FleetPlanetAtmosphere",1.026f);
        }
        static Transform Layer(string name,Transform parent,Mesh mesh,string material,float size)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.transform.localScale=Vector3.one*size;go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>(material);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go.transform;
        }
        void Update()
        {
            if(game==null || game.Paused) return;
            float dt=Time.deltaTime;
            planet.Rotate(Vector3.up,.045f*dt,Space.Self);
            cloudOffset=Mathf.Repeat(cloudOffset+dt*.00013f,1);
            Shader.SetGlobalFloat("_FleetCloudOffset",cloudOffset);
        }
        static Mesh CreateSphere(int longitude,int latitude)
        {
            var vertices=new Vector3[(longitude+1)*(latitude+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
            for(int y=0;y<=latitude;y++) for(int x=0;x<=longitude;x++)
            {
                float u=x/(float)longitude,v=y/(float)latitude,a=u*Mathf.PI*2,b=v*Mathf.PI;
                int index=y*(longitude+1)+x;
                vertices[index]=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                normals[index]=vertices[index];uv[index]=new Vector2(u,1-v);
            }
            var triangles=new int[longitude*latitude*6];int t=0;
            for(int y=0;y<latitude;y++) for(int x=0;x<longitude;x++)
            {
                int a=y*(longitude+1)+x,b=a+longitude+1;
                triangles[t++]=a;triangles[t++]=a+1;triangles[t++]=b;
                triangles[t++]=a+1;triangles[t++]=b+1;triangles[t++]=b;
            }
            var mesh=new Mesh{name="Smooth orbital sphere",vertices=vertices,normals=normals,uv=uv,triangles=triangles};
            mesh.RecalculateBounds();return mesh;
        }
    }
}
