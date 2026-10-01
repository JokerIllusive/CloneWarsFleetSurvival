using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        void CheckReactorDamage(List<string> checks)
        {
            Begin(Faction.Republic);ClearBattle();
            var source=Spawn(Faction.CIS,ShipClass.Destroyer,false,false,Vector3.zero);
            float radius=source.Stats.Radius*2.8f,peak=110+source.Stats.Radius*12;
            var center=Spawn(Faction.Republic,ShipClass.Carrier,true,false,Vector3.zero);
            var midway=Spawn(Faction.Republic,ShipClass.Frigate,true,false,new Vector3(radius*.5f,0,0));midway.Shield=20;
            var edge=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(-radius,0,0));edge.Shield=0;
            var outside=Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(radius+.01f,0,0));outside.Shield=0;
            var fighters=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(0,0,radius*.25f));fighters.Shield=0;
            var arriving=Spawn(Faction.Republic,ShipClass.Frigate,true,false,new Vector3(0,0,-2));arriving.IsArriving=true;
            var docked=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(0,0,-3));docked.Docked=true;
            source.Hull=0;ShipDestroyed(source,true);var wreck=wrecks.Single();
            var ring=wreck.GetComponentsInChildren<LineRenderer>().Single(r=>r.name=="Reactor blast radius");
            Check(Mathf.Abs(wreck.BlastRadius-radius)<.001f && Mathf.Abs(new Vector2(ring.GetPosition(0).x,ring.GetPosition(0).z).magnitude-radius)<.001f,"Reactor damage and its world/minimap warning share one blast radius",checks);
            wreck.Tick(1.5f);
            Check(!wreck.Complete && center.Shield==center.MaxShield && midway.Hull==midway.MaxHull,"Reactor countdown and secondary explosions do not apply the final blast early",checks);
            wreck.Tick(1.2f);
            Check(wreck.Complete && Mathf.Abs(center.Shield-(center.MaxShield-peak))<.001f && center.Hull==center.MaxHull,"Reactor center delivers full damage through shields before hull",checks);
            Check(midway.Shield==0 && Mathf.Abs(midway.Hull-(midway.MaxHull-(peak*.625f-20)))<.001f,"Mid-radius reactor damage exhausts shields and spills into friendly hull",checks);
            Check(Mathf.Abs(edge.Hull-(edge.MaxHull-peak*.25f))<.001f,"Hostiles on the danger-ring edge receive 25 percent reactor damage",checks);
            Check(outside.Hull==outside.MaxHull,"A large hull with its center just outside the marked radius receives no reactor damage",checks);
            Check(Mathf.Abs(fighters.Hull-(fighters.MaxHull-peak*.8125f))<.001f && fighters.Squadron.ActiveCount<6,"Reactor damage inside the ring destroys individual fighter craft",checks);
            Check(arriving.Shield==arriving.MaxShield && arriving.Hull==arriving.MaxHull && docked.Hull==docked.MaxHull,"Hyperspace and docked units remain protected from the reactor blast",checks);
            float heldHull=midway.Hull,heldShield=center.Shield;wreck.Tick(5);
            Check(midway.Hull==heldHull && center.Shield==heldShield,"A completed reactor detonation applies damage exactly once",checks);
            ClearBattle();
        }
        IEnumerator ReactorDamagePreview()
        {
            Begin(Faction.Republic);ClearBattle();Wave=6;Phase=BattlePhase.Preparation;
            Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(-30,0,-24));
            var target=Spawn(Faction.Republic,ShipClass.Frigate,true,false,new Vector3(-1,0,0));target.Shield=0;target.Selected=true;
            var fighters=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(14,0,-4));fighters.Shield=0;
            Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(-5,0,12));
            Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(32,0,5));
            var source=Spawn(Faction.CIS,ShipClass.Carrier,false,false,new Vector3(5,0,0));source.Hull=0;ShipDestroyed(source,true);
            var wreck=wrecks.Single();cameraFocus=new Vector3(3,0,-4);cameraDistance=64;PositionCamera();
            Notify("REACTOR BLAST / ships inside the orange ring take damage; shields absorb the hit first");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-reactor-before-preview.png"));
            wreck.Tick(3);
            Notify("DETONATION / selected Acclamator: "+Mathf.CeilToInt(target.Hull)+" hull remaining | ARC-170: "+fighters.Squadron.ActiveCount+" / 6 fighters");
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-reactor-after-preview.png"));
        }
    }
}
