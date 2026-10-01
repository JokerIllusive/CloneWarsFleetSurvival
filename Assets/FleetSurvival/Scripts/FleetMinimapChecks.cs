using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        void CheckDetailedMinimap(List<string> checks)
        {
            Begin(Faction.Republic);ClearBattle();Salvage=2000;
            var command=Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(-25,0,-20));
            var carrier=Spawn(Faction.Republic,ShipClass.Carrier,true,false,new Vector3(-30,0,25));
            var fighter=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-5,0,-10));
            var interceptor=Spawn(Faction.Republic,ShipClass.Interceptor,true,false,new Vector3(15,0,-25));
            var bomber=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(15,0,30),1,SquadronRole.Strike);
            var enemy=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(40,0,5));
            command.Selected=true;command.transform.rotation=Quaternion.Euler(0,90,0);HUD.RefreshTacticalMap();
            var map=FindObjectOfType<FleetMinimap>();var rect=map.GetComponent<RectTransform>();
            var symbols=map.GetComponentsInChildren<FleetMapIcon>();
            Check(symbols.Where(s=>s.Shape!=FleetMapShape.Destination).Select(s=>s.Shape).Distinct().Count()==6 && symbols.Count(s=>s.Selected)==1,"Minimap distinguishes commands, carriers, capitals and three squadron roles",checks);
            var flagshipIcon=symbols.Single(s=>s.Shape==FleetMapShape.Command);
            Check(Mathf.Abs(Mathf.DeltaAngle(flagshipIcon.rectTransform.eulerAngles.z,-90))<.01f && flagshipIcon.Selected && flagshipIcon.color==new Color(.22f,.8f,1),"Minimap heading and selection preserve allied contact color",checks);
            var terrain=map.GetComponentInChildren<FleetMapTerrain>();
            Check(terrain.AsteroidCount==112 && terrain.LandmarkCount==2 && !terrain.raycastTarget,"Detailed map uses actual sector asteroid and shipyard data",checks);
            var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(HUD.MapPoint(enemy.transform.position)))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            Check(hits.Count>0 && hits[0].gameObject==map.gameObject && symbols.All(s=>!s.raycastTarget),"Terrain and contact overlays do not block camera-pan clicks",checks);
            HUD.RefreshMapReadout(e.position,null);Check(HUD.MapReadout.Contains("HOSTILE / Munificent") && HUD.MapReadout.Contains("Hull 440") && HUD.MapReadout.Contains("Shields 240"),"Hover identifies an enemy contact with current hull and shields",checks);
            OrderMove(new Vector3(20,0,-5),false);HUD.RefreshTacticalMap();
            var goals=map.GetComponentsInChildren<FleetMapIcon>().Where(s=>s.Shape==FleetMapShape.Destination).ToArray();
            Check(goals.Length==1 && Vector2.Distance(goals[0].rectTransform.anchoredPosition,HUD.MapPoint(command.Destination))<.01f,"Selected formation destination is plotted at its actual final slot",checks);
            bool[] selected=Ships.Select(s=>s.Selected).ToArray();Vector3[] orders=Ships.Select(s=>s.Destination).ToArray();
            ExecuteEvents.Execute(map.gameObject,e,ExecuteEvents.pointerDownHandler);
            Check(selected.SequenceEqual(Ships.Select(s=>s.Selected)) && orders.SequenceEqual(Ships.Select(s=>s.Destination)),"Panning through detailed contacts preserves fleet selection and orders",checks);
            command.Selected=false;HUD.RefreshTacticalMap();Check(!map.GetComponentsInChildren<FleetMapIcon>().Any(s=>s.Shape==FleetMapShape.Destination),"Deselection hides the destination without cancelling the move",checks);
            carrier.Hangar.Recover(fighter);fighter.transform.position=carrier.Hangar.DockPoint;Tick(.01f);HUD.RefreshTacticalMap();
            Check(fighter.Docked && !map.GetComponentsInChildren<FleetMapIcon>().Any(s=>s.Shape==FleetMapShape.Fighter),"Docked wings are absent from airborne minimap contacts",checks);
            Check(RecruitAt(ShipClass.Frigate,new Vector3(-25,0,5)),"Detailed minimap scenario reserves a reinforcement",checks);HUD.RefreshTacticalMap();
            var incoming=map.GetComponentsInChildren<FleetMapIcon>().Single(s=>s.Shape==FleetMapShape.Incoming);
            e.position=RectTransformUtility.WorldToScreenPoint(null,incoming.rectTransform.position);HUD.RefreshMapReadout(e.position,null);
            Check(HUD.MapIncomingCount==1 && HUD.MapReadout.Contains("INBOUND / Acclamator") && HUD.MapReadout.Contains("hyperspace charge"),"Queued hyperspace arrivals expose landing positions and countdowns",checks);
            float jumpTime=jumps[0].Elapsed;var pulse=incoming.rectTransform.localScale;
            TogglePause();Tick(4);HUD.RefreshTacticalMap();Check(jumps[0].Elapsed==jumpTime && incoming.rectTransform.localScale==pulse,"Pause freezes hyperspace minimap countdown and arrival pulse",checks);TogglePause();
            Tick(3);HUD.RefreshTacticalMap();Check(HUD.MapIncomingCount==0 && !map.GetComponentsInChildren<FleetMapIcon>().Any(s=>s.Shape==FleetMapShape.Incoming),"Completed arrival removes its pending minimap beacon",checks);
            enemy.Hull=0;ShipDestroyed(enemy,true);HUD.RefreshTacticalMap();var warning=map.GetComponentsInChildren<FleetMapIcon>().Single(s=>s.Shape==FleetMapShape.Reactor);
            e.position=RectTransformUtility.WorldToScreenPoint(null,warning.rectTransform.position);HUD.RefreshMapReadout(e.position,null);
            Check(HUD.MapReactorCount==1 && HUD.MapReadout.Contains("REACTOR FAILURE") && HUD.MapReadout.Contains("blast radius"),"Reactor hazards show their countdown and blast-radius warning",checks);
            ClearBattle();HUD.RefreshTacticalMap();Check(HUD.MapIncomingCount==0 && HUD.MapReactorCount==0 && !map.GetComponentsInChildren<FleetMapIcon>().Any(),"Battle reset clears contact, route, arrival and reactor overlays",checks);
        }
        IEnumerator DetailedMinimapPreview()
        {
            Begin(Faction.Republic);ClearBattle();Wave=6;Salvage=1200;Phase=BattlePhase.Combat;enemiesRemaining=0;bossQueued=false;
            var command=Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(-22,0,-18));
            var destroyer=Spawn(Faction.Republic,ShipClass.Destroyer,true,false,new Vector3(-30,0,-40));
            Spawn(Faction.Republic,ShipClass.Carrier,true,false,new Vector3(-42,0,22));
            Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-10,0,-2));
            Spawn(Faction.Republic,ShipClass.Interceptor,true,false,new Vector3(-10,0,30));
            Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-4,0,-35),1,SquadronRole.Strike);
            Spawn(Faction.CIS,ShipClass.Flagship,false,false,new Vector3(22,0,-15));
            var doomed=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(8,0,22));
            Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(30,0,-38));
            Spawn(Faction.CIS,ShipClass.Carrier,false,false,new Vector3(38,0,28));
            Spawn(Faction.CIS,ShipClass.Interceptor,false,false,new Vector3(12,0,2));
            Spawn(Faction.CIS,ShipClass.Fighter,false,false,new Vector3(8,0,42),1,SquadronRole.Strike);
            command.Selected=true;destroyer.Selected=true;OrderMove(new Vector3(-14,0,8),false);
            RecruitAt(ShipClass.Frigate,new Vector3(-46,0,-10));
            Tick(.8f);doomed.Hull=0;ShipDestroyed(doomed,true);Tick(.2f);
            cameraFocus=new Vector3(-4,0,-6);cameraDistance=92;PositionCamera();
            Notify("TACTICAL MINIMAP / orbital detail, ship headings, destinations, arrivals and reactor danger");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-minimap-preview.png"));
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-minimap-compact-preview.png"),1280,800);
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-minimap-detail-preview.png"),3200,1800,true);
        }
    }
}
