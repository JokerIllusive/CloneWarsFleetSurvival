using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FleetSurvival.Editor
{
    public static partial class HullSectionBaker
    {
        static Mesh SaveDamageMesh(Mesh mesh,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);return existing;
        }
        static void BakeBreaches(Mesh source,Material[] materials,string directory,int sectionIndex,HullSection hull)
        {
            var v=source.vertices;var n=source.normals;var uv=source.uv;var uv2=source.uv2;var tangent=source.tangents;var colors=source.colors;
            var triangles=new int[source.subMeshCount][];
            for(int sub=0;sub<triangles.Length;sub++)triangles[sub]=source.GetTriangles(sub);
            var inner=Object.Instantiate(source);inner.name=source.name+" retained inner plating";
            var innerVertices=(Vector3[])v.Clone();float inset=Mathf.Clamp(source.bounds.size.y*.1f,.035f,.1f);
            for(int i=0;i<v.Length;i++)innerVertices[i]-=n[i]*inset;
            inner.vertices=innerVertices;inner.RecalculateBounds();hull.InnerHull=SaveDamageMesh(inner,directory+"/InnerHull"+sectionIndex+".asset");
            const int count=12;hull.BreachFragments=new Mesh[count];hull.BreachCenters=new Vector3[count];hull.BreachNormals=new Vector3[count];hull.BreachRadii=new float[count];
            float radius=Mathf.Clamp(source.bounds.size.magnitude*.12f,.28f,.7f);
            for(int patch=0;patch<count;patch++)
            {
                var desired=new Vector3(Mathf.Lerp(source.bounds.min.x,source.bounds.max.x,(patch%3+.5f)/3),source.bounds.max.y,Mathf.Lerp(source.bounds.min.z,source.bounds.max.z,(patch/3+.5f)/4));
                Vector3 center=source.bounds.center,normal=Vector3.up;float best=float.MaxValue;
                foreach(var indices in triangles)for(int t=0;t<indices.Length;t+=3)
                {
                    int a=indices[t],b=indices[t+1],c=indices[t+2];var p=(v[a]+v[b]+v[c])/3;
                    float distance=(p-desired).sqrMagnitude;
                    if(distance>=best)continue;best=distance;center=p;normal=(n[a]+n[b]+n[c]).normalized;
                }
                hull.BreachCenters[patch]=center;hull.BreachNormals[patch]=normal;hull.BreachRadii[patch]=radius;
                var fragment=new Section();
                for(int sub=0;sub<triangles.Length;sub++)
                {
                    var indices=triangles[sub];var list=new List<int>();fragment.Triangles[materials[sub]]=list;
                    for(int t=0;t<indices.Length;t+=3)
                    {
                        int a=indices[t],b=indices[t+1],c=indices[t+2];var p=(v[a]+v[b]+v[c])/3;
                        if((p-center).sqrMagnitude>radius*radius || Vector3.Dot((n[a]+n[b]+n[c]).normalized,normal)<-.2f)continue;
                        for(int corner=0;corner<3;corner++)
                        {
                            int old=indices[t+corner];if(!fragment.Indices.TryGetValue(old,out int index))
                            {
                                index=fragment.Vertices.Count;fragment.Indices[old]=index;fragment.Vertices.Add(v[old]-center);fragment.Normals.Add(n[old]);
                                fragment.UV.Add(uv[old]);fragment.UV2.Add(uv2[old]);fragment.Tangents.Add(tangent[old]);fragment.Colors.Add(colors[old]);
                            }
                            list.Add(index);
                        }
                    }
                }
                if(fragment.Vertices.Count==0)continue;
                ThickenArmor(fragment,inset*.7f);
                var mesh=new Mesh{name=source.name+" localized armor "+patch,indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(fragment.Vertices);mesh.SetNormals(fragment.Normals);mesh.SetUVs(0,fragment.UV);mesh.SetUVs(1,fragment.UV2);mesh.SetTangents(fragment.Tangents);mesh.SetColors(fragment.Colors);mesh.subMeshCount=materials.Length;
                for(int sub=0;sub<materials.Length;sub++)mesh.SetTriangles(fragment.Triangles[materials[sub]],sub);
                mesh.RecalculateBounds();hull.BreachFragments[patch]=SaveDamageMesh(mesh,directory+"/Breach"+sectionIndex+"_"+patch+".asset");
            }
        }
    }
}
