using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        void CheckCarriersAndTactics(List<string> checks)
        {
            foreach(var faction in new[]{Faction.Republic,Faction.CIS})
            {
                Begin(faction);ClearBattle();Salvage=1000;
                var carrier=Spawn(faction,ShipClass.Carrier,true,false,Vector3.zero);carrier.Selected=true;
                Check(carrier.Hangar.Bays==(faction==Faction.Republic?2:3),"Limited carrier bay count "+faction,checks);
                int capacity=FleetCapacityUsed,funds=Salvage;
                Check(LaunchSelectedSquadron(SquadronRole.Strike),"Selected carrier launches strike squadron "+faction,checks);
                var squad=carrier.Hangar.Squadrons.Single();
                Check(squad.Squadron.ActiveCount==6 && squad.Role==SquadronRole.Strike && squad.HomeHangar==carrier.Hangar && squad.WeaponRefits==0 && FleetCapacityUsed==capacity+1 && Salvage==funds-75,"Hangar launch spends salvage, reserves capacity and creates six unrefitted craft "+faction,checks);
                Check(squad.IsArriving && !squad.GetComponent<Collider>().enabled && !squad.Targetable && !LaunchSelectedSquadron(SquadronRole.Fighter),"Launch animation and cooldown block immediate combat and repeat launches "+faction,checks);
                Vector3 before=squad.transform.position;float cooldown=carrier.Hangar.Cooldown;
                TogglePause();Tick(3);Check(squad.transform.position==before && carrier.Hangar.Cooldown==cooldown,"Pause freezes hangar launches and deck cooldown "+faction,checks);TogglePause();
                for(int i=0;i<42;i++) Tick(.05f);
                Check(squad.Targetable && squad.GetComponent<Collider>().enabled && squad.VisualRoot.localScale==Vector3.one && Vector3.Distance(carrier.transform.position,squad.transform.position)>10,"Six-craft wing flies out of its carrier and becomes active "+faction,checks);
                foreach(var ship in Ships) ship.Selected=ship==squad;
                Check(UpgradeWeapons() && squad.WeaponRefits==1 && Mathf.Abs(squad.RefitBonus-.1f)<.001f,"Strike loadout refit uses its own base weapon damage "+faction,checks);
                squad.Shield=0;squad.Damage(squad.MaxHull/3+.1f);
                Check(squad.Squadron.ActiveCount==4 && RunTally.FriendlyFighters==2,"Strike squadron tracks two individual craft losses "+faction,checks);
                Check(RecoverSelectedSquadron() && squad.ReturningToHangar,"Owned squadron receives recovery order "+faction,checks);
                float returningHull=squad.Hull;squad.Damage(1);
                Check(squad.Hull<returningHull && squad.ReturningToHangar,"Returning fighters remain vulnerable "+faction,checks);
                for(int i=0;i<1800 && !squad.Docked;i++) Tick(.04f);
                Check(squad.Docked && !squad.Targetable && !squad.GetComponent<Collider>().enabled && !squad.VisualRoot.gameObject.activeSelf && FleetCapacityUsed==capacity+1,"Recovery reaches carrier and retains squadron capacity "+faction,checks);
                SelectHangarBay(carrier.Hangar,0);HUD.RefreshText();
                Check(HUD.HangarDetails.Contains("Docked") && HUD.SelectedDetails.Contains("Strike / torpedoes"),"Hangar bay selection exposes docked unit and loadout information "+faction,checks);
                funds=Salvage;int survivors=squad.Squadron.ActiveCount;float timer=squad.DockTimer;float hull=squad.Hull;
                TogglePause();Tick(12);Check(squad.DockTimer==timer && squad.Squadron.ActiveCount==survivors && squad.Hull==hull && Salvage==funds,"Pause freezes hangar repair, replacement and spending "+faction,checks);TogglePause();
                Check(!RelaunchSelectedSquadron(),"Docked squadron cannot instantly relaunch "+faction,checks);
                Tick(6);Check(squad.Squadron.ActiveCount==5 && Salvage==funds-8 && RunTally.FriendlyFighters==2,"Hangar replaces one fighter for eight salvage without erasing losses "+faction,checks);
                Tick(6);Check(squad.Squadron.ActiveCount==6 && Salvage==funds-16 && Mathf.Abs(squad.Hull-squad.MaxHull)<.01f,"Hangar restores full six-craft squadron over time "+faction,checks);
                int units=Ships.Count;capacity=FleetCapacityUsed;
                Check(RelaunchSelectedSquadron() && Ships.Count==units && FleetCapacityUsed==capacity && squad.WeaponRefits==1,"Relaunch reuses recovered unit, capacity and refits "+faction,checks);
                Tick(2.1f);var enemy=Spawn(faction==Faction.Republic?Faction.CIS:Faction.Republic,ShipClass.Carrier,false,false,new Vector3(0,0,25));
                Fire(squad,enemy,squad.EffectiveVolleyDamage*FleetTactics.DamageModifier(squad,enemy));squad.Weapons.Tick(.5f);
                Check(Bolts.Count==6 && Bolts.All(b=>b.Torpedo) && Mathf.Abs(Bolts.Sum(b=>b.Damage)-squad.EffectiveVolleyDamage*1.9f)<.01f,"Every surviving strike craft fires a capital-effective torpedo "+faction,checks);
            }
            Begin(Faction.Republic);ClearBattle();Salvage=1000;
            var host=Spawn(Faction.Republic,ShipClass.Carrier,true,false,Vector3.zero);var owned=host.Hangar.Launch(SquadronRole.Fighter);Tick(2.1f);
            host.Hangar.Recover(owned);owned.transform.position=host.Hangar.DockPoint;Tick(.01f);Salvage=0;owned.Shield=0;
            // A missing fighter is represented by ordinary damage before docking, rather than a test-only repair shortcut.
            host.Hangar.Tick(12);host.Hangar.Relaunch(owned);Tick(2.1f);owned.Shield=0;owned.Damage(owned.MaxHull/6+.1f);host.Hangar.Recover(owned);owned.transform.position=host.Hangar.DockPoint;Tick(.01f);
            Tick(12);Check(owned.Docked && owned.Squadron.ActiveCount==5 && Salvage==0,"No salvage means no free replacement fighters",checks);
            Salvage=8;Tick(6);Check(owned.Squadron.ActiveCount==6 && Salvage==0,"Replacement resumes after salvage becomes available",checks);
            Salvage=1000;while(FleetCapacityUsed<22) Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(60,0,-50));
            int money=Salvage;Check(!host.Hangar.CanLaunch(SquadronRole.Interceptor) && host.Hangar.Launch(SquadronRole.Interceptor)==null && Salvage==money,"Full fleet blocks carrier launch without spending salvage",checks);
            Begin(Faction.Republic);Salvage=1000;while(FleetCapacityUsed<21) Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(60,0,-50));
            Check(RecruitAt(ShipClass.Fighter,new Vector3(-50,0,-20)),"Pending hyperspace wing reserves the last fleet capacity",checks);money=Salvage;
            Check(!Flagship.Hangar.CanLaunch(SquadronRole.Fighter) && Flagship.Hangar.Launch(SquadronRole.Fighter)==null && Salvage==money,"Carrier launch respects capacity reserved for hyperspace call-ins",checks);
            Begin(Faction.Republic);Salvage=1000;
            Check(Flagship.Hangar.Bays==1 && Flagship.Hangar.Launch(SquadronRole.Fighter)!=null,"Command ship has one usable hangar bay",checks);
            Flagship.Hangar.Tick(12);money=Salvage;Check(Flagship.Hangar.Launch(SquadronRole.Strike)==null && Salvage==money,"Occupied command bay prevents fighter spam after cooldown",checks);
            Begin(Faction.Republic);ClearBattle();Salvage=1000;
            host=Spawn(Faction.Republic,ShipClass.Carrier,true,false,Vector3.zero);
            var adopted=Spawn(Faction.Republic,ShipClass.Fighter,true,false,host.Hangar.DockPoint);
            Check(host.Hangar.Recover(adopted) && adopted.HomeHangar==host.Hangar,"Carrier can recover an existing purchased squadron into a free bay",checks);Tick(.02f);
            var airborne=host.Hangar.Launch(SquadronRole.Interceptor);Tick(2.1f);
            var excess=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(35,0,0));
            Check(!host.Hangar.Recover(excess) && excess.HomeHangar==null,"Recovery cannot exceed carrier bay capacity",checks);
            host.Shield=0;host.Damage(host.MaxHull+1);
            Check(!Ships.Contains(adopted) && !adopted.Alive && RunTally.FriendlyFighters==6 && RunTally.FriendlyCapitals==1,"Carrier loss destroys docked craft and counts each loss once",checks);
            Check(airborne.Alive && airborne.HomeHangar==null && !airborne.ReturningToHangar && Ships.Contains(airborne),"Deployed squadrons survive carrier loss independently",checks);
            Begin(Faction.Republic);ClearBattle();Salvage=1000;
            host=Spawn(Faction.Republic,ShipClass.Carrier,true,false,Vector3.zero);airborne=host.Hangar.Launch(SquadronRole.Fighter);host.Shield=0;host.Damage(host.MaxHull+1);
            Check(airborne.Targetable && airborne.GetComponent<Collider>().enabled && airborne.HomeHangar==null,"Carrier death during takeoff releases surviving squadron without a stuck launch",checks);
            Begin(Faction.Republic);ClearBattle();Phase=BattlePhase.Combat;Wave=6;
            var enemyCarrier=Spawn(Faction.CIS,ShipClass.Carrier,false,false,new Vector3(35,0,30));
            for(int n=0;n<3;n++) {enemyCarrier.Hangar.Tick(12);var launched=enemyCarrier.Hangar.Squadrons.Last();launched.Tick(2.1f);launched.Shield=0;launched.Damage(launched.MaxHull+1);}
            enemyCarrier.Hangar.Tick(100);
            Check(enemyCarrier.Hangar.UsedBays==0 && EnemyCount==1,"Enemy carrier has a finite wave reserve and cannot spawn endlessly",checks);
            CheckRoleTactics(checks);
        }
        void CheckRoleTactics(List<string> checks)
        {
            Begin(Faction.Republic);ClearBattle();
            var hunter=Spawn(Faction.Republic,ShipClass.Interceptor,true,false,Vector3.zero);
            var cap=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(0,0,10));
            var craft=Spawn(Faction.CIS,ShipClass.Fighter,false,false,new Vector3(0,0,15));
            Check(NearestEnemy(hunter)==craft,"Interceptor prefers a nearby fighter wing over a closer capital",checks);
            var strike=Spawn(Faction.Republic,ShipClass.Fighter,true,false,Vector3.zero,1,SquadronRole.Strike);
            Check(NearestEnemy(strike)==cap,"Strike loadout prefers capital targets",checks);
            var fighter=Spawn(Faction.Republic,ShipClass.Fighter,true,false,Vector3.zero);
            var bomber=Spawn(Faction.CIS,ShipClass.Fighter,false,false,new Vector3(0,0,15),1,SquadronRole.Strike);
            Check(NearestEnemy(fighter)==bomber,"Fighter screen prioritizes an enemy strike squadron",checks);
            Check(FleetTactics.DamageModifier(hunter,craft)>FleetTactics.DamageModifier(hunter,cap) && FleetTactics.DamageModifier(strike,cap)>FleetTactics.DamageModifier(strike,craft) && FleetTactics.DamageModifier(cap,craft)<1,"Role counters favor interceptors versus craft and strikes versus capitals",checks);
            var launch=FleetSound.Get("StrikeLaunch");var samples=new float[launch.samples];
            Check(launch.GetData(samples,0) && launch.frequency==44100 && launch.length>.5f && samples.Any(s=>Mathf.Abs(s)>.02f),"Strike loadouts have a distinct non-silent torpedo-launch sound",checks);
            hunter.AttackTarget(cap);hunter.Tick(.01f);Check(hunter.ForcedTarget==cap,"Explicit focus-fire order overrides automatic role preference",checks);
            var carrier=Spawn(Faction.CIS,ShipClass.Carrier,false,false,new Vector3(0,0,50));
            Check(FleetTactics.BattlePosition(carrier,strike,out var rangePoint) && Vector3.Distance(rangePoint,strike.transform.position)>carrier.Stats.Range*.8f,"Enemy carrier maintains standoff range",checks);
            var escort=Spawn(Faction.CIS,ShipClass.Escort,false,false,new Vector3(30,0,40));
            Check(FleetTactics.BattlePosition(escort,strike,out var screenPoint) && Vector3.Distance(screenPoint,carrier.transform.position)<25,"Enemy escort stays near its carrier when the threat is distant",checks);
            var destroyer=Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(35,0,40));
            Check(FleetTactics.BattlePosition(destroyer,strike,out var flankPoint) && Vector3.Cross((strike.transform.position-destroyer.transform.position).normalized,(flankPoint-strike.transform.position).normalized).magnitude>.1f,"Enemy destroyer chooses an offset flanking approach",checks);
            Check(!FleetTactics.BattlePosition(fighter,cap,out var ignored),"Enemy tactics do not replace player fleet orders",checks);
            Check(FleetRules.WavePlan(6).Any(e=>e.Kind==ShipClass.Carrier) && FleetRules.WavePlan(4).Any(e=>e.Kind==ShipClass.Escort) && Enumerable.Range(3,8).Any(w=>FleetRules.WavePlan(w).Any(e=>e.Role==SquadronRole.Strike)),"Later waves include carriers, screening escorts and strike loadouts",checks);
            Wave=5;Check(NextWaveBriefing.Contains("carrier") && NextWaveBriefing.Contains("WAVE 6"),"Next-wave briefing reveals upcoming fleet composition",checks);
        }
    }
}
