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
        public FleetFormation Formation { get; set; }
        public float CurrentSpeed { get; private set; }
        public bool IsArriving { get; set; }
        public FleetWeapons Weapons { get; private set; }
        public Transform VisualRoot { get; private set; }
        public FleetSquadron Squadron { get; private set; }
        public SquadronRole Role { get; private set; }
        public FleetHangar Hangar { get; private set; }
        public FleetEngineEffects EngineEffects { get; private set; }
        public FleetHangar HomeHangar { get; set; }
        public bool Docked { get; set; }
        public bool ReturningToHangar { get; set; }
        public float DockTimer { get; set; }
        public float ServiceTimer { get; set; }
        float hangarLaunch;
        LineRenderer teamBeacon;
        public bool Targetable => Alive && !IsArriving && !Docked;
        public float BaseDamage => Squadron!=null?FleetRules.SquadronStats(Faction,Kind,Role).Damage:FleetRules.Stats(Faction,Kind).Damage;
        public FleetMovePreview MovePreview { get; private set; }
        public int WeaponRefits { get; set; }
        public float RefitBonus => Stats.Damage/BaseDamage-1;
        public float EffectiveVolleyDamage => Stats.Damage*Destruction.Firepower*(Squadron!=null?Squadron.ActiveCount/6f:1);
        public float EffectiveDPS => EffectiveVolleyDamage/Stats.Interval;
        float cooldown, lastHit = -100;
        public bool Alive => Hull > 0;

        public void Configure(FleetGame game, Faction faction, ShipClass kind, bool friendly, bool flagship, float multiplier,SquadronRole? role=null)
        {
            Game=game; Faction=faction; Kind=kind; Friendly=friendly; IsFlagship=flagship;
            Role=role??(kind==ShipClass.Interceptor?SquadronRole.Interceptor:SquadronRole.Fighter);
            Stats=FleetRules.SquadronStats(faction,kind,Role);
            MaxHull=Stats.Hull*multiplier; MaxShield=Stats.Shield*multiplier;
            Hull=MaxHull; Shield=MaxShield; cooldown=Random.Range(.1f,.8f);
            ShipVisuals.Build(transform,faction,kind,Role);
            VisualRoot=transform.GetChild(0);
            Destruction=gameObject.AddComponent<ShipDestruction>(); Destruction.Initialize(this);
            if(kind==ShipClass.Fighter || kind==ShipClass.Interceptor) Squadron=new FleetSquadron(this);
            if(kind==ShipClass.Carrier || kind==ShipClass.Flagship) Hangar=new FleetHangar(this);
            Weapons=gameObject.AddComponent<FleetWeapons>(); Weapons.Initialize(this);
            EngineEffects=new FleetEngineEffects(this);
            if(friendly) { MovePreview=gameObject.AddComponent<FleetMovePreview>();MovePreview.Initialize(this); }
            var collider=gameObject.AddComponent<SphereCollider>(); collider.radius=Stats.Radius;
            SelectionRing=ShipVisuals.Ring(transform,Stats.Radius+1,FleetRules.Color(faction),.1f);
            SelectionRing.enabled=false;
            // A small team beacon keeps same-faction friendlies and enemies distinguishable.
            var beacon=ShipVisuals.Ring(transform,Stats.Radius*.72f,friendly?new Color(.2f,.85f,1,.65f):new Color(1,.25f,.18f,.65f),.055f,"Team beacon");
            teamBeacon=beacon;
            beacon.transform.localPosition=new Vector3(0,-.2f,0);
        }

        public void Tick(float dt)
        {
            if(!Alive) return;
            EngineEffects.Tick(dt);
            if(Hangar!=null) Hangar.Tick(dt);
            if(Docked) return;
            if(hangarLaunch>0)
            {
                hangarLaunch=Mathf.Max(0,hangarLaunch-dt);float t=1-hangarLaunch/2;
                if(HomeHangar!=null && HomeHangar.Owner.Alive)
                {var owner=HomeHangar.Owner;transform.position=Vector3.Lerp(HomeHangar.Port,HomeHangar.LaunchPoint(this),Mathf.SmoothStep(0,1,t));transform.rotation=owner.transform.rotation;}
                VisualRoot.localScale=Vector3.one*Mathf.Lerp(.08f,1,t);
                if(hangarLaunch<=0) FinishHangarLaunch();return;
            }
            if(IsArriving) return;
            if(ReturningToHangar && HomeHangar!=null)
            {
                FlyTowards(HomeHangar.DockPoint,true,dt);
                if(Vector3.Distance(transform.position,HomeHangar.DockPoint)<2.5f)
                {ReturningToHangar=false;Docked=true;Selected=false;DockTimer=ServiceTimer=0;CurrentSpeed=0;GetComponent<Collider>().enabled=false;VisualRoot.gameObject.SetActive(false);SelectionRing.enabled=false;teamBeacon.enabled=false;transform.position=HomeHangar.Port;}
                return;
            }
            Weapons.Tick(dt);
            SelectionRing.enabled=Selected;
            if(Game.BattleTime-lastHit>7) Shield=Mathf.Min(MaxShield,Shield+MaxShield*.025f*dt);
            cooldown-=dt;
            FleetShip target=ForcedTarget;
            if(target == null || !target.Targetable || target.Friendly==Friendly) { ForcedTarget=null; target=Game.NearestEnemy(this); }
            float distance=target!=null ? Vector3.Distance(transform.position,target.transform.position) : float.MaxValue;
            bool inRange=distance<=Stats.Range;
            Vector3 desired=transform.position;
            bool move=false;
            if(!HasMoveOrder || ForcedTarget!=null) Formation=null;
            if(HasMoveOrder && !(AttackMove && inRange))
            {
                desired=Formation!=null?Formation.Slot(this):Destination; move=true;
                if((Formation==null || Formation.Arrived) && Vector3.Distance(desired,transform.position)<.9f && CurrentSpeed<.25f)
                { HasMoveOrder=false; Formation=null; move=false; }
            }
            else if(target!=null)
            {
                desired=target.transform.position;
                if(!inRange && (!Friendly || ForcedTarget!=null || distance<Stats.Range+16)) move=true;
                if((Kind==ShipClass.Fighter || Kind==ShipClass.Interceptor) && inRange)
                {
                    Vector3 radial=transform.position-target.transform.position;radial.y=0;if(radial.sqrMagnitude<.01f)radial=-target.transform.forward;radial.Normalize();
                    float clearance=Mathf.Min(Stats.Range*.9f,target.Stats.Radius+Stats.Radius+1.5f);
                    desired=target.transform.position+radial*clearance+Vector3.Cross(Vector3.up,radial)*7;
                    move=true;
                }
            }
            if(FleetTactics.BattlePosition(this,target,out var tacticalPoint))
            {desired=Vector3.ClampMagnitude(tacticalPoint,FleetRules.ArenaRadius-Stats.Radius-1);move=Vector3.Distance(desired,transform.position)>1.2f;}
            FlyTowards(desired,move,dt);
            if(Squadron!=null) Squadron.Tick(dt,!move && !inRange && !HasMoveOrder && CurrentSpeed<.4f);
            if(inRange && cooldown<=0 && Game.Phase==BattlePhase.Combat)
            {
                Game.Fire(this,target,EffectiveVolleyDamage*FleetTactics.DamageModifier(this,target)); cooldown=Stats.Interval;
            }
        }

        void FlyTowards(Vector3 destination,bool move,float dt)
        {
            var handling=FleetRules.Handling(Kind);
            Vector3 delta=destination-transform.position; delta.y=0;
            float distance=delta.magnitude;
            float desiredSpeed=0;
            if(move && distance>.15f)
            {
                Vector3 direction=delta/distance;
                Vector3 separation=Vector3.zero;
                foreach(var other in Game.Ships)
                {
                    if(other==null || other==this || !other.Targetable) continue;
                    Vector3 away=transform.position-other.transform.position;
                    float gap=Stats.Radius+other.Stats.Radius+.8f;
                    float squared=away.sqrMagnitude;
                    if(squared<gap*gap && squared>.001f)
                        separation+=away.normalized*(1-Mathf.Sqrt(squared)/gap)*1.8f;
                }
                Vector3 heading=(direction+separation).normalized;
                if(heading.sqrMagnitude>.001f)
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),handling.TurnRate*dt);
                float alignment=Mathf.Clamp01((Vector3.Dot(transform.forward,heading)+.3f)/1.3f);
                float maximum=Stats.Speed*Destruction.Mobility;
                desiredSpeed=Mathf.Min(maximum,Mathf.Sqrt(2*handling.Braking*Mathf.Max(0,distance-.65f)))*alignment;
            }
            float change=desiredSpeed<CurrentSpeed?handling.Braking:handling.Acceleration*Destruction.Mobility;
            CurrentSpeed=Mathf.MoveTowards(CurrentSpeed,desiredSpeed,change*dt);
            Vector3 position=transform.position+transform.forward*CurrentSpeed*dt; position.y=0;
            float boundary=FleetRules.ArenaRadius-Stats.Radius;
            if(position.magnitude>boundary) { position=position.normalized*boundary; CurrentSpeed=0; }
            transform.position=position;
        }

        public void Damage(float amount) => Damage(amount,transform.position+transform.forward*Stats.Radius);
        public void Damage(float amount,Vector3 impact)
        {
            if(!Targetable) return;
            lastHit=Game.BattleTime;
            float absorbed=Mathf.Min(Shield,amount); Shield-=absorbed; Hull-=amount-absorbed;
            if(amount>absorbed) { if(Squadron!=null) Squadron.HullHit(amount-absorbed,impact); else Destruction.HullHit(impact,amount-absorbed); }
            if(Hull<=0) { Hull=0; Game.ShipDestroyed(this); }
        }

        public void MoveTo(Vector3 destination, bool attackMove=false)
        { if(Docked || IsArriving) return;ReturningToHangar=false;destination.y=0; Destination=Vector3.ClampMagnitude(destination,FleetRules.ArenaRadius-Stats.Radius-1); HasMoveOrder=true; AttackMove=attackMove; ForcedTarget=null; Formation=null; }
        public void AttackTarget(FleetShip target)
        { if(Docked || IsArriving) return;ReturningToHangar=false;ForcedTarget=target; HasMoveOrder=false; AttackMove=false; Formation=null; }
        public void StartHangarLaunch()
        {Docked=false;ReturningToHangar=false;DockTimer=ServiceTimer=0;HasMoveOrder=false;ForcedTarget=null;Formation=null;Selected=false;IsArriving=true;hangarLaunch=2;VisualRoot.gameObject.SetActive(true);teamBeacon.enabled=false;VisualRoot.localScale=Vector3.one*.08f;GetComponent<Collider>().enabled=false;Weapons.Clear();}
        public void FinishHangarLaunch()
        {hangarLaunch=0;IsArriving=false;VisualRoot.localScale=Vector3.one;teamBeacon.enabled=true;GetComponent<Collider>().enabled=true;var p=transform.position;p.y=0;transform.position=p;Destination=p;}
    }

    public sealed class FleetBolt : MonoBehaviour
    {
        public FleetGame Game;
        public FleetShip Target;
        public float Damage;
        public bool Torpedo;
        float age;
        public void Tick(float dt)
        {
            age+=dt;
            if(Target==null || !Target.Targetable || age>4) { Game.RemoveBolt(this); return; }
            Vector3 direction=Target.transform.position+Vector3.up*.7f-transform.position;
            float travel=(Torpedo?52:95)*dt;
            if(direction.magnitude<=travel+Target.Stats.Radius*.35f)
            {
                var impact=Target.transform.position+new Vector3(direction.normalized.x*-Target.Stats.Radius*.45f,Target.Squadron!=null?.8f:2,direction.normalized.z*-Target.Stats.Radius*.45f);
                bool shielded=Target.Shield>0;Game.BattleEffects.Pulse(impact,shielded?new Color(.22f,.65f,1):new Color(1,.38f,.06f),Torpedo?3.3f:shielded?1.5f:2,.23f,!shielded);
                Target.Damage(Damage,transform.position); Game.RemoveBolt(this); return;
            }
            transform.rotation=Quaternion.LookRotation(direction);
            transform.position+=direction.normalized*travel;
            var wake=GetComponent<LineRenderer>();if(wake!=null){wake.SetPosition(0,transform.position);wake.SetPosition(1,transform.position-direction.normalized*3.2f);}
        }
    }
}
