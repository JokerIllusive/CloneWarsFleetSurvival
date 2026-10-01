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
        float holdingTime,holdingBlend;
        public int ActiveCount { get; private set; }
        public bool IsActive(int index) => index>=0 && index<6 && hull[index]>0;
        public Vector3 Muzzle(int index,Vector3 local) => fighters[index].TransformPoint(local);
        public FleetSquadron(FleetShip owner)
        {
            ship=owner;
            for(int i=0;i<6;i++) fighters[i]=ship.VisualRoot.GetChild(i);
            Repair();
        }
        public void Tick(float dt,bool holding)
        {
            holdingBlend=Mathf.MoveTowards(holdingBlend,holding?1:0,dt*1.5f);
            if(holding) holdingTime+=dt*.65f;
            for(int i=0;i<6;i++) if(IsActive(i))
            {
                float t=holdingTime+i*.22f;
                Vector3 loop=new Vector3(Mathf.Sin(t)*.65f,Mathf.Sin(t*1.3f)*.12f,Mathf.Cos(t)*.85f);
                fighters[i].localPosition=Offsets[i]+loop*holdingBlend;
                var turn=Quaternion.LookRotation(new Vector3(Mathf.Cos(t)*.65f,0,-Mathf.Sin(t)*.85f))*Quaternion.Euler(0,0,-8);
                var desired=Quaternion.Slerp(Quaternion.identity,turn,holdingBlend);
                fighters[i].localRotation=Quaternion.RotateTowards(fighters[i].localRotation,desired,dt*110);
            }
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
                    ship.Game.FighterLost(ship,fighters[closest].position);
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
