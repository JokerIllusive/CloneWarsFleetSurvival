using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetHangar
    {
        public readonly FleetShip Owner;
        readonly List<FleetShip> squads=new List<FleetShip>();
        public IReadOnlyList<FleetShip> Squadrons => squads;
        public int Bays => Owner.Kind==ShipClass.Carrier?(Owner.Faction==Faction.CIS?3:2):1;
        public int UsedBays => squads.Count(s=>s!=null && s.Alive);
        public float Cooldown { get; private set; }
        int enemyReserve;
        public FleetHangar(FleetShip owner) {Owner=owner;enemyReserve=Bays;Cooldown=owner.Friendly?0:12;}
        public Vector3 Port => Owner.transform.TransformPoint(new Vector3(0,.8f,Owner.Stats.Radius*.2f));
        public Vector3 DockPoint => Vector3.ClampMagnitude(Owner.transform.position-Owner.transform.forward*(Owner.Stats.Radius+8),FleetRules.ArenaRadius-7);
        public Vector3 LaunchPoint(FleetShip squad)
        {return Vector3.ClampMagnitude(Owner.transform.position+Owner.transform.forward*(Owner.Stats.Radius+squad.Stats.Radius+5)+Owner.transform.right*(squads.IndexOf(squad)-(Bays-1)*.5f)*14,FleetRules.ArenaRadius-squad.Stats.Radius-1);}
        public bool Available => Owner.Alive && !Owner.IsArriving && !Owner.Game.Paused && (Owner.Game.Phase==BattlePhase.Preparation || Owner.Game.Phase==BattlePhase.Combat);
        public bool CanLaunch(SquadronRole role) => Available && Cooldown<=0 && UsedBays<Bays && (!Owner.Friendly || Owner.Game.FleetCapacityUsed<FleetRules.FleetLimit && Owner.Game.Salvage>=FleetRules.LaunchCost(role));
        public FleetShip Launch(SquadronRole role)
        {
            if(!CanLaunch(role)) return null;
            if(Owner.Friendly && !Owner.Game.SpendSalvage(FleetRules.LaunchCost(role))) return null;
            var kind=role==SquadronRole.Interceptor?ShipClass.Interceptor:ShipClass.Fighter;
            var squad=Owner.Game.Spawn(Owner.Faction,kind,Owner.Friendly,false,Port,Owner.MaxHull/Owner.Stats.Hull,role);
            squads.Add(squad);squad.HomeHangar=this;squad.StartHangarLaunch();Cooldown=12;return squad;
        }
        public bool Recover(FleetShip squad)
        {
            if(!Available || squad==null || !squad.Alive || squad.Friendly!=Owner.Friendly || squad.Squadron==null || squad.IsArriving || squad.Docked || squad.ReturningToHangar || squad.HomeHangar!=null && squad.HomeHangar!=this) return false;
            if(!squads.Contains(squad)) {if(UsedBays>=Bays) return false;squads.Add(squad);}
            squad.HomeHangar=this;squad.Weapons.Clear();squad.ForcedTarget=null;squad.Formation=null;squad.HasMoveOrder=false;squad.ReturningToHangar=true;return true;
        }
        public bool Relaunch(FleetShip squad)
        {
            if(!Available || Cooldown>0 || squad==null || squad.HomeHangar!=this || !squad.Docked || squad.DockTimer<6) return false;
            squad.StartHangarLaunch();Cooldown=12;return true;
        }
        public void Tick(float dt)
        {
            if(!Available) return;
            squads.RemoveAll(s=>s==null || !s.Alive);Cooldown=Mathf.Max(0,Cooldown-dt);
            foreach(var squad in squads.ToArray()) if(squad.Docked)
            {
                squad.transform.position=Port;squad.DockTimer+=dt;squad.ServiceTimer+=dt;
                squad.Squadron.RepairLiving(squad.MaxHull*.04f*dt);squad.Shield=Mathf.Min(squad.MaxShield,squad.Shield+squad.MaxShield*.06f*dt);
                if(squad.ServiceTimer>=6) {squad.ServiceTimer-=6;if(squad.Squadron.ActiveCount<6 && (!Owner.Friendly || Owner.Game.SpendSalvage(8))) squad.Squadron.RestoreOne();}
            }
            if(!Owner.Friendly && Owner.Game.Phase==BattlePhase.Combat && Owner.Game.Wave>=6 && enemyReserve>0 && CanLaunch(enemyReserve%2==0?SquadronRole.Interceptor:SquadronRole.Strike))
            {if(Launch(enemyReserve%2==0?SquadronRole.Interceptor:SquadronRole.Strike)!=null) enemyReserve--;}
        }
        public void CarrierLost()
        {
            foreach(var squad in squads.ToArray()) if(squad!=null && squad.Alive)
            {
                squad.HomeHangar=null;squad.ReturningToHangar=false;
                if(squad.Docked) {squad.Docked=false;squad.IsArriving=false;squad.Damage(squad.MaxHull+squad.MaxShield+1);}
                else if(squad.IsArriving) squad.FinishHangarLaunch();
            }
            squads.Clear();
        }
    }
}
