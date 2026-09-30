using System.Collections.Generic;
using UnityEngine;

namespace FleetSurvival
{
    public static class ShipVisuals
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Material Material(Color color, bool glow = false)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + glow;
            if (Materials.TryGetValue(key, out var cached)) return cached;
            var source = Resources.Load<Material>(glow ? "FleetGlow" : "FleetHull");
            var mat = source != null ? new Material(source) : new Material(Shader.Find(glow ? "Unlit/Color" : "Standard"));
            mat.color = color;
            if (!glow) { mat.SetFloat("_Glossiness", .38f); mat.SetFloat("_Metallic", .35f); }
            Materials[key] = mat;
            return mat;
        }

        static Transform Part(Transform parent, string name, PrimitiveType shape, Vector3 pos, Vector3 scale, Color color, bool glow = false)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            if(Application.isPlaying) Object.Destroy(part.GetComponent<Collider>());
            else Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = Material(color, glow);
            return part.transform;
        }

        static Transform Wedge(Transform parent, string name, float width, float length, float height, Color color)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var vertices = new[] { new Vector3(-width/2,0,-length/2), new Vector3(width/2,0,-length/2), new Vector3(0,0,length/2),
                new Vector3(-width/2,height,-length/2), new Vector3(width/2,height,-length/2), new Vector3(0,height*.3f,length/2) };
            var mesh = new Mesh { name = name + "Mesh", vertices = vertices,
                triangles = new[] { 3,5,4, 0,1,2, 0,3,4, 0,4,1, 1,4,5, 1,5,2, 2,5,3, 2,3,0 } };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            return go.transform;
        }

        public static LineRenderer Ring(Transform parent, float radius, Color color, float width, string name = "SelectionRing")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 64;
            line.sharedMaterial = Material(Color.white, true);
            line.startColor = line.endColor = color; line.startWidth = line.endWidth = width;
            for (int i=0;i<64;i++) { float a=i*Mathf.PI*2/64; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,-.3f,Mathf.Sin(a)*radius)); }
            return line;
        }

        static void CarrierRing(Transform parent,Color color)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            const int segments=40;
            for(int i=0;i<segments;i++)
            {
                float a=(35+i*290f/segments)*Mathf.Deg2Rad, b=(35+(i+1)*290f/segments)*Mathf.Deg2Rad;
                int n=vertices.Count;
                foreach(float angle in new[]{a,b})
                {
                    vertices.Add(new Vector3(Mathf.Sin(angle)*5,-.5f,Mathf.Cos(angle)*5));
                    vertices.Add(new Vector3(Mathf.Sin(angle)*7,-.5f,Mathf.Cos(angle)*7));
                    vertices.Add(new Vector3(Mathf.Sin(angle)*5,1.1f,Mathf.Cos(angle)*5));
                    vertices.Add(new Vector3(Mathf.Sin(angle)*7,1.1f,Mathf.Cos(angle)*7));
                }
                int[] faces={2,6,7,2,7,3, 0,1,5,0,5,4, 0,4,6,0,6,2, 1,3,7,1,7,5};
                foreach(int index in faces) triangles.Add(n+index);
                if(i==0) foreach(int index in new[]{0,2,3,0,3,1}) triangles.Add(n+index);
                if(i==segments-1) foreach(int index in new[]{4,5,7,4,7,6}) triangles.Add(n+index);
            }
            var mesh=new Mesh{name="Lucrehulk carrier ring",vertices=vertices.ToArray(),triangles=triangles.ToArray()};
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go=new GameObject("Carrier ring",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=Material(color);
        }

        public static void Build(Transform root, Faction faction, ShipClass kind)
        {
            if(kind!=ShipClass.Fighter && kind!=ShipClass.Interceptor) { BuildSingle(root,faction,kind); return; }
            var group=new GameObject("Six-fighter squadron").transform; group.SetParent(root,false);
            foreach(var offset in FleetSquadron.Offsets)
            {
                var craft=new GameObject("Squadron fighter").transform; craft.SetParent(group,false);
                craft.localPosition=offset; craft.localScale=Vector3.one*FleetSquadron.CraftScale;
                BuildSingle(craft,faction,kind);
            }
        }

        static void BuildSingle(Transform root, Faction faction, ShipClass kind)
        {
            string assetName = faction==Faction.Republic
                ? (kind==ShipClass.Flagship || kind==ShipClass.Destroyer || kind==ShipClass.Carrier ? "VenatorDetailed" : kind==ShipClass.Frigate ? "Acclamator" : kind==ShipClass.Escort ? "Arquitens" : kind==ShipClass.Interceptor ? "V19Torrent" : "ARC170")
                : (kind==ShipClass.Flagship ? "Providence" : kind==ShipClass.Frigate || kind==ShipClass.Escort ? "Munificent" : kind==ShipClass.Destroyer ? "Recusant" : kind==ShipClass.Carrier ? "Lucrehulk" : "Vulture");
            if(assetName!=null)
            {
                var supplied=Resources.Load<GameObject>("Ships/"+assetName);
                if(supplied!=null)
                {
                    var instance=Object.Instantiate(supplied,root,false);
                    if(kind==ShipClass.Destroyer && faction==Faction.Republic) instance.transform.localScale=Vector3.one*.8f;
                    if(kind==ShipClass.Escort && faction==Faction.CIS) instance.transform.localScale=Vector3.one*.8f;
                    if(faction==Faction.CIS) foreach(var part in instance.GetComponentsInChildren<Transform>()) part.gameObject.layer=8;
                    return;
                }
            }
            Color hull = new Color(.52f,.6f,.68f), dark = new Color(.16f,.22f,.3f);
            Color trim = faction == Faction.Republic ? new Color(.6f,.12f,.11f) : new Color(.16f,.35f,.62f);
            Color engine = new Color(.25f,.8f,1f);
            float scale = kind == ShipClass.Flagship ? 1 : kind == ShipClass.Frigate ? .64f : .27f;
            var body = new GameObject("ShipModel").transform;
            body.SetParent(root, false); body.localScale = Vector3.one * scale;
            if (faction == Faction.Republic)
            {
                if (kind == ShipClass.Fighter)
                {
                    Part(body,"Fuselage",PrimitiveType.Cube,new Vector3(0,.15f,0),new Vector3(1.5f,1,7),hull);
                    Part(body,"Cockpit",PrimitiveType.Sphere,new Vector3(0,.7f,1),new Vector3(1.6f,.8f,2),dark);
                    for(int s=-1;s<=1;s+=2)
                    {
                        var wing=Part(body,"S-foil",PrimitiveType.Cube,new Vector3(s*3.4f,.15f,-.4f),new Vector3(5,.2f,2),trim);
                        wing.localRotation=Quaternion.Euler(0,0,s*12);
                        Part(body,"EnginePod",PrimitiveType.Capsule,new Vector3(s*2.7f,0,-1),new Vector3(.8f,2,.8f),hull).localRotation=Quaternion.Euler(90,0,0);
                        Part(body,"IonEngine",PrimitiveType.Sphere,new Vector3(s*2.7f,0,-3),new Vector3(.75f,.75f,.3f),engine,true);
                        Part(body,"Cannon",PrimitiveType.Cube,new Vector3(s*5.5f,0,1),new Vector3(.22f,.22f,3),dark);
                    }
                }
                else
                {
                    bool venator=kind==ShipClass.Flagship;
                    Wedge(body,"Armored hull",venator?8:10,venator?14:11,1.3f,hull);
                    Wedge(body,"Dorsal armor",venator?6.3f:8,venator?11:8,.7f,new Color(.65f,.7f,.74f)).localPosition=new Vector3(0,1.1f,-.7f);
                    Part(body,"Red flight deck",PrimitiveType.Cube,new Vector3(0,1.85f,-1.2f),new Vector3(venator?1.15f:.65f,.14f,venator?9:6),trim);
                    if(!venator)
                    {
                        Part(body,"Central command bridge",PrimitiveType.Cube,new Vector3(0,2.5f,-3.7f),new Vector3(2,1.2f,1.6f),dark);
                        Part(body,"Bridge windows",PrimitiveType.Cube,new Vector3(0,3.05f,-2.87f),new Vector3(1.65f,.16f,.06f),engine,true);
                    }
                    for(int s=-1;s<=1;s+=2)
                    {
                        if(venator)
                        {
                            Part(body,"Bridge tower",PrimitiveType.Cube,new Vector3(s*1.5f,2.4f,-4.4f),new Vector3(.85f,1.5f,1.7f),dark);
                            Part(body,"Bridge windows",PrimitiveType.Cube,new Vector3(s*1.5f,3.05f,-3.5f),new Vector3(.65f,.18f,.06f),engine,true);
                        }
                        for(int i=0;i<3;i++) Part(body,"Turbolaser",PrimitiveType.Cube,new Vector3(s*(2.6f-i*.4f),1.6f,-3+i*2.3f),new Vector3(.6f,.5f,1.3f),dark);
                        Part(body,"Engine glow",PrimitiveType.Sphere,new Vector3(s*2.4f,.4f,venator?-7.1f:-5.6f),new Vector3(1.3f,1.1f,.4f),engine,true);
                    }
                }
            }
            else if(kind == ShipClass.Flagship)
            {
                CarrierRing(body,hull);
                for(int i=0;i<8;i++)
                {
                    float a=(50+i*36)*Mathf.Deg2Rad;
                    var segment=Part(body,"Armored dorsal stripe",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*6,1.16f,Mathf.Cos(a)*6),new Vector3(.45f,.1f,1.8f),trim);
                    segment.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                }
                Part(body,"Core sphere",PrimitiveType.Sphere,new Vector3(0,.6f,0),new Vector3(5,4,5),hull);
                Part(body,"Command spine",PrimitiveType.Cube,new Vector3(0,.3f,-4),new Vector3(2.7f,1.2f,4),dark);
                Part(body,"Control bridge",PrimitiveType.Cube,new Vector3(0,2.55f,0),new Vector3(2,.8f,1.3f),trim);
                for(int s=-1;s<=1;s+=2) Part(body,"Reactor exhaust",PrimitiveType.Sphere,new Vector3(s*3,.1f,-6.2f),new Vector3(1.3f,1,.5f),engine,true);
            }
            else if(kind == ShipClass.Frigate)
            {
                Part(body,"Forward hull",PrimitiveType.Capsule,new Vector3(0,.2f,2),new Vector3(3,3.5f,3),hull).localRotation=Quaternion.Euler(90,0,0);
                Part(body,"Spinal rail",PrimitiveType.Cube,new Vector3(0,.2f,-.5f),new Vector3(1.1f,1.1f,12),dark);
                for(int s=-1;s<=1;s+=2)
                {
                    var flank=Part(body,"Armor fin",PrimitiveType.Cube,new Vector3(s*2.5f,.2f,-1),new Vector3(1.6f,2,8),hull);
                    flank.localRotation=Quaternion.Euler(0,s*9,0);
                    Part(body,"Blue hull stripe",PrimitiveType.Cube,new Vector3(s*2.5f,1.25f,-1),new Vector3(.65f,.1f,6),trim);
                    Part(body,"Engine glow",PrimitiveType.Sphere,new Vector3(s*2.2f,0,-5.2f),new Vector3(1.2f,1,.35f),engine,true);
                }
                Part(body,"Command pod",PrimitiveType.Sphere,new Vector3(0,1.2f,-4),new Vector3(2,1.5f,3),trim);
            }
            else
            {
                Part(body,"Droid core",PrimitiveType.Sphere,Vector3.zero,new Vector3(2.5f,1.3f,4),dark);
                for(int s=-1;s<=1;s+=2)
                {
                    var wing=Part(body,"Vulture blade",PrimitiveType.Cube,new Vector3(s*2.7f,.5f,.3f),new Vector3(.8f,1.4f,6.5f),trim);
                    wing.localRotation=Quaternion.Euler(0,-s*12,-s*22);
                    Part(body,"Wing tip",PrimitiveType.Cube,new Vector3(s*2.2f,.5f,3.4f),new Vector3(.8f,1.4f,2),hull);
                    Part(body,"Engine glow",PrimitiveType.Sphere,new Vector3(s*2.8f,.5f,-3),new Vector3(.65f,.65f,.25f),engine,true);
                }
                Part(body,"Droid sensor",PrimitiveType.Sphere,new Vector3(0,.4f,1.8f),new Vector3(.7f,.3f,.4f),new Color(1,.2f,.1f),true);
            }
        }
    }
}
