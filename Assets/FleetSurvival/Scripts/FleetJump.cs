using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetJump
    {
        public readonly ShipClass Kind;
        public readonly Vector3 Destination;
        public FleetShip Ship { get; private set; }
        public float Elapsed { get; private set; }
        public Vector3 ReservedPosition => Ship!=null?landing:Destination;
        readonly FleetGame game;
        readonly LineRenderer beacon;
        LineRenderer wake;
        Vector3 landing,start,visualScale;
        public FleetJump(FleetGame owner,ShipClass kind,Vector3 destination)
        {
            game=owner; Kind=kind; Destination=landing=destination;
            beacon=ShipVisuals.Ring(null,FleetRules.Stats(game.PlayerFaction,kind).Radius+1,new Color(.12f,.7f,1),.12f,"Hyperspace arrival beacon");
            beacon.transform.position=destination; game.AddCombatEffect(beacon.gameObject);
            game.PlayBattleSound(FleetSound.Get("HyperspaceCharge"),destination,.16f);
        }
        public bool Tick(float dt)
        {
            Elapsed+=dt;
            if(beacon!=null) beacon.transform.localScale=Vector3.one*(1+Mathf.Sin(Elapsed*7)*.1f);
            if(Elapsed<2.2f) return true;
            if(Ship==null)
            {
                if(!game.ResolveArrivalPosition(Kind,Destination,this,out landing))
                { game.RefundCallIn(Kind); Cleanup(); return false; }
                start=landing-Vector3.forward*100;
                Ship=game.Spawn(game.PlayerFaction,Kind,true,false,start); Ship.IsArriving=true;
                visualScale=Ship.VisualRoot.localScale;
                Ship.GetComponent<Collider>().enabled=false;
                game.PlayBattleSound(FleetSound.Get("HyperspaceExit"),landing,.23f);
                wake=new GameObject("Hyperspace wake",typeof(LineRenderer)).GetComponent<LineRenderer>();
                game.AddCombatEffect(wake.gameObject); wake.sharedMaterial=ShipVisuals.Material(new Color(.25f,.75f,1),true);
                wake.positionCount=2; wake.startWidth=.65f; wake.endWidth=.03f;
                wake.SetPositions(new[]{landing+Vector3.up*.5f,start+Vector3.up*.5f});
            }
            float u=Mathf.Clamp01((Elapsed-2.2f)/.65f);
            Ship.transform.position=Vector3.Lerp(start,landing,1-Mathf.Pow(1-u,5));
            Ship.VisualRoot.localScale=Vector3.Scale(visualScale,new Vector3(1,1,Mathf.Lerp(7,1,1-Mathf.Pow(1-u,2))));
            if(wake!=null) wake.startWidth=Mathf.Lerp(.65f,.02f,u);
            if(u<1) return true;
            Ship.transform.position=landing; Ship.VisualRoot.localScale=visualScale;
            Ship.IsArriving=false; Ship.GetComponent<Collider>().enabled=true;
            game.Notify("Hyperspace arrival complete: "+Ship.Stats.Name+".");
            Cleanup(); return false;
        }
        public void Cancel()
        { if(Ship!=null && Ship.IsArriving) { game.Ships.Remove(Ship); Object.Destroy(Ship.gameObject); } Cleanup(); }
        void Cleanup() { if(beacon!=null) Object.Destroy(beacon.gameObject); if(wake!=null) Object.Destroy(wake.gameObject); }
    }
}
