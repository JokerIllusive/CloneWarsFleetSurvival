using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetGame
    {
        static Vector3 SurfacePoint(HullSection section,int index) => section.transform.TransformPoint(section.BreachCenters[index]);
        void CheckLocalizedDestruction(List<string> checks)
        {
            Begin(Faction.Republic);ClearBattle();Salvage=5000;
            var ship=Spawn(Faction.Republic,ShipClass.Flagship,true,false,Vector3.zero);
            var sections=ship.GetComponentsInChildren<HullSection>();var original=sections.Select(s=>s.GetComponent<MeshFilter>().sharedMesh).ToArray();
            var originalMaterials=sections.Select(s=>s.GetComponent<MeshRenderer>().sharedMaterials).ToArray();
            Check(Resources.Load<Shader>("FleetDamagedHull")!=null && Resources.Load<Shader>("FleetDamagedHull").isSupported,"Localized damage shader is included and supported in the Windows player",checks);
            Check(sections.All(s=>s.InnerHull!=null && s.BreachFragments.Length==12 && s.BreachFragments.Any(m=>m!=null)),"Capital sections include shared inner plating and twelve localized fracture sites",checks);
            var point=SurfacePoint(sections[0],0);ship.Damage(20,point);
            Check(ship.Destruction.LocalizedBreaches==0 && !ship.GetComponentsInChildren<HullDamageSkin>().Any(),"Shield-only hits leave hull geometry and materials intact",checks);
            ship.Shield=0;ship.Damage(75,point);
            var skin=ship.GetComponentsInChildren<HullDamageSkin>().Single(s=>s.MarkCount>0);
            Check(ship.Destruction.LocalizedBreaches==1 && ship.Destruction.Status=="INTACT","A substantial hull hit produces a localized breach before a health-stage threshold",checks);
            Check(sections.Select((s,i)=>s.GetComponent<MeshFilter>().sharedMesh==original[i] && s.gameObject.activeSelf).All(v=>v) && skin.InnerRenderer.GetComponent<MeshFilter>().sharedMesh==skin.GetComponent<HullSection>().InnerHull && skin.InnerRenderer.GetComponent<Collider>()==null,"Breach retains the original hull and exposes shared non-colliding inner plating",checks);
            var block=new MaterialPropertyBlock();skin.GetComponent<MeshRenderer>().GetPropertyBlock(block,0);
            Check(block.GetFloat("_BreachCount")==1 && block.GetVector("_Breach0").w>0 && block.GetVector("_Breach0").w<=.7f && skin.GetComponent<MeshRenderer>().sharedMaterials[0].GetTexture("baseColorTexture")==skin.OriginalMaterials[0].GetTexture("baseColorTexture"),"Localized scorch and jagged cutaway preserve ship textures with a small impact radius",checks);
            ship.Damage(75,point);Check(ship.Destruction.LocalizedBreaches==1,"Repeated hits at one breach do not duplicate armor shards and fire emitters",checks);
            foreach(var section in sections.Skip(1))ship.Damage(30,SurfacePoint(section,6));
            Check(ship.Destruction.LocalizedBreaches>1 && ship.Destruction.DamagedSections>1,"Separated impacts damage distinct hull areas rather than only advancing a global stage",checks);
            foreach(var section in sections)for(int i=0;i<12;i++)ship.Destruction.HullHit(SurfacePoint(section,i),30);
            Check(ship.Destruction.LocalizedBreaches<=8 && ship.GetComponentsInChildren<HullDamageSkin>().All(s=>s.MarkCount<=4),"Persistent impacts and fire emitters are bounded per ship and hull section",checks);
            Check(RepairFleet() && ship.Destruction.LocalizedBreaches==0 && ship.GetComponentsInChildren<HullDamageSkin>().All(s=>!s.enabled) && sections.Select((s,i)=>s.GetComponent<MeshRenderer>().sharedMaterials.SequenceEqual(originalMaterials[i])).All(v=>v),"Fleet repair restores original textured armor and removes cutaways, lining and fires",checks);
            var doomed=Spawn(Faction.CIS,ShipClass.Destroyer,false,false,new Vector3(35,0,30));var target=doomed.GetComponentsInChildren<HullSection>()[0];doomed.Shield=0;doomed.Damage(40,SurfacePoint(target,0));
            int retained=doomed.Destruction.LocalizedBreaches;doomed.Hull=0;ShipDestroyed(doomed,true);var wreck=wrecks.Single();
            Check(wreck.GetComponentsInChildren<HullDamageSkin>().Sum(s=>s.MarkCount)==retained && wreck.BlastPath.Count==5 && wreck.BlastPath.Distinct().Count()==5,"Wreck retains impact scars and routes five secondary explosions through distinct real hull surfaces",checks);
            TogglePause();wreck.Tick(1);Check(wreck.SecondaryBlasts==0,"Pause freezes the spreading hull-explosion sequence",checks);TogglePause();
            wreck.Tick(1.6f);Check(wreck.SecondaryBlasts==5 && !wreck.Complete && wreck.GetComponentsInChildren<HullDamageSkin>().Sum(s=>s.MarkCount)>retained && wreck.FragmentCount<=18,"Secondary blasts spread local cutaways and bounded smaller fragments before the final detonation",checks);
            wreck.Tick(1.1f);Check(wreck.Complete && wreck.Parts.Count==3 && wreck.GetComponentsInChildren<HullDamageSkin>().All(s=>s.InnerRenderer!=null && s.InnerRenderer.gameObject.activeSelf),"Final breakup carries visible damaged plating into three slowly drifting wreck groups",checks);
            ClearBattle();
        }
        IEnumerator LocalizedDestructionPreview()
        {
            Begin(Faction.Republic);ClearBattle();Salvage=1800;Wave=6;
            var ship=Spawn(Faction.Republic,ShipClass.Flagship,true,true,Vector3.zero);ship.Selected=true;ship.Shield=0;
            ship.transform.rotation=Quaternion.Euler(0,220,0);var sections=ship.GetComponentsInChildren<HullSection>();
            foreach(var section in sections.Where((s,i)=>i==1 || i==3 || i==5))ship.Damage(190,SurfacePoint(section,7));
            cameraFocus=Vector3.zero;cameraDistance=35;PositionCamera();
            Notify("LOCALIZED DAMAGE / charred impact edges, retained inner plating and smaller armor fractures");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-localized-damage-preview.png"));
            cameraDistance=25;PositionCamera();HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-breach-detail-preview.png"),3200,1800,false,true);
            Begin(Faction.CIS);ClearBattle();Wave=6;
            var survivor=Spawn(Faction.CIS,ShipClass.Flagship,true,true,new Vector3(-30,0,-25));
            var source=Spawn(Faction.CIS,ShipClass.Destroyer,true,false,Vector3.zero);source.Shield=0;
            source.transform.rotation=Quaternion.Euler(0,55,0);var hull=source.GetComponentsInChildren<HullSection>();source.Damage(100,SurfacePoint(hull[3],6));source.Hull=0;ShipDestroyed(source,true);
            var wreck=wrecks.Single();wreck.Tick(1.1f);cameraFocus=Vector3.zero;cameraDistance=35;PositionCamera();
            Notify("SPREADING SECONDARY BLASTS / explosions follow the damaged hull into neighboring sections");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-secondary-damage-preview.png"));
            wreck.Tick(2);wreck.Drift(8);
            Notify("RETAINED WRECK / exposed inner plating and fractured armor drift apart gently");
            yield return new WaitForSeconds(1.2f);
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-fractured-wreck-preview.png"));
        }
    }
}
