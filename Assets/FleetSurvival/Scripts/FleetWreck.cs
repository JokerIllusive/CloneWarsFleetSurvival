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
        Transform glow;
        LineRenderer danger;
        float age,nextBlast=.18f,radius,blastDamage;
        int blasts,fragments;
        public bool Meltdown { get; private set; }
        public bool Complete { get; private set; }
        public float Radius => radius;
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
            foreach(var renderer in hull.GetComponentsInChildren<MeshRenderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int sub=0;sub<materials.Length;sub++)
                {
                    var tint=new MaterialPropertyBlock();
                    foreach(var property in new[]{"_Color","_BaseColor","baseColorFactor"}) if(materials[sub].HasProperty(property)) tint.SetColor(property,materials[sub].GetColor(property)*new Color(.19f,.16f,.14f));
                    if(materials[sub].HasProperty("emissiveFactor")) tint.SetColor("emissiveFactor",Color.black);
                    renderer.SetPropertyBlock(tint,sub);
                }
            }
            var template=GameObject.CreatePrimitive(PrimitiveType.Sphere);template.SetActive(false);
            var core=new GameObject("Exposed reactor glow",typeof(MeshFilter),typeof(MeshRenderer));
            core.GetComponent<MeshFilter>().sharedMesh=template.GetComponent<MeshFilter>().sharedMesh;Destroy(template);
            core.transform.SetParent(root.transform,false);core.transform.localPosition=Vector3.up*.8f;core.transform.localScale=Vector3.one*source.Stats.Radius*.35f;
            core.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(new Color(1,.28f,.035f),true);wreck.glow=core.transform;
            core.SetActive(meltdown);
            if(meltdown)
            {
                wreck.danger=ShipVisuals.Ring(root.transform,source.Stats.Radius*2.8f,new Color(.8f,.25f,.05f),.1f,"Reactor blast radius");
                owner.Notify("REACTOR FAILURE: "+source.Stats.Name+" wreck. Move ships outside its orange blast ring.");
            }
            var drift=root.AddComponent<FleetDebris>();drift.Game=owner;drift.Lifetime=18;drift.Velocity=source.transform.forward*source.CurrentSpeed*.1f;drift.Spin=Vector3.up*.7f;
            return wreck;
        }
        public void Tick(float dt)
        {
            if(Complete || game==null || game.Paused) return;
            age+=dt;
            if(Meltdown && glow!=null) glow.localScale=Vector3.one*radius*(.35f+.12f*Mathf.Sin(age*14)+.18f*age/2.6f);
            while(age>=nextBlast && blasts<5)
            {
                Vector3 point=transform.TransformPoint(new Vector3(Mathf.Sin(blasts*2.1f)*radius*.6f,.5f,Mathf.Cos(blasts*1.7f)*radius*.7f));
                game.Burst(point,radius*.22f);ReleaseFragments(point,2);blasts++;nextBlast+=.33f;
            }
            if(Countdown>0) return;
            Complete=true;
            if(Meltdown) { game.Burst(transform.position,radius*1.45f);game.ReactorBlast(transform.position,radius*2.8f,blastDamage); }
            else game.Burst(transform.position,radius*.55f);
            ReleaseFragments(transform.position,8);
            if(glow!=null) glow.gameObject.SetActive(false);if(danger!=null) danger.enabled=false;
        }
        void ReleaseFragments(Vector3 point,int count)
        {
            foreach(var section in sections.OrderBy(s=>(s.transform.position-point).sqrMagnitude))
            {
                if(section.ArmorFragments==null) continue;
                for(int i=0;i<section.ArmorFragments.Length && count>0 && fragments<18;i++)
                {
                    var mesh=section.ArmorFragments[i];if(mesh==null || !released.Add(mesh)) continue;
                    var shard=new GameObject("Fractured wreck armor",typeof(MeshFilter),typeof(MeshRenderer),typeof(FleetDebris));game.AddCombatEffect(shard);shard.layer=section.gameObject.layer;
                    shard.transform.SetPositionAndRotation(section.transform.TransformPoint(section.FragmentCenters[i]),section.transform.rotation);shard.transform.localScale=section.transform.lossyScale*.85f;
                    shard.GetComponent<MeshFilter>().sharedMesh=mesh;shard.GetComponent<MeshRenderer>().sharedMaterials=section.GetComponent<MeshRenderer>().sharedMaterials;
                    var drift=shard.GetComponent<FleetDebris>();drift.Game=game;drift.Lifetime=8;
                    drift.Velocity=(shard.transform.position-transform.position).normalized*Random.Range(2.5f,6)+Random.insideUnitSphere*1.5f;drift.Spin=Random.onUnitSphere*Random.Range(16,48);
                    fragments++;count--;
                }
                if(count<=0 || fragments>=18) break;
            }
        }
    }
}
