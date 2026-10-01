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
        public int LocalizedBreaches { get; private set; }
        public Vector3 LastImpact { get; private set; }
        public float Mobility => damageStage>=2?.65f:1;
        public float Firepower => damageStage>=1?.85f:1;
        public string Status => damageStage>=3?"CRITICAL":damageStage>=2?"ENGINES DAMAGED":damageStage>=1?"HULL DAMAGED":"INTACT";
        public void Initialize(FleetShip owner)
        {
            ship=owner;
            sections.AddRange(GetComponentsInChildren<HullSection>());
            foreach(var section in sections) originalPositions[section]=section.transform.localPosition;
        }
        public void HullHit(Vector3 impact,float damage=0)
        {
            int stage=ship.Hull/ship.MaxHull<.15f?3:ship.Hull/ship.MaxHull<.35f?2:ship.Hull/ship.MaxHull<.65f?1:0;
            damageStage=Mathf.Max(damageStage,stage);LastImpact=transform.InverseTransformPoint(impact);
            if(LocalizedBreaches>=8 || (damage>0 && damage<ship.MaxHull*.015f))return;
            HullSection closest=null; float best=float.MaxValue;
            foreach(var section in sections)
            {
                if(section==null || !section.gameObject.activeSelf || section.BreachCenters==null)continue;
                foreach(var center in section.BreachCenters){float distance=(section.transform.TransformPoint(center)-impact).sqrMagnitude;if(distance<best){closest=section;best=distance;}}
            }
            if(closest==null)return;
            var skin=closest.GetComponent<HullDamageSkin>();if(skin==null || !skin.enabled)skin=closest.gameObject.AddComponent<HullDamageSkin>();
            if(!skin.AddImpact(impact))return;
            skin.ReleaseShard(ship.Game);LocalizedBreaches++;if(scorched.Add(closest))DamagedSections++;
            FireAt(closest.transform,skin.LatestPoint+skin.LatestNormal*.08f);
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
        void FireAt(Transform parent,Vector3 local)
        {
            var go=new GameObject("Hull breach fire",typeof(ParticleSystem)); go.transform.SetParent(parent,false); go.transform.localPosition=local;
            var ps=go.GetComponent<ParticleSystem>(); var main=ps.main; main.loop=true; main.startLifetime=.65f;
            main.startSpeed=ship.Stats.Radius*.2f; main.startSize=Mathf.Clamp(ship.Stats.Radius*.1f,.25f,.6f); main.startColor=new Color(1,.4f,.08f);
            main.maxParticles=12; var emission=ps.emission; emission.rateOverTime=7;
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
                var skin=section.GetComponent<HullDamageSkin>();if(skin!=null)skin.RestoreIntact();
                var renderer=section.GetComponent<MeshRenderer>(); renderer.SetPropertyBlock(null);
                for(int sub=0;sub<renderer.sharedMaterials.Length;sub++) renderer.SetPropertyBlock(null,sub);
            }
            foreach(var fire in fires) if(fire!=null){fire.SetActive(false);Destroy(fire);} fires.Clear(); damageStage=0; DamagedSections=0;LocalizedBreaches=0;
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
