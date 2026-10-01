using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        void CheckBattleVisuals(List<string> assertions)
        {
            Begin(Faction.Republic);ClearBattle();
            var squad=Spawn(Faction.Republic,ShipClass.Interceptor,true,false,new Vector3(-10,0,0));
            Check(squad.EngineEffects.JetCount==12 && squad.VisualRoot.childCount==6,"Engine effects preserve six-fighter hierarchy",assertions);
            squad.MoveTo(new Vector3(15,0,5));for(int i=0;i<12;i++)Tick(.05f);
            Check(Mathf.Abs(squad.EngineEffects.Bank)>.1f && Mathf.Abs(squad.EngineEffects.Bank)<=22,"Moving fighters bank within visual limits",assertions);
            Check(Mathf.Abs(squad.transform.position.y)<.001f && squad.GetComponent<Collider>().enabled,"Banking preserves tactical command plane and collider",assertions);
            BattleEffects.Pulse(Vector3.zero,Color.blue,2,.3f);int active=BattleEffects.ActiveFlashes;float bank=squad.EngineEffects.Bank;
            TogglePause();Tick(2);Check(BattleEffects.ActiveFlashes==active && squad.EngineEffects.Bank==bank,"Tactical pause freezes flashes and fighter bank",assertions);TogglePause();
            for(int i=0;i<100;i++)BattleEffects.Pulse(Vector3.zero,Color.red,1,.1f);
            Check(BattleEffects.ActiveFlashes<=32 && effectsRoot.GetComponentsInChildren<Light>().Length<=8,"Active battle flash pool and unshadowed lights remain bounded",assertions);
            BattleEffects.Tick(1);Check(BattleEffects.ActiveFlashes==0,"Battle flashes expire without lingering glows",assertions);
            ClearBattle();Check(BattleEffects.ActiveFlashes==0,"Restart clears pooled battle effects",assertions);
            var capital=Spawn(Faction.Republic,ShipClass.Frigate,true,false,Vector3.zero);
            capital.Shield=0;capital.Damage(capital.MaxHull+1);
            Check(wrecks.Last().GetComponentsInChildren<MeshRenderer>().All(r=>!r.name.StartsWith("Engine ")),"Dead capital husks retain no active engine glow",assertions);
        }
        IEnumerator BattleVisualPreview()
        {
            Begin(Faction.Republic);ClearBattle();Wave=4;Phase=BattlePhase.Combat;Salvage=650;enemiesRemaining=0;bossQueued=false;
            Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(-12,0,-18));
            Spawn(Faction.Republic,ShipClass.Frigate,true,false,new Vector3(-13,0,7));
            Spawn(Faction.Republic,ShipClass.Destroyer,true,false,new Vector3(-16,0,-37));
            Spawn(Faction.Republic,ShipClass.Carrier,true,false,new Vector3(-25,0,30));
            var arc=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-2,0,-6));
            Spawn(Faction.Republic,ShipClass.Interceptor,true,false,new Vector3(-3,0,16));
            Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-4,0,-32),1,SquadronRole.Strike);
            var providence=Spawn(Faction.CIS,ShipClass.Flagship,false,false,new Vector3(10,0,-17));
            var munificent=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(12,0,9));
            Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(11,0,-38));
            Spawn(Faction.CIS,ShipClass.Carrier,false,false,new Vector3(25,0,32));
            Spawn(Faction.CIS,ShipClass.Fighter,false,false,new Vector3(3,0,1));
            Spawn(Faction.CIS,ShipClass.Interceptor,false,false,new Vector3(4,0,21));
            foreach(var ship in Ships)ship.transform.rotation=Quaternion.Euler(0,ship.Friendly?90:270,0);
            // Stage explicit focus-fire orders; use ordinary health and damage profiles.
            foreach(var ship in Ships.Where(s=>s.Squadron==null))ship.AttackTarget(Ships.Where(s=>s.Squadron==null && s.Friendly!=ship.Friendly).OrderBy(s=>(s.transform.position-ship.transform.position).sqrMagnitude).First());
            munificent.Damage(munificent.Shield+munificent.MaxHull*.42f);
            arc.AttackTarget(providence);
            cameraFocus=new Vector3(-3,0,-5);cameraDistance=105;PositionCamera();
            for(int i=0;i<150;i++)Tick(.025f);
            Notify("BATTLE VISUAL PREVIEW / fighter flybys, engine wakes, battery flashes and shield impacts");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-battle-wide-preview.png"));
            cameraFocus=new Vector3(-4,0,-15);cameraDistance=68;PositionCamera();
            for(int i=0;i<25;i++)Tick(.025f);
            Notify("BATTLE VISUAL PREVIEW / close engagement");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-battle-close-preview.png"));
        }
    }
}
