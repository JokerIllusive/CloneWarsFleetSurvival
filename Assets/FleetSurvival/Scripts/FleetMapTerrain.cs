using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetMapTerrain : UnityEngine.UI.MaskableGraphic
    {
        FleetGame game;
        FleetEnvironment environment;
        Vector2 extent;
        float refresh;
        public int AsteroidCount => environment.MapAsteroids.Count;
        public int LandmarkCount => environment.MapLandmarks.Count;
        public void Initialize(FleetGame owner,FleetEnvironment source,Vector2 radius)
        {game=owner;environment=source;extent=radius;raycastTarget=false;SetVerticesDirty();}
        void Update(){refresh-=Time.unscaledDeltaTime;if(refresh<=0){SetVerticesDirty();refresh=.25f;}}
        Vector2 Point(Vector3 world) => new Vector2(world.x/FleetRules.ArenaRadius*extent.x,world.z/FleetRules.ArenaRadius*extent.y);
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(game==null || environment==null)return;
            // Geonosis is outside the command sector. Quiet orbit contours provide regional context.
            var center=Point(environment.PlanetPosition);
            foreach(float radius in new[]{66f,120f,155f,190f})Ring(vh,center,extent*(radius/FleetRules.ArenaRadius),radius==66?new Color(.5f,.28f,.11f,.4f):new Color(.32f,.22f,.14f,.28f),.7f);
            Ring(vh,Vector2.zero,extent*.48f,new Color(.15f,.25f,.29f,.25f),.7f);
            foreach(var rock in environment.MapAsteroids)
            {
                if(rock==null)continue;var p=Point(rock.position);float size=Mathf.Clamp(rock.lossyScale.x*.85f,1.2f,4.3f);
                Quad(vh,p,new Vector2(size,size*.7f),new Color(.46f,.37f,.25f,.5f));
            }
            foreach(var yard in environment.MapLandmarks)
            {
                var p=Point(yard.position);var tint=new Color(.56f,.49f,.31f,.8f);float scale=yard.lossyScale.x;
                Line(vh,p-new Vector2(0,13)*scale,p+new Vector2(0,13)*scale,1.4f,tint);
                for(int bay=-2;bay<=2;bay++)Line(vh,p+new Vector2(-11,bay*5)*scale,p+new Vector2(11,bay*5)*scale,1.2f,tint);
            }
            foreach(var wreck in game.ActiveWrecks)
                if(wreck!=null && wreck.Meltdown && !wreck.Complete)Ring(vh,Point(wreck.transform.position),extent*(wreck.BlastRadius/FleetRules.ArenaRadius),new Color(1,.44f,.1f,.8f),1.5f);
        }
        static void Ring(UnityEngine.UI.VertexHelper vh,Vector2 center,Vector2 radius,Color tint,float width)
        {for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Line(vh,center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),center+new Vector2(Mathf.Cos(b)*radius.x,Mathf.Sin(b)*radius.y),width,tint);}}
        static void Quad(UnityEngine.UI.VertexHelper vh,Vector2 center,Vector2 size,Color tint)
        {
            int start=vh.currentVertCount;var half=size*.5f;
            vh.AddVert(center+new Vector2(-half.x,-half.y),tint,Vector2.zero);vh.AddVert(center+new Vector2(-half.x,half.y),tint,Vector2.zero);vh.AddVert(center+half,tint,Vector2.zero);vh.AddVert(center+new Vector2(half.x,-half.y),tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        static void Line(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            int start=vh.currentVertCount;var delta=b-a;var offset=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            vh.AddVert(a-offset,tint,Vector2.zero);vh.AddVert(a+offset,tint,Vector2.zero);vh.AddVert(b+offset,tint,Vector2.zero);vh.AddVert(b-offset,tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
