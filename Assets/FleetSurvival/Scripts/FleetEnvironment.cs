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
        sealed class Rock { public Transform Root;public float Radius,Height,Angle,Speed;public Vector3 Spin,Center; }
        readonly List<Rock> rocks=new List<Rock>();
        readonly List<ParticleSystem> dust=new List<ParticleSystem>();
        Mesh box;
        public int LandmarkCount { get; private set; }
        public int AsteroidCount { get; private set; }
        public void Initialize(FleetGame owner,Transform sector,Vector3 sunlight)
        {
            game=owner; transform.SetParent(sector,false);
            Shader.SetGlobalVector("_FleetSunDirection",sunlight.normalized);
            Shader.SetGlobalFloat("_FleetCloudOffset",0);
            RenderSettings.skybox=Resources.Load<Material>("FleetSpaceSky");
            game.ViewCamera.clearFlags=CameraClearFlags.Skybox;
            var mesh=CreateSphere(192,96);
            planet=Layer("Geonosis",transform,mesh,"FleetPlanet",66);
            planet.localPosition=new Vector3(148,-122,102);
            planet.localRotation=Quaternion.Euler(12,28,-18);
            Layer("Geonosis dust haze",planet,mesh,"FleetPlanetClouds",1.008f);
            Layer("Scattering atmosphere",planet,mesh,"FleetPlanetAtmosphere",1.026f);
            Shader.SetGlobalVector("_FleetPlanetCenter",planet.position);Shader.SetGlobalFloat("_FleetPlanetRadius",66);
            var ring=Layer("Geonosis rocky ring",planet,CreateRing(),"FleetGeonosisRing",1);
            ring.localRotation=Quaternion.Euler(16,0,9);
            BuildAsteroids();
            BuildOrbitalRuins();
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
                rock.Root.localPosition=rock.Center+new Vector3(Mathf.Cos(rock.Angle)*rock.Radius,rock.Height,Mathf.Sin(rock.Angle)*rock.Radius*.72f);
                rock.Root.Rotate(rock.Spin*dt,Space.Self);
            }
            foreach(var cloud in dust) cloud.Simulate(dt,true,false);
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
            for(int i=0;i<112;i++)
            {
                bool belt=i>=28;float size=.35f+(float)rng.NextDouble()*(belt?1.9f:2.8f);
                if(i==31 || i==64 || i==92) size=4.7f;
                var root=Layer("Drifting asteroid "+i.ToString("00"),transform,meshes[i%4],"FleetAsteroidRock",size);
                // Loose rocks fill the depth; the denser belt stays along the sector perimeter.
                var rock=new Rock{Root=root,Radius=(belt?82:28)+(float)rng.NextDouble()*(belt?35:72),Height=-26-(float)rng.NextDouble()*32,Angle=(float)rng.NextDouble()*Mathf.PI*2,Speed=.0018f+(float)rng.NextDouble()*.0035f,Spin=new Vector3(1+i%4,2+i%5,1+i%3)};
                if(belt) rock.Angle=(i%2==0?2.1f:.1f)+(float)rng.NextDouble()*.9f;
                root.localPosition=new Vector3(Mathf.Cos(rock.Angle)*rock.Radius,rock.Height,Mathf.Sin(rock.Angle)*rock.Radius*.72f);
                root.localRotation=Quaternion.Euler(i*43,i*87,i*29);rocks.Add(rock);
                AsteroidCount++;
            }
        }
        void BuildOrbitalRuins()
        {
            box=CreateBox();
            BuildShipyard(new Vector3(-48,-20,48),-24,1);
            BuildShipyard(new Vector3(62,-37,85),38,.65f);
            BuildDust("Outer belt dust",new Vector3(-60,-33,42),new Vector3(43,6,30),711);
            BuildDust("Shipyard dust",new Vector3(57,-40,68),new Vector3(32,5,22),919);
        }
        void BuildShipyard(Vector3 position,float heading,float size)
        {
            var yard=new GameObject("Abandoned orbital shipyard "+LandmarkCount).transform;
            yard.SetParent(transform,false);yard.localPosition=position;yard.localRotation=Quaternion.Euler(9,heading,-6);yard.localScale=Vector3.one*size;LandmarkCount++;
            var hull=new List<CombineInstance>();var trim=new List<CombineInstance>();var lamps=new List<CombineInstance>();
            AddBox(hull,new Vector3(0,0,0),new Vector3(2.8f,2,19));
            AddBox(hull,new Vector3(0,2,-6),new Vector3(5,3.2f,4));
            AddBox(trim,new Vector3(0,3.7f,-6),new Vector3(6,.3f,5));
            for(int bay=0;bay<5;bay++)
            {
                float z=-6+bay*3.7f;
                foreach(int side in new[]{-1,1})
                {
                    // An open docking gantry, with trusses and lit service strips.
                    float x=side*(bay==3?6:8);
                    AddBox(hull,new Vector3(x*.5f,0,z),new Vector3(Mathf.Abs(x),.55f,.7f));
                    AddBox(hull,new Vector3(x,1.5f,z),new Vector3(.7f,3.5f,.8f));
                    AddBox(trim,new Vector3(x*.5f,3.1f,z),new Vector3(Mathf.Abs(x),.35f,.6f));
                    AddBox(lamps,new Vector3(x*.5f,3.34f,z),new Vector3(Mathf.Abs(x)*.76f,.1f,.13f));
                    AddBox(trim,new Vector3(x*.55f,1.5f,z),new Vector3(Mathf.Abs(x)*.92f,.18f,.18f),side*20);
                    AddBox(lamps,new Vector3(x,1.5f,z-.46f),new Vector3(.19f,.7f,.1f));
                }
            }
            for(int i=0;i<8;i++) AddBox(lamps,new Vector3(-1.42f,1.5f,-7+i*1.9f),new Vector3(.08f,.15f,.7f));
            AddBox(hull,new Vector3(1,5.5f,-6),new Vector3(.2f,4,.2f));
            AddBox(trim,new Vector3(1,7,-6),new Vector3(3,.2f,.35f));
            Combine(yard,"Docking gantries",hull,ShipVisuals.Material(new Color(.23f,.22f,.21f)));
            Combine(yard,"Exposed trusses",trim,ShipVisuals.Material(new Color(.37f,.3f,.2f)));
            Combine(yard,"Service lights",lamps,ShipVisuals.Material(new Color(.65f,.32f,.1f),true));
            for(int i=0;i<12;i++)
            {
                var fragment=MeshPart("Orbital scrap",transform,box,ShipVisuals.Material(new Color(.27f,.26f,.24f)));
                fragment.localScale=new Vector3(.3f+i%3*.25f,.18f,1+i%4*.35f);
                var rock=new Rock{Root=fragment,Center=position,Radius=13+i*.6f,Height=-1-i%4,Angle=i*2.4f,Speed=.008f,Spin=new Vector3(1,2,1)};
                fragment.localPosition=position+new Vector3(Mathf.Cos(rock.Angle)*rock.Radius,rock.Height,Mathf.Sin(rock.Angle)*rock.Radius*.72f);rocks.Add(rock);
            }
        }
        void AddBox(List<CombineInstance> pieces,Vector3 position,Vector3 scale,float tilt=0)
        { pieces.Add(new CombineInstance{mesh=box,transform=Matrix4x4.TRS(position,Quaternion.Euler(0,0,tilt),scale)}); }
        static Transform MeshPart(string name,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go.transform;
        }
        static void Combine(Transform parent,string name,List<CombineInstance> pieces,Material material)
        { var mesh=new Mesh{name=name};mesh.CombineMeshes(pieces.ToArray(),true,true);MeshPart(name,parent,mesh,material); }
        void BuildDust(string name,Vector3 position,Vector3 extent,uint seed)
        {
            var go=new GameObject(name,typeof(ParticleSystem));go.transform.SetParent(transform,false);go.transform.localPosition=position;
            var cloud=go.GetComponent<ParticleSystem>();cloud.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            cloud.useAutoRandomSeed=false;cloud.randomSeed=seed;
            var main=cloud.main;main.playOnAwake=false;main.loop=true;main.startLifetime=100;main.startSpeed=.025f;main.maxParticles=130;
            main.startSize=new ParticleSystem.MinMaxCurve(3,7);main.startColor=new Color(.22f,.16f,.1f,.09f);
            var emission=cloud.emission;emission.rateOverTime=1.2f;
            var shape=cloud.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=extent;
            var renderer=cloud.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("FleetParticle");
            var fade=cloud.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1)});fade.color=gradient;
            // Manually simulated with the sector clock, so tactical pause freezes even the haze.
            cloud.Simulate(100,true,true);cloud.Pause();dust.Add(cloud);
        }
        static Mesh CreateBox()
        {
            var v=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
            var mesh=new Mesh{name="Orbital structure block",vertices=v,triangles=new[]{0,2,1,0,3,2,5,6,4,4,6,7,4,7,0,0,7,3,1,2,5,5,2,6,3,7,2,2,7,6,4,0,5,5,0,1}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
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
