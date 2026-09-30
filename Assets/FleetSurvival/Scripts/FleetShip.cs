using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetShip : MonoBehaviour
    {
        public FleetGame Game;
        public Faction Faction;
        public ShipClass Kind;
        public ShipStats Stats;
        public bool Friendly, Selected, IsFlagship, HasMoveOrder, AttackMove;
        public float Hull, Shield, MaxHull, MaxShield;
        public Vector3 Destination;
        public FleetShip ForcedTarget;
        public LineRenderer SelectionRing;
        public ShipDestruction Destruction { get; private set; }
        float cooldown, lastHit = -100;
        public bool Alive => Hull > 0;

        public void Configure(FleetGame game, Faction faction, ShipClass kind, bool friendly, bool flagship, float multiplier)
        {
            Game=game; Faction=faction; Kind=kind; Friendly=friendly; IsFlagship=flagship;
            Stats=FleetRules.Stats(faction,kind);
            MaxHull=Stats.Hull*multiplier; MaxShield=Stats.Shield*multiplier;
            Hull=MaxHull; Shield=MaxShield; cooldown=Random.Range(.1f,.8f);
            ShipVisuals.Build(transform,faction,kind);
            Destruction=gameObject.AddComponent<ShipDestruction>(); Destruction.Initialize(this);
            var collider=gameObject.AddComponent<SphereCollider>(); collider.radius=Stats.Radius;
            SelectionRing=ShipVisuals.Ring(transform,Stats.Radius+1,FleetRules.Color(faction),.1f);
            SelectionRing.enabled=false;
            // A small team beacon keeps same-faction friendlies and enemies distinguishable.
            var beacon=ShipVisuals.Ring(transform,Stats.Radius*.72f,friendly?new Color(.2f,.85f,1,.65f):new Color(1,.25f,.18f,.65f),.055f,"Team beacon");
            beacon.transform.localPosition=new Vector3(0,-.2f,0);
        }

        public void Tick(float dt)
        {
            if(!Alive) return;
            SelectionRing.enabled=Selected;
            if(Game.BattleTime-lastHit>7) Shield=Mathf.Min(MaxShield,Shield+MaxShield*.025f*dt);
            cooldown-=dt;
            FleetShip target=ForcedTarget;
            if(target == null || !target.Alive || target.Friendly==Friendly) { ForcedTarget=null; target=Game.NearestEnemy(this); }
            float distance=target!=null ? Vector3.Distance(transform.position,target.transform.position) : float.MaxValue;
            bool inRange=distance<=Stats.Range;
            Vector3 desired=transform.position;
            bool move=false;
            if(HasMoveOrder && !(AttackMove && inRange))
            {
                desired=Destination; move=true;
                if(Vector3.Distance(desired,transform.position)<1.2f) HasMoveOrder=false;
            }
            else if(target!=null)
            {
                desired=target.transform.position;
                if(!inRange && (!Friendly || ForcedTarget!=null || distance<Stats.Range+16)) move=true;
                if((Kind==ShipClass.Fighter || Kind==ShipClass.Interceptor) && inRange)
                {
                    Vector3 radial=(transform.position-target.transform.position).normalized;
                    desired=target.transform.position+radial*Stats.Range*.62f+Vector3.Cross(Vector3.up,radial)*4;
                    move=true;
                }
            }
            if(move)
            {
                Vector3 direction=(desired-transform.position).normalized;
                Vector3 separation=Vector3.zero;
                foreach(var other in Game.Ships)
                {
                    if(other==null || other==this || !other.Alive) continue;
                    Vector3 delta=transform.position-other.transform.position;
                    float gap=Stats.Radius+other.Stats.Radius+.8f;
                    if(delta.sqrMagnitude<gap*gap && delta.sqrMagnitude>.001f)
                        separation+=delta.normalized*(1-delta.magnitude/gap)*2.2f;
                }
                Vector3 step=(direction+separation).normalized*Stats.Speed*Destruction.Mobility*dt;
                if((desired-transform.position).sqrMagnitude>step.sqrMagnitude) transform.position+=step;
                Vector3 clamped=transform.position; clamped.y=0;
                if(clamped.magnitude>FleetRules.ArenaRadius) clamped=clamped.normalized*FleetRules.ArenaRadius;
                transform.position=clamped;
                if(step.sqrMagnitude>.00001f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(step),dt*4);
            }
            else if(target!=null)
            {
                Vector3 heading=target.transform.position-transform.position;
                if(heading.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(heading),dt*2);
            }
            if(inRange && cooldown<=0 && Game.Phase==BattlePhase.Combat)
            {
                Game.Fire(this,target,Stats.Damage*Destruction.Firepower); cooldown=Stats.Interval;
            }
        }

        public void Damage(float amount) => Damage(amount,transform.position+transform.forward*Stats.Radius);
        public void Damage(float amount,Vector3 impact)
        {
            if(!Alive) return;
            lastHit=Game.BattleTime;
            float absorbed=Mathf.Min(Shield,amount); Shield-=absorbed; Hull-=amount-absorbed;
            if(amount>absorbed) Destruction.HullHit(impact);
            if(Hull<=0) { Hull=0; Game.ShipDestroyed(this); }
        }

        public void MoveTo(Vector3 destination, bool attackMove=false)
        { Destination=destination; HasMoveOrder=true; AttackMove=attackMove; ForcedTarget=null; }
    }

    public sealed class FleetBolt : MonoBehaviour
    {
        public FleetGame Game;
        public FleetShip Target;
        public float Damage;
        float age;
        public void Tick(float dt)
        {
            age+=dt;
            if(Target==null || !Target.Alive || age>4) { Game.RemoveBolt(this); return; }
            Vector3 direction=Target.transform.position+Vector3.up*.7f-transform.position;
            float travel=95*dt;
            if(direction.magnitude<=travel+Target.Stats.Radius*.35f)
            {
                Target.Damage(Damage,transform.position); Game.RemoveBolt(this); return;
            }
            transform.rotation=Quaternion.LookRotation(direction);
            transform.position+=direction.normalized*travel;
        }
    }
}
