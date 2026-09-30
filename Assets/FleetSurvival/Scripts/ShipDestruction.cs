using System.Collections.Generic;
using UnityEngine;

namespace FleetSurvival
{
    public sealed class ShipDestruction : MonoBehaviour
    {
        FleetShip ship;
        readonly List<HullSection> sections=new List<HullSection>();
        readonly Dictionary<HullSection,Vector3> originalPositions=new Dictionary<HullSection,Vector3>();
        readonly List<GameObject> fires=new List<GameObject>();
        readonly HashSet<HullSection> scorched=new HashSet<HullSection>();
        int damageStage;
        public int DamagedSections { get; private set; }
        public float Mobility => damageStage>=2?.65f:1;
        public float Firepower => damageStage>=1?.85f:1;
        public void Initialize(FleetShip owner)
        {
            ship=owner;
            sections.AddRange(GetComponentsInChildren<HullSection>());
            foreach(var section in sections) originalPositions[section]=section.transform.localPosition;
        }
        public void HullHit(Vector3 impact)
        {
            int stage=ship.Hull/ship.MaxHull<.15f?3:ship.Hull/ship.MaxHull<.35f?2:ship.Hull/ship.MaxHull<.65f?1:0;
            if(stage<=damageStage) return;
            while(damageStage<stage)
            {
                damageStage++;
                HullSection closest=null; float best=float.MaxValue;
                foreach(var section in sections)
                {
                    if(section==null || !section.gameObject.activeSelf || scorched.Contains(section)) continue;
                    float distance=(section.transform.position-impact).sqrMagnitude;
                    if(distance<best) { closest=section; best=distance; }
                }
                if(closest!=null)
                {
                    SpawnArmorFragments(closest,impact); scorched.Add(closest); DamagedSections++;
                    var renderer=closest.GetComponent<MeshRenderer>(); var materials=renderer.sharedMaterials;
                    for(int sub=0;sub<materials.Length;sub++)
                    {
                        var tint=new MaterialPropertyBlock(); var burn=new Color(.3f,.24f,.2f);
                        foreach(var property in new[]{"_Color","_BaseColor","baseColorFactor"})
                            if(materials[sub].HasProperty(property)) tint.SetColor(property,materials[sub].GetColor(property)*burn);
                        if(materials[sub].HasProperty("emissiveFactor")) tint.SetColor("emissiveFactor",Color.black);
                        renderer.SetPropertyBlock(tint,sub);
                    }
                    Vector3 breach=renderer.bounds.ClosestPoint(impact); breach.y=renderer.bounds.max.y+.15f;
                    FireAt(transform.InverseTransformPoint(breach));
                }
            }
        }
        public void BreakApart()
        {
            foreach(var section in sections) if(section!=null && section.gameObject.activeSelf) { SpawnFragment(section); section.gameObject.SetActive(false); }
        }
        public void BreakModel(Transform model)
        {
            foreach(var section in model.GetComponentsInChildren<HullSection>())
                if(section.gameObject.activeSelf) { SpawnFragment(section); section.gameObject.SetActive(false); }
        }
        void SpawnFragment(HullSection section)
        {
            var filter=section.GetComponent<MeshFilter>(); var renderer=section.GetComponent<MeshRenderer>();
            var fragment=new GameObject("Drifting hull debris",typeof(MeshFilter),typeof(MeshRenderer),typeof(FleetDebris));
            ship.Game.AddCombatEffect(fragment);
            fragment.transform.SetPositionAndRotation(section.transform.position,section.transform.rotation);
            fragment.transform.localScale=section.transform.lossyScale; fragment.layer=section.gameObject.layer;
            fragment.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
            fragment.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            var debris=fragment.GetComponent<FleetDebris>(); debris.Game=ship.Game;
            Vector3 outward=(section.transform.position-ship.transform.position).normalized;
            debris.Velocity=outward*Random.Range(4,8)+Random.insideUnitSphere*1.5f;
            debris.Spin=Random.onUnitSphere*Random.Range(8,28); debris.Lifetime=10;
        }
        void SpawnArmorFragments(HullSection section,Vector3 impact)
        {
            if(section.ArmorFragments==null || section.FragmentCenters==null) return;
            var chosen=new List<int>();
            for(int i=0;i<section.ArmorFragments.Length;i++) if(section.ArmorFragments[i]!=null) chosen.Add(i);
            chosen.Sort((a,b)=>(section.transform.TransformPoint(section.FragmentCenters[a])-impact).sqrMagnitude.CompareTo((section.transform.TransformPoint(section.FragmentCenters[b])-impact).sqrMagnitude));
            for(int n=0;n<Mathf.Min(3,chosen.Count);n++)
            {
                int i=chosen[n]; var go=new GameObject("Armor fragment",typeof(MeshFilter),typeof(MeshRenderer),typeof(FleetDebris));
                ship.Game.AddCombatEffect(go); go.layer=section.gameObject.layer;
                go.transform.SetPositionAndRotation(section.transform.TransformPoint(section.FragmentCenters[i]),section.transform.rotation);
                go.transform.localScale=section.transform.lossyScale*.85f;
                go.GetComponent<MeshFilter>().sharedMesh=section.ArmorFragments[i];
                go.GetComponent<MeshRenderer>().sharedMaterials=section.GetComponent<MeshRenderer>().sharedMaterials;
                var debris=go.GetComponent<FleetDebris>(); debris.Game=ship.Game;
                Vector3 away=(go.transform.position-ship.transform.position).normalized;
                debris.Velocity=away*Random.Range(5,8)+Vector3.up*Random.Range(.6f,1.8f)+Random.insideUnitSphere;
                debris.Spin=Random.onUnitSphere*Random.Range(20,55); debris.Lifetime=6;
            }
        }
        void FireAt(Vector3 local)
        {
            var go=new GameObject("Hull breach fire",typeof(ParticleSystem)); go.transform.SetParent(transform,false); go.transform.localPosition=local;
            var ps=go.GetComponent<ParticleSystem>(); var main=ps.main; main.loop=true; main.startLifetime=.65f;
            main.startSpeed=ship.Stats.Radius*.3f; main.startSize=ship.Stats.Radius*.25f; main.startColor=new Color(1,.4f,.08f);
            main.maxParticles=22; var emission=ps.emission; emission.rateOverTime=14;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=ship.Stats.Radius*.12f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("FleetParticle");
            var fade=ps.colorOverLifetime; fade.enabled=true;
            var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.18f,.02f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)}); fade.color=gradient;
            ps.Play(); fires.Add(go);
        }
        public void Repair()
        {
            foreach(var section in sections) if(section!=null)
            {
                section.gameObject.SetActive(true); section.transform.localPosition=originalPositions[section];
                var renderer=section.GetComponent<MeshRenderer>(); renderer.SetPropertyBlock(null);
                for(int sub=0;sub<renderer.sharedMaterials.Length;sub++) renderer.SetPropertyBlock(null,sub);
            }
            foreach(var fire in fires) if(fire!=null) Destroy(fire); fires.Clear(); damageStage=0; DamagedSections=0;
            scorched.Clear();
        }
    }
    public sealed class FleetDebris : MonoBehaviour
    {
        public FleetGame Game;
        public Vector3 Velocity,Spin;
        public float Lifetime;
        void Update()
        {
            if(Game==null) { Destroy(gameObject); return; }
            if(Game.Phase==BattlePhase.Menu) { Destroy(gameObject); return; }
            if(Game.Paused) return;
            float dt=Time.deltaTime; transform.position+=Velocity*dt; transform.Rotate(Spin*dt,Space.World);
            Lifetime-=dt;
            if(Lifetime<1.2f) transform.localScale*=Mathf.Exp(-dt*2);
            if(Lifetime<=0) Destroy(gameObject);
        }
    }
}
