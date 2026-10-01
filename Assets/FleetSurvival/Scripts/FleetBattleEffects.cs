using UnityEngine;

namespace FleetSurvival
{
    // Shared geometry, a fixed flash pool, and a maximum of eight unshadowed lights.
    public sealed class FleetBattleEffects
    {
        public static Mesh Quad
        {
            get
            {
                if(quad==null) {quad=new Mesh{name="Soft battle glow"};quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.colors=new[]{Color.white,Color.white,Color.white,Color.white};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();}
                return quad;
            }
        }
        static Mesh quad;
        readonly FleetGame game;
        readonly Flash[] flashes=new Flash[32];
        int next;
        sealed class Flash {public Transform Root;public MeshRenderer Halo,Core;public Light Light;public ParticleSystem Sparks;public float Age,Duration,Size;public Color Color;public readonly MaterialPropertyBlock Block=new MaterialPropertyBlock();}
        public FleetBattleEffects(FleetGame owner) {game=owner;}
        public int ActiveFlashes {get {int count=0;foreach(var f in flashes)if(f!=null && f.Root!=null && f.Root.gameObject.activeSelf)count++;return count;}}
        public void Clear(){for(int i=0;i<flashes.Length;i++){if(flashes[i]!=null && flashes[i].Root!=null)flashes[i].Root.gameObject.SetActive(false);flashes[i]=null;}next=0;}
        public static MeshRenderer Glow(Transform parent,string name,float size,Color color)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*size;go.GetComponent<MeshFilter>().sharedMesh=Quad;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("FleetParticle");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var block=new MaterialPropertyBlock();block.SetColor("_Color",color);renderer.SetPropertyBlock(block);return renderer;
        }
        Flash Create(int index)
        {
            var root=new GameObject("Pooled battle flash").transform;game.AddCombatEffect(root.gameObject);
            var f=new Flash{Root=root};f.Halo=Glow(root,"Colored flare",1,Color.white);f.Core=Glow(root,"White hot core",.3f,Color.white);
            if(index<8) {f.Light=root.gameObject.AddComponent<Light>();f.Light.type=LightType.Point;f.Light.range=10;f.Light.shadows=LightShadows.None;f.Light.renderMode=LightRenderMode.ForceVertex;}
            var sparks=new GameObject("Impact sparks",typeof(ParticleSystem));sparks.transform.SetParent(root,false);f.Sparks=sparks.GetComponent<ParticleSystem>();var main=f.Sparks.main;main.loop=false;main.playOnAwake=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.12f,.35f);main.startSpeed=new ParticleSystem.MinMaxCurve(2,6);main.startSize=.11f;main.startColor=new Color(1,.6f,.15f);main.maxParticles=12;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=f.Sparks.emission;emission.rateOverTime=0;var shape=f.Sparks.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
            f.Sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("FleetParticle");f.Sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);root.gameObject.SetActive(false);return f;
        }
        public void Pulse(Vector3 position,Color color,float size,float duration,bool sparks=false)
        {
            int i=next++%flashes.Length;var f=flashes[i];if(f==null || f.Root==null)f=flashes[i]=Create(i);
            f.Root.gameObject.SetActive(true);f.Root.position=position;f.Color=color;f.Age=0;f.Duration=duration;f.Size=size;
            f.Sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(sparks){f.Sparks.Play();f.Sparks.Emit(8);}Draw(f);
        }
        public void Tick(float dt)
        {if(game.Paused)return;foreach(var f in flashes)if(f!=null && f.Root!=null && f.Root.gameObject.activeSelf){f.Age+=dt;if(f.Age>f.Duration){f.Root.gameObject.SetActive(false);continue;}Draw(f);}}
        void Draw(Flash f)
        {
            float fade=1-Mathf.Clamp01(f.Age/f.Duration);f.Halo.transform.rotation=f.Core.transform.rotation=game.ViewCamera.transform.rotation;
            f.Halo.transform.localScale=Vector3.one*f.Size*(1+(1-fade)*.6f);f.Core.transform.localScale=Vector3.one*f.Size*.28f;
            var color=f.Color;color.a=fade*.85f;f.Block.SetColor("_Color",color);f.Halo.SetPropertyBlock(f.Block);f.Block.SetColor("_Color",new Color(1,.96f,.85f,fade));f.Core.SetPropertyBlock(f.Block);
            if(f.Light!=null){f.Light.color=f.Color;f.Light.intensity=fade*2.5f;}
        }
    }

    public sealed class FleetEngineEffects
    {
        readonly FleetShip ship;
        readonly System.Collections.Generic.List<Jet> jets=new System.Collections.Generic.List<Jet>();
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        float previousYaw,bank;
        sealed class Jet {public Transform Anchor;public MeshRenderer Halo,Core;public LineRenderer Wake;public float Size;}
        public float Bank => bank;
        public int JetCount => jets.Count;
        public FleetEngineEffects(FleetShip owner)
        {
            ship=owner;previousYaw=ship.transform.eulerAngles.y;
            if(ship.Squadron!=null)for(int i=0;i<6;i++)Build(ship.VisualRoot.GetChild(i));
        }
        void Build(Transform body)
        {
            var filters=body.GetComponentsInChildren<MeshFilter>();Bounds bounds=new Bounds();bool first=true;
            foreach(var filter in filters)if(filter.sharedMesh!=null)
            {
                var b=filter.sharedMesh.bounds;var matrix=body.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            }
            if(first)return;
            int count=ship.Faction==Faction.CIS && ship.Kind==ShipClass.Interceptor?3:ship.Squadron!=null?2:3;
            // Keep fighter group children unchanged; attach effects beneath each craft prefab.
            var parent=ship.Squadron!=null?body.GetChild(0):body;
            var engineRoot=new GameObject("Engine effects").transform;engineRoot.SetParent(parent,false);
            for(int i=0;i<count;i++)
            {
                float x=(i-(count-1)*.5f)*bounds.size.x*.27f;
                var anchor=new GameObject("Ion engine").transform;anchor.SetParent(engineRoot,false);
                anchor.position=body.TransformPoint(new Vector3(bounds.center.x+x,bounds.center.y,bounds.min.z+.08f));
                float size=ship.Squadron!=null?.55f:1.1f;
                var jet=new Jet{Anchor=anchor,Size=size};jet.Halo=FleetBattleEffects.Glow(anchor,"Engine halo",size,new Color(.12f,.5f,1,.75f));jet.Core=FleetBattleEffects.Glow(anchor,"Engine core",size*.3f,new Color(.6f,.85f,1));
                var line=new GameObject("Engine wake",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(anchor,false);line.useWorldSpace=true;line.positionCount=2;line.sharedMaterial=Resources.Load<Material>("FleetParticle");line.numCapVertices=3;line.startColor=new Color(.12f,.55f,1,.5f);line.endColor=new Color(.05f,.25f,1,0);jet.Wake=line;jets.Add(jet);
            }
        }
        public void Tick(float dt)
        {
            bool visible=ship.Targetable;float power=Mathf.Clamp01(ship.CurrentSpeed/ship.Stats.Speed);
            float yaw=ship.transform.eulerAngles.y;float turn=dt>0?Mathf.DeltaAngle(previousYaw,yaw)/dt:0;previousYaw=yaw;
            bank=Mathf.MoveTowards(bank,visible && ship.Squadron!=null?Mathf.Clamp(-turn*.14f,-22,22):0,dt*70);
            if(ship.Squadron!=null && !ship.IsArriving)ship.VisualRoot.localRotation=Quaternion.Euler(0,0,bank);
            foreach(var jet in jets)
            {
                jet.Halo.enabled=jet.Core.enabled=jet.Wake.enabled=visible;
                if(!visible)continue;
                jet.Halo.transform.rotation=jet.Core.transform.rotation=ship.Game.ViewCamera.transform.rotation;
                float scale=jet.Anchor.lossyScale.x;
                block.SetColor("_Color",new Color(.12f,.55f,1,.3f+power*.4f));jet.Halo.SetPropertyBlock(block);
                jet.Halo.transform.localScale=Vector3.one*jet.Size*(1+power*.7f);
                jet.Wake.startWidth=jet.Size*scale*.3f;jet.Wake.endWidth=0;
                jet.Wake.SetPosition(0,jet.Anchor.position);jet.Wake.SetPosition(1,jet.Anchor.position-jet.Anchor.forward*(ship.Squadron!=null?.3f+power*2.2f:.4f+power*2.8f)*scale);
            }
        }
        public void Shutdown(){foreach(var jet in jets)if(jet.Anchor!=null)jet.Anchor.parent.gameObject.SetActive(false);}
    }
}
