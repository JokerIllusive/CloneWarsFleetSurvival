using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace FleetSurvival
{
    public sealed class FleetEnvironment : MonoBehaviour
    {
        FleetGame game;
        Transform planet;
        float cloudOffset;
        sealed class Rock { public Transform Root;public float Radius,Height,Angle,Speed;public Vector3 Spin; }
        readonly List<Rock> rocks=new List<Rock>();
        public int AsteroidCount => rocks.Count;
        public void Initialize(FleetGame owner,Transform sector,Vector3 sunlight)
        {
            game=owner; transform.SetParent(sector,false);
            Shader.SetGlobalVector("_FleetSunDirection",sunlight.normalized);
            Shader.SetGlobalFloat("_FleetCloudOffset",0);
            RenderSettings.skybox=Resources.Load<Material>("FleetSpaceSky");
            game.ViewCamera.clearFlags=CameraClearFlags.Skybox;
            var mesh=CreateSphere(192,96);
            planet=Layer("Geonosis",transform,mesh,"FleetPlanet",66);
            planet.localPosition=new Vector3(130,-110,85);
            planet.localRotation=Quaternion.Euler(12,28,-18);
            Layer("Geonosis dust haze",planet,mesh,"FleetPlanetClouds",1.008f);
            Layer("Scattering atmosphere",planet,mesh,"FleetPlanetAtmosphere",1.026f);
            Shader.SetGlobalVector("_FleetPlanetCenter",planet.position);Shader.SetGlobalFloat("_FleetPlanetRadius",66);
            var ring=Layer("Geonosis rocky ring",planet,CreateRing(),"FleetGeonosisRing",1);
            ring.localRotation=Quaternion.Euler(16,0,9);
            BuildAsteroids();
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
        { TickVisuals(Time.deltaTime); }
        public void TickVisuals(float dt)
        {
            if(game==null || game.Paused) return;
            planet.Rotate(Vector3.up,.045f*dt,Space.Self);
            cloudOffset=Mathf.Repeat(cloudOffset+dt*.00013f,1);
            Shader.SetGlobalFloat("_FleetCloudOffset",cloudOffset);
            foreach(var rock in rocks)
            {
                rock.Angle+=dt*rock.Speed;
                rock.Root.localPosition=new Vector3(Mathf.Cos(rock.Angle)*rock.Radius,rock.Height,Mathf.Sin(rock.Angle)*rock.Radius*.72f);
                rock.Root.Rotate(rock.Spin*dt,Space.Self);
            }
        }
        void BuildAsteroids()
        {
            var meshes=new Mesh[4];
            for(int m=0;m<4;m++)
            {
                var mesh=CreateSphere(16,10);var v=mesh.vertices;
                for(int i=0;i<v.Length;i++)
                {
                    var p=v[i];float rough=Mathf.PerlinNoise(p.x*2.8f+m*9,p.y*2.8f+p.z*1.2f+m*3);
                    v[i]=Vector3.Scale(p*(.7f+rough*.65f),new Vector3(1+m*.12f,.83f,1.04f));
                }
                mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.name="Craggy asteroid variant "+m;meshes[m]=mesh;
            }
            var rng=new System.Random(4117);
            for(int i=0;i<36;i++)
            {
                var root=Layer("Drifting asteroid "+i.ToString("00"),transform,meshes[i%4],"FleetAsteroidRock",.7f+(float)rng.NextDouble()*2.3f);
                var rock=new Rock{Root=root,Radius=26+(float)rng.NextDouble()*104,Height=-25-(float)rng.NextDouble()*26,Angle=(float)rng.NextDouble()*Mathf.PI*2,Speed=.008f+(float)rng.NextDouble()*.012f,Spin=new Vector3(3+i%7,4+i%11,2+i%5)};
                root.localPosition=new Vector3(Mathf.Cos(rock.Angle)*rock.Radius,rock.Height,Mathf.Sin(rock.Angle)*rock.Radius*.72f);
                root.localRotation=Quaternion.Euler(i*43,i*87,i*29);rocks.Add(rock);
            }
        }
        static Mesh CreateRing()
        {
            const int segments=192;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float t=i/(float)segments,a=t*Mathf.PI*2;
                for(int side=0;side<2;side++) {vertices[i*2+side]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(side==0?1.28f:1.84f);uv[i*2+side]=new Vector2(t,side);}
                if(i==segments) continue;int p=i*2,n=i*6;
                triangles[n]=p;triangles[n+1]=p+2;triangles[n+2]=p+1;triangles[n+3]=p+1;triangles[n+4]=p+2;triangles[n+5]=p+3;
            }
            var mesh=new Mesh{name="Geonosis debris ring",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateBounds();return mesh;
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
