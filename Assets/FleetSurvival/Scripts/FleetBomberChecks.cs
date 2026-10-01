using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        void CheckBomberModels(List<string> checks)
        {
            foreach(var faction in new[]{Faction.Republic,Faction.CIS})
            {
                Begin(faction);ClearBattle();Salvage=1000;
                var carrier=Spawn(faction,ShipClass.Carrier,true,false,Vector3.zero);
                var bombers=carrier.Hangar.Launch(SquadronRole.Strike);
                string model=faction==Faction.Republic?"YWing":"Hyena";
                Check(bombers!=null && bombers.Squadron.ActiveCount==6 && Enumerable.Range(0,6).All(i=>bombers.VisualRoot.GetChild(i).GetChild(0).name.StartsWith(model)),"Carrier launches six dedicated bomber craft "+faction,checks);
                var materials=bombers.VisualRoot.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).ToArray();
                Check(materials.Length>0 && materials.All(m=>m!=null && m.shader!=null && m.shader.name!="Hidden/InternalErrorShader") && materials.Any(m=>m.mainTexture!=null) && bombers.Stats.Name.Contains(faction==Faction.Republic?"Y-wing":"Hyena"),"Bomber textures and selected-unit names match their model "+faction,checks);
                var enemyBombers=Spawn(faction,ShipClass.Fighter,false,false,new Vector3(25,0,0),1,SquadronRole.Strike);
                var fighters=Spawn(faction,ShipClass.Fighter,true,false,new Vector3(-25,0,0));
                Check(enemyBombers.VisualRoot.GetChild(0).GetChild(0).name.StartsWith(model) && fighters.VisualRoot.GetChild(0).GetChild(0).name.StartsWith(faction==Faction.Republic?"ARC170":"Vulture"),"Enemy strike wings use bombers while ordinary fighters retain their models "+faction,checks);
            }
            Begin(Faction.CIS);ClearBattle();
            var kinds=new[]{ShipClass.Frigate,ShipClass.Flagship,ShipClass.Destroyer,ShipClass.Carrier};
            float[] lengths={10,17,13,18};
            for(int i=0;i<kinds.Length;i++)
            {
                var ship=Spawn(Faction.CIS,kinds[i],true,kinds[i]==ShipClass.Flagship,new Vector3(i*30,0,0));
                var meshes=ship.VisualRoot.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<HullSection>()!=null).ToArray();
                var renderers=meshes.Select(m=>m.GetComponent<MeshRenderer>()).ToArray();
                var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Check(meshes.Length==6 && ship.VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0 && Mathf.Abs(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)-lengths[i])<.05f && renderers.SelectMany(r=>r.sharedMaterials).All(m=>m!=null && m.shader!=null && m.shader.name!="Hidden/InternalErrorShader"),"CIS replacement hull retains normalized scale, static sections and valid materials "+kinds[i],checks);
            }
        }
        IEnumerator BomberFleetPreview()
        {
            foreach(var faction in new[]{Faction.Republic,Faction.CIS})
            {
                Begin(faction);ClearBattle();Salvage=650;Wave=4;Phase=BattlePhase.Combat;enemiesRemaining=0;bossQueued=false;
                var carrier=Spawn(faction,ShipClass.Carrier,true,true,new Vector3(-18,0,-15));
                var target=Spawn(faction==Faction.Republic?Faction.CIS:Faction.Republic,ShipClass.Carrier,false,false,new Vector3(15,0,12));
                var bombers=carrier.Hangar.Launch(SquadronRole.Strike);for(int i=0;i<44;i++)Tick(.05f);
                bombers.transform.position=new Vector3(-3,0,0);bombers.AttackTarget(target);bombers.Selected=true;
                cameraFocus=new Vector3(-5,0,-5);cameraDistance=68;PositionCamera();
                for(int i=0;i<12;i++)Tick(.025f);
                Notify(faction==Faction.Republic?"BTL-B Y-WING / six-craft bomber squadron":"HYENA BOMBER / six-craft bomber squadron");
                yield return null;
                HUD.CapturePreview(Path.Combine(Application.persistentDataPath,faction==Faction.Republic?"fleet-ywing-preview.png":"fleet-hyena-preview.png"));
            }
            Begin(Faction.CIS);ClearBattle();Salvage=650;
            Spawn(Faction.CIS,ShipClass.Flagship,true,true,new Vector3(-18,0,16));
            Spawn(Faction.CIS,ShipClass.Frigate,true,false,new Vector3(18,0,16));
            Spawn(Faction.CIS,ShipClass.Destroyer,true,false,new Vector3(-18,0,-15));
            Spawn(Faction.CIS,ShipClass.Carrier,true,false,new Vector3(18,0,-15));
            Spawn(Faction.CIS,ShipClass.Fighter,true,false,new Vector3(-10,0,-36),1,SquadronRole.Strike);
            Spawn(Faction.CIS,ShipClass.Interceptor,true,false,new Vector3(12,0,-36));
            cameraFocus=new Vector3(0,0,-8);cameraDistance=100;PositionCamera();
            Tick(.02f);Notify("CIS FLEET / replacement capital hulls and Hyena bombers");
            yield return null;HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-cis-replacement-preview.png"));
        }
    }
}
