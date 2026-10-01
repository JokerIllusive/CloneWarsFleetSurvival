using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetWreck : MonoBehaviour
    {
        FleetGame game;
        HullSection[] sections;
        readonly HashSet<Mesh> released=new HashSet<Mesh>();
        readonly List<Transform> parts=new List<Transform>();
        readonly List<Vector3> partVelocities=new List<Vector3>(),partSpins=new List<Vector3>();
        readonly List<Vector3> blastPath=new List<Vector3>();
        public IReadOnlyList<Vector3> BlastPath => blastPath;
        public int SecondaryBlasts => blasts;
        public IReadOnlyList<Transform> Parts => parts;
        Transform glow;
        LineRenderer danger;
        float age,nextBlast=.18f,radius,blastDamage;
        int blasts,fragments;
        public bool Meltdown { get; private set; }
        public bool Complete { get; private set; }
        public float Radius => radius;
        public float BlastRadius => radius*2.8f;
        public float Countdown => Mathf.Max(0,(Meltdown?2.6f:1.8f)-age);
        public int FragmentCount => fragments;
        public static FleetWreck Create(FleetGame owner,FleetShip source,bool meltdown)
        {
            var root=new GameObject("Persistent "+source.Stats.Name+" wreck",typeof(FleetWreck));owner.AddCombatEffect(root);
            root.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            var wreck=root.GetComponent<FleetWreck>();wreck.game=owner;wreck.radius=source.Stats.Radius;wreck.Meltdown=meltdown;
            wreck.blastDamage=110+source.Stats.Radius*12;
            var hull=Instantiate(source.VisualRoot,root.transform,false);hull.name="Scorched retained hull";
            wreck.sections=hull.GetComponentsInChildren<HullSection>();
            foreach(var skin in hull.GetComponentsInChildren<HullDamageSkin>())if(skin.enabled && skin.MarkCount>0)
            {
                skin.RestoreAfterClone();skin.WreckTone();var section=skin.GetComponent<HullSection>();
                for(int i=0;i<skin.MarkCount;i++)wreck.released.Add(section.BreachFragments[skin.PatchIndices[i]]);
            }
            foreach(var renderer in hull.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.GetComponentInParent<HullDamageSkin>()!=null)continue;
                var materials=renderer.sharedMaterials;
                for(int sub=0;sub<materials.Length;sub++)
                {
                    var tint=new MaterialPropertyBlock();
                    foreach(var property in new[]{"_Color","_BaseColor","baseColorFactor"}) if(materials[sub].HasProperty(property)) tint.SetColor(property,materials[sub].GetColor(property)*new Color(.19f,.16f,.14f));
                    if(materials[sub].HasProperty("emissiveFactor")) tint.SetColor("emissiveFactor",Color.black);
                    renderer.SetPropertyBlock(tint,sub);
                }
            }
            wreck.BuildBlastPath(source.Destruction.LastImpact);
            var template=GameObject.CreatePrimitive(PrimitiveType.Sphere);template.SetActive(false);
            var core=new GameObject("Exposed reactor glow",typeof(MeshFilter),typeof(MeshRenderer));
            core.GetComponent<MeshFilter>().sharedMesh=template.GetComponent<MeshFilter>().sharedMesh;Destroy(template);
            core.transform.SetParent(root.transform,false);core.transform.localPosition=Vector3.up*.8f;core.transform.localScale=Vector3.one*Mathf.Clamp(source.Stats.Radius*.2f,.35f,.7f);
            core.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(new Color(1,.28f,.035f),true);wreck.glow=core.transform;
            core.SetActive(meltdown);
            if(meltdown)
            {
                wreck.danger=ShipVisuals.Ring(root.transform,wreck.BlastRadius,new Color(.8f,.25f,.05f),.1f,"Reactor blast radius");
                owner.Notify("REACTOR FAILURE: "+source.Stats.Name+" wreck. Move ships outside its orange blast ring.");
            }
            var drift=root.AddComponent<FleetDebris>();drift.Game=owner;drift.Lifetime=18;drift.Velocity=source.transform.forward*Mathf.Min(source.CurrentSpeed*.05f,.25f);drift.Spin=Vector3.up*.3f;
            return wreck;
        }
        void Update() { Drift(Time.deltaTime); }
        public void Drift(float dt)
        {
            if(!Complete || game==null || game.Paused || game.Phase==BattlePhase.Menu) return;
            for(int i=0;i<parts.Count;i++)
            {
                parts[i].localPosition+=partVelocities[i]*dt;
                parts[i].localRotation=Quaternion.Euler(partSpins[i]*dt)*parts[i].localRotation;
            }
        }
        public void Tick(float dt)
        {
            if(Complete || game==null || game.Paused) return;
            age+=dt;
            if(Meltdown && glow!=null) glow.localScale=Vector3.one*Mathf.Clamp(radius*.2f,.35f,.7f)*(1+.2f*Mathf.Sin(age*14)+.5f*age/2.6f);
            while(age>=nextBlast && blasts<5)
            {
                Vector3 point=transform.TransformPoint(blastPath[blasts]);
                var section=sections.OrderBy(s=>s.GetComponent<MeshRenderer>().bounds.SqrDistance(point)).FirstOrDefault();
                if(section!=null && section.InnerHull!=null)
                {
                    var skin=section.GetComponent<HullDamageSkin>();if(skin==null)skin=section.gameObject.AddComponent<HullDamageSkin>();
                    if(skin.AddImpact(point))skin.WreckTone();
                }
                game.Burst(point,radius*.22f);ReleaseFragments(point,2);blasts++;nextBlast+=.33f;
            }
            if(Countdown>0) return;
            Complete=true;
            if(Meltdown) { game.Burst(transform.position,radius*1.45f);game.ReactorBlast(transform.position,BlastRadius,blastDamage); }
            else game.Burst(transform.position,radius*.55f);
            ReleaseFragments(transform.position,8);
            SplitHusk();
            if(glow!=null) glow.gameObject.SetActive(false);if(danger!=null) danger.enabled=false;
        }
        void BuildBlastPath(Vector3 initial)
        {
            var unused=new List<HullSection>(sections);Vector3 previous=transform.TransformPoint(initial);
            for(int i=0;i<5;i++)
            {
                HullSection best=null;Vector3 point=previous;float distance=float.MaxValue;
                foreach(var section in unused)
                {
                    var candidates=section.BreachCenters;
                    if(candidates==null || candidates.Length==0)candidates=new[]{Vector3.zero};
                    foreach(var local in candidates){var world=section.transform.TransformPoint(local);float d=(world-previous).sqrMagnitude;if(d<distance){distance=d;best=section;point=world;}}
                }
                blastPath.Add(transform.InverseTransformPoint(point));previous=point;if(best!=null)unused.Remove(best);
            }
        }
        void SplitHusk()
        {
            var ordered=sections.OrderBy(s=>transform.InverseTransformPoint(s.GetComponent<MeshRenderer>().bounds.center).z).ToArray();
            int count=Mathf.Min(3,ordered.Length);
            for(int i=0;i<count;i++)
            {
                int start=i*ordered.Length/count,end=(i+1)*ordered.Length/count;
                Vector3 center=Vector3.zero;for(int n=start;n<end;n++) center+=ordered[n].GetComponent<MeshRenderer>().bounds.center;center/=(end-start);
                var part=new GameObject("Slowly drifting wreck section "+(i+1)).transform;part.SetParent(transform,false);part.position=center;
                for(int n=start;n<end;n++) ordered[n].transform.SetParent(part,true);
                parts.Add(part);
                float side=i%2==0?1:-1;
                partVelocities.Add(new Vector3(side*.09f,side*.025f,(i-(count-1)*.5f)*.32f));
                partSpins.Add(new Vector3(side*.5f,(i-(count-1)*.5f)*1.3f,.3f));
            }
        }
        void ReleaseFragments(Vector3 point,int count)
        {
            foreach(var section in sections.OrderBy(s=>(s.transform.position-point).sqrMagnitude))
            {
                var meshes=section.BreachFragments??section.ArmorFragments;
                var centers=section.BreachCenters??section.FragmentCenters;
                if(meshes==null || centers==null) continue;
                var order=Enumerable.Range(0,meshes.Length).OrderBy(i=>(section.transform.TransformPoint(centers[i])-point).sqrMagnitude);
                foreach(int i in order)
                {
                    if(count<=0 || fragments>=18)break;
                    var mesh=meshes[i];if(mesh==null || !released.Add(mesh)) continue;
                    var shard=new GameObject("Fractured wreck armor",typeof(MeshFilter),typeof(MeshRenderer),typeof(FleetDebris));game.AddCombatEffect(shard);shard.layer=section.gameObject.layer;
                    shard.transform.SetPositionAndRotation(section.transform.TransformPoint(centers[i]),section.transform.rotation);shard.transform.localScale=section.transform.lossyScale*.85f;
                    shard.GetComponent<MeshFilter>().sharedMesh=mesh;var skin=section.GetComponent<HullDamageSkin>();shard.GetComponent<MeshRenderer>().sharedMaterials=skin!=null?skin.OriginalMaterials:section.GetComponent<MeshRenderer>().sharedMaterials;
                    var drift=shard.GetComponent<FleetDebris>();drift.Game=game;drift.Lifetime=8;
                    drift.Velocity=(shard.transform.position-transform.position).normalized*Random.Range(.35f,.65f)+Random.insideUnitSphere*.08f;drift.Spin=Random.onUnitSphere*Random.Range(3,8);
                    fragments++;count--;
                }
                if(count<=0 || fragments>=18) break;
            }
        }
    }
}
