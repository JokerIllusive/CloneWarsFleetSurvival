using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetSquadron
    {
        public const float CraftScale=.52f;
        public static readonly Vector3[] Offsets={new Vector3(-2.2f,0,3),new Vector3(2.2f,0,3),new Vector3(-4.4f,0,0),new Vector3(4.4f,0,0),new Vector3(-2.2f,0,-3),new Vector3(2.2f,0,-3)};
        readonly FleetShip ship;
        readonly Transform[] fighters=new Transform[6];
        readonly float[] hull=new float[6];
        public int ActiveCount { get; private set; }
        public bool IsActive(int index) => index>=0 && index<6 && hull[index]>0;
        public FleetSquadron(FleetShip owner)
        {
            ship=owner;
            for(int i=0;i<6;i++) fighters[i]=ship.VisualRoot.GetChild(i);
            Repair();
        }
        public void HullHit(float damage,Vector3 impact)
        {
            while(damage>.0001f && ActiveCount>0)
            {
                int closest=-1; float distance=float.MaxValue;
                for(int i=0;i<6;i++) if(IsActive(i))
                {
                    float d=(fighters[i].position-impact).sqrMagnitude;
                    if(d<distance) { distance=d; closest=i; }
                }
                float hit=Mathf.Min(damage,hull[closest]); hull[closest]-=hit; damage-=hit;
                if(hull[closest]<=.0001f)
                {
                    hull[closest]=0; ActiveCount--;
                    ship.Destruction.BreakModel(fighters[closest]);
                    ship.Game.FighterLost(fighters[closest].position);
                    fighters[closest].gameObject.SetActive(false);
                }
            }
        }
        public void Repair()
        {
            ActiveCount=6;
            for(int i=0;i<6;i++) { hull[i]=ship.MaxHull/6; fighters[i].gameObject.SetActive(true); }
        }
    }
}
