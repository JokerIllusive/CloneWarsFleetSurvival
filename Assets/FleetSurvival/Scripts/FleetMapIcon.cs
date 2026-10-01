using UnityEngine;

namespace FleetSurvival
{
    public enum FleetMapShape { Capital, Carrier, Command, Fighter, Interceptor, Bomber, Destination, Incoming, Reactor }

    // Small vector symbols stay sharp at every Canvas scale and do not intercept map clicks.
    public sealed class FleetMapIcon : UnityEngine.UI.MaskableGraphic
    {
        public FleetMapShape Shape { get; private set; }
        public bool Selected { get; private set; }
        public void Configure(FleetMapShape shape,bool selected,Color tint)
        {
            raycastTarget=false;
            if(Shape!=shape || Selected!=selected){Shape=shape;Selected=selected;SetVerticesDirty();}
            color=tint;
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();float size=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
            if(Shape==FleetMapShape.Destination)
            {
                Strip(vh,new Vector2(-.4f,0),new Vector2(.4f,0),.10f,size,color);
                Strip(vh,new Vector2(0,-.4f),new Vector2(0,.4f),.10f,size,color);
                return;
            }
            if(Shape==FleetMapShape.Incoming)
            {
                for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f,b=a+.15f;Strip(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.45f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.45f,.08f,size,color);}
                Strip(vh,new Vector2(-.2f,0),new Vector2(.2f,0),.08f,size,color);Strip(vh,new Vector2(0,-.2f),new Vector2(0,.2f),.08f,size,color);return;
            }
            if(Shape==FleetMapShape.Reactor)
            {
                Polygon(vh,size,color,new[]{new Vector2(0,.5f),new Vector2(.46f,-.4f),new Vector2(-.46f,-.4f)});
                var dark=new Color(.05f,.03f,.01f,color.a);Strip(vh,new Vector2(0,.22f),new Vector2(0,-.06f),.12f,size,dark);Strip(vh,new Vector2(0,-.19f),new Vector2(0,-.25f),.12f,size,dark);return;
            }
            if(Shape==FleetMapShape.Fighter)Polygon(vh,size,color,new[]{new Vector2(0,.48f),new Vector2(.4f,-.35f),new Vector2(-.4f,-.35f)});
            else if(Shape==FleetMapShape.Interceptor)
            {
                Polygon(vh,size,color,new[]{new Vector2(0,.48f),new Vector2(.4f,-.02f),new Vector2(0,.15f),new Vector2(-.4f,-.02f)});
                Polygon(vh,size,color,new[]{new Vector2(0,.05f),new Vector2(.4f,-.45f),new Vector2(0,-.28f),new Vector2(-.4f,-.45f)});
            }
            else if(Shape==FleetMapShape.Bomber)
            {
                Polygon(vh,size,color,new[]{new Vector2(0,.48f),new Vector2(.43f,0),new Vector2(0,-.48f),new Vector2(-.43f,0)});
                Strip(vh,new Vector2(-.24f,0),new Vector2(.24f,0),.13f,size,new Color(.02f,.05f,.08f,color.a));
            }
            else if(Shape==FleetMapShape.Command)
                Polygon(vh,size,color,new[]{new Vector2(0,.5f),new Vector2(.16f,.16f),new Vector2(.5f,0),new Vector2(.16f,-.16f),new Vector2(0,-.5f),new Vector2(-.16f,-.16f),new Vector2(-.5f,0),new Vector2(-.16f,.16f)});
            else if(Shape==FleetMapShape.Carrier)
            {
                Polygon(vh,size,color,new[]{new Vector2(0,.5f),new Vector2(.45f,.25f),new Vector2(.45f,-.3f),new Vector2(0,-.48f),new Vector2(-.45f,-.3f),new Vector2(-.45f,.25f)});
                Strip(vh,new Vector2(-.27f,.1f),new Vector2(.27f,.1f),.10f,size,new Color(.02f,.05f,.08f,color.a));
                Strip(vh,new Vector2(-.27f,-.12f),new Vector2(.27f,-.12f),.10f,size,new Color(.02f,.05f,.08f,color.a));
            }
            else Polygon(vh,size,color,new[]{new Vector2(0,.5f),new Vector2(.34f,.1f),new Vector2(.26f,-.45f),new Vector2(-.26f,-.45f),new Vector2(-.34f,.1f)});
            if(Selected)
            {
                var gold=new Color(1,.85f,.25f,1);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
                {
                    var corner=new Vector2(x*.66f,y*.66f);
                    Strip(vh,corner,corner-new Vector2(x*.24f,0),.09f,size,gold);
                    Strip(vh,corner,corner-new Vector2(0,y*.24f),.09f,size,gold);
                }
            }
        }
        static void Polygon(UnityEngine.UI.VertexHelper vh,float size,Color tint,Vector2[] vertices)
        {
            int start=vh.currentVertCount;vh.AddVert(Vector3.zero,tint,Vector2.zero);
            foreach(var vertex in vertices)vh.AddVert(vertex*size,tint,Vector2.zero);
            for(int i=0;i<vertices.Length;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%vertices.Length+1);
        }
        static void Strip(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,float width,float size,Color tint)
        {
            var delta=b-a;var offset=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            int start=vh.currentVertCount;vh.AddVert((a-offset)*size,tint,Vector2.zero);vh.AddVert((a+offset)*size,tint,Vector2.zero);vh.AddVert((b+offset)*size,tint,Vector2.zero);vh.AddVert((b-offset)*size,tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
