using System.Collections.Generic;
using UnityEngine;

namespace FleetSurvival
{
    // Each ship stores only a few local impact points; hull, lining and shard meshes are shared assets.
    public sealed class HullDamageSkin : MonoBehaviour
    {
        static readonly Dictionary<Material,Material> outerMaterials=new Dictionary<Material,Material>(),innerMaterials=new Dictionary<Material,Material>();
        public Material[] OriginalMaterials;
        public MeshRenderer InnerRenderer;
        public Vector4[] Marks=new Vector4[4];
        public int MarkCount;
        public int[] PatchIndices=new int[4];
        public Vector3 LatestPoint { get; private set; }
        public Vector3 LatestNormal { get; private set; }
        public float LatestRadius { get; private set; }
        public bool AddImpact(Vector3 impact)
        {
            var section=GetComponent<HullSection>();
            if(section.BreachFragments==null || section.InnerHull==null || MarkCount>=4)return false;
            var local=transform.InverseTransformPoint(impact);
            for(int i=0;i<MarkCount;i++)if(Vector3.Distance(local,new Vector3(Marks[i].x,Marks[i].y,Marks[i].z))<Marks[i].w*.9f)return false;
            int chosen=-1;float nearest=float.MaxValue;
            for(int i=0;i<section.BreachFragments.Length;i++)
            {
                if(section.BreachFragments[i]==null)continue;bool used=false;
                for(int m=0;m<MarkCount;m++)used|=PatchIndices[m]==i;
                float distance=(section.BreachCenters[i]-local).sqrMagnitude;
                if(!used && distance<nearest){chosen=i;nearest=distance;}
            }
            if(chosen<0)return false;
            LatestPoint=section.BreachCenters[chosen];LatestNormal=section.BreachNormals[chosen];LatestRadius=section.BreachRadii[chosen];
            for(int i=0;i<MarkCount;i++)if(Vector3.Distance(LatestPoint,new Vector3(Marks[i].x,Marks[i].y,Marks[i].z))<LatestRadius*.9f)return false;
            Marks[MarkCount]=new Vector4(LatestPoint.x,LatestPoint.y,LatestPoint.z,LatestRadius);PatchIndices[MarkCount]=chosen;MarkCount++;
            RestoreAfterClone();return true;
        }
        static Material DamagedMaterial(Material original,bool inner)
        {
            var cache=inner?innerMaterials:outerMaterials;if(cache.TryGetValue(original,out var material))return material;
            var shader=Resources.Load<Shader>("FleetDamagedHull");
            material=new Material(shader){name=original.name+(inner?" inner plating":" localized damage")};material.CopyPropertiesFromMaterial(original);
            if(!original.HasProperty("baseColorFactor")){material.SetColor("baseColorFactor",original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);if(original.HasProperty("_MainTex"))material.SetTexture("baseColorTexture",original.GetTexture("_MainTex"));}
            material.SetFloat("_HasNormal",original.HasProperty("normalTexture") && original.GetTexture("normalTexture")!=null?1:0);
            material.SetFloat("_Inner",inner?1:0);material.SetFloat("_WreckTint",1);cache[original]=material;return material;
        }
        public void RestoreAfterClone()
        {
            var section=GetComponent<HullSection>();var renderer=GetComponent<MeshRenderer>();
            if(OriginalMaterials==null || OriginalMaterials.Length==0)OriginalMaterials=renderer.sharedMaterials;
            var materials=new Material[OriginalMaterials.Length];var inner=new Material[OriginalMaterials.Length];
            for(int i=0;i<materials.Length;i++){materials[i]=DamagedMaterial(OriginalMaterials[i],false);inner[i]=DamagedMaterial(OriginalMaterials[i],true);}
            renderer.sharedMaterials=materials;
            if(InnerRenderer==null)
            {
                var lining=new GameObject("Retained exposed inner plating",typeof(MeshFilter),typeof(MeshRenderer));lining.transform.SetParent(transform,false);
                lining.layer=gameObject.layer;lining.GetComponent<MeshFilter>().sharedMesh=section.InnerHull;InnerRenderer=lining.GetComponent<MeshRenderer>();
            }
            InnerRenderer.sharedMaterials=inner;
            for(int sub=0;sub<materials.Length;sub++){renderer.SetPropertyBlock(null,sub);InnerRenderer.SetPropertyBlock(null,sub);}
            Apply(renderer,1);Apply(InnerRenderer,1);
        }
        void Apply(MeshRenderer renderer,float wreckTint)
        {
            for(int sub=0;sub<renderer.sharedMaterials.Length;sub++)
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,sub);block.SetFloat("_BreachCount",MarkCount);block.SetFloat("_WreckTint",wreckTint);
                for(int i=0;i<4;i++)block.SetVector("_Breach"+i,Marks[i]);renderer.SetPropertyBlock(block,sub);
            }
        }
        public void WreckTone(){Apply(GetComponent<MeshRenderer>(),.35f);if(InnerRenderer!=null)Apply(InnerRenderer,.7f);}
        public void RestoreIntact()
        {
            GetComponent<MeshRenderer>().sharedMaterials=OriginalMaterials;
            if(InnerRenderer!=null){InnerRenderer.gameObject.SetActive(false);Destroy(InnerRenderer.gameObject);InnerRenderer=null;}
            MarkCount=0;enabled=false;Destroy(this);
        }
        public void ReleaseShard(FleetGame game)
        {
            var section=GetComponent<HullSection>();int i=PatchIndices[MarkCount-1];
            var shard=new GameObject("Localized fractured armor",typeof(MeshFilter),typeof(MeshRenderer),typeof(FleetDebris));game.AddCombatEffect(shard);shard.layer=gameObject.layer;
            shard.transform.SetPositionAndRotation(transform.TransformPoint(LatestPoint),transform.rotation);shard.transform.localScale=transform.lossyScale*.92f;
            shard.GetComponent<MeshFilter>().sharedMesh=section.BreachFragments[i];shard.GetComponent<MeshRenderer>().sharedMaterials=OriginalMaterials;
            var debris=shard.GetComponent<FleetDebris>();debris.Game=game;debris.Lifetime=7;
            debris.Velocity=transform.TransformDirection(LatestNormal)*Random.Range(.7f,1.3f)+Random.insideUnitSphere*.15f;debris.Spin=Random.onUnitSphere*Random.Range(4,10);
        }
    }
}
