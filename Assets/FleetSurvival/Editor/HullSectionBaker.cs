using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FleetSurvival.Editor
{
    // Partition the real imported triangles into sections once during authoring.
    // Runtime ships share these meshes, avoiding geometry allocations during combat.
    public static class HullSectionBaker
    {
        sealed class Section
        {
            public readonly List<Vector3> Vertices=new List<Vector3>(), Normals=new List<Vector3>();
            public readonly List<Vector4> Tangents=new List<Vector4>();
            public readonly List<Vector2> UV=new List<Vector2>(), UV2=new List<Vector2>();
            public readonly List<Color> Colors=new List<Color>();
            public readonly Dictionary<long,int> Indices=new Dictionary<long,int>();
            public readonly Dictionary<Material,List<int>> Triangles=new Dictionary<Material,List<int>>();
        }
        public static void Bake(GameObject root,string name)
        {
            string directory="Assets/FleetSurvival/BakedShips/"+name;
            Directory.CreateDirectory(directory); AssetDatabase.Refresh();
            var filters=root.GetComponentsInChildren<MeshFilter>();
            if(filters.Length==0) throw new Exception("No static hull meshes for "+name);
            Bounds bounds=new Bounds(Vector3.zero,Vector3.zero); bool first=true;
            foreach(var filter in filters)
            {
                var renderer=filter.GetComponent<Renderer>(); if(renderer==null) continue;
                if(first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds);
            }
            var sections=new Section[6]; for(int i=0;i<6;i++) sections[i]=new Section();
            var optimizedMaterials=new Dictionary<Material,Material>();
            var optimizedTextures=new Dictionary<Texture,Texture>();
            for(int meshIndex=0;meshIndex<filters.Length;meshIndex++)
            {
                var filter=filters[meshIndex]; var renderer=filter.GetComponent<Renderer>();
                if(renderer==null || filter.sharedMesh==null) continue;
                var mesh=filter.sharedMesh;
                if(!mesh.isReadable) throw new Exception("Mesh is not readable: "+mesh.name);
                var v=mesh.vertices; var n=mesh.normals; var uv=mesh.uv; var uv2=mesh.uv2; var tangent=mesh.tangents; var colors=mesh.colors;
                var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    if(mesh.GetTopology(sub)!=MeshTopology.Triangles) continue;
                    var sourceMaterial=renderer.sharedMaterials[Mathf.Min(sub,renderer.sharedMaterials.Length-1)];
                    if(sourceMaterial==null) continue;
                    if(!optimizedMaterials.TryGetValue(sourceMaterial,out var material))
                    {
                        material=OptimizeMaterial(sourceMaterial,directory,optimizedMaterials.Count,optimizedTextures);
                        optimizedMaterials[sourceMaterial]=material;
                    }
                    int[] indices=mesh.GetTriangles(sub);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        Vector3 centroid=matrix.MultiplyPoint3x4((v[indices[i]]+v[indices[i+1]]+v[indices[i+2]])/3);
                        int longitudinal=Mathf.Clamp((int)(Mathf.InverseLerp(bounds.min.z,bounds.max.z,centroid.z)*3),0,2);
                        var section=sections[longitudinal*2+(centroid.x>=0?1:0)];
                        if(!section.Triangles.TryGetValue(material,out var triangleList)) { triangleList=new List<int>(); section.Triangles[material]=triangleList; }
                        for(int corner=0;corner<3;corner++)
                        {
                            int sourceCorner=matrix.determinant<0 && corner>0?3-corner:corner;
                            int index=indices[i+sourceCorner]; long key=((long)meshIndex<<32)|(uint)index;
                            if(!section.Indices.TryGetValue(key,out int vertexIndex))
                            {
                                vertexIndex=section.Vertices.Count; section.Indices[key]=vertexIndex;
                                section.Vertices.Add(matrix.MultiplyPoint3x4(v[index]));
                                section.Normals.Add(n.Length==v.Length?matrix.MultiplyVector(n[index]).normalized:Vector3.up);
                                section.UV.Add(uv.Length==v.Length?uv[index]:Vector2.zero); section.UV2.Add(uv2.Length==v.Length?uv2[index]:Vector2.zero);
                                section.Colors.Add(colors.Length==v.Length?colors[index]:Color.white);
                                var t=tangent.Length==v.Length?tangent[index]:new Vector4(1,0,0,1);
                                var direction=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;
                                section.Tangents.Add(new Vector4(direction.x,direction.y,direction.z,t.w));
                            }
                            triangleList.Add(vertexIndex);
                        }
                    }
                }
            }
            // Source files remain untouched. The playable prefab uses shared, partitioned meshes.
            for(int i=root.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            for(int i=0;i<sections.Length;i++)
            {
                var section=sections[i]; if(section.Vertices.Count==0) continue;
                var sectionBounds=new Bounds(section.Vertices[0],Vector3.zero); foreach(var vertex in section.Vertices) sectionBounds.Encapsulate(vertex);
                var center=sectionBounds.center;
                for(int j=0;j<section.Vertices.Count;j++) section.Vertices[j]-=center;
                var mesh=new Mesh {name=name+" hull section "+i,indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(section.Vertices); mesh.SetNormals(section.Normals); mesh.SetUVs(0,section.UV); mesh.SetUVs(1,section.UV2); mesh.SetTangents(section.Tangents); mesh.SetColors(section.Colors);
                mesh.subMeshCount=section.Triangles.Count; var materials=new List<Material>(); int sub=0;
                foreach(var pair in section.Triangles) { mesh.SetTriangles(pair.Value,sub++); materials.Add(pair.Key); }
                mesh.RecalculateBounds();
                string path=directory+"/HullSection"+i+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null) AssetDatabase.CreateAsset(mesh,path); else { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
                var piece=new GameObject("HullSection"+i,typeof(MeshFilter),typeof(MeshRenderer),typeof(HullSection));
                piece.transform.SetParent(root.transform,false); piece.transform.localPosition=center;
                piece.GetComponent<MeshFilter>().sharedMesh=mesh; piece.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
                BakeArmorFragments(mesh,materials.ToArray(),directory,i,piece.GetComponent<HullSection>());
            }
            AssetDatabase.SaveAssets(); Debug.Log("HULL_SECTIONS_BAKED "+name+" / "+root.transform.childCount+" sections");
        }
        static void BakeArmorFragments(Mesh source,Material[] materials,string directory,int sectionIndex,HullSection hull)
        {
            var fragments=new Section[8]; for(int i=0;i<fragments.Length;i++) fragments[i]=new Section();
            var vertices=source.vertices; var normals=source.normals; var uv=source.uv; var uv2=source.uv2;
            var tangents=source.tangents; var colors=source.colors;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=source.GetTriangles(sub);
                for(int t=0;t<indices.Length;t+=3)
                {
                    Vector3 midpoint=(vertices[indices[t]]+vertices[indices[t+1]]+vertices[indices[t+2]])/3;
                    int row=Mathf.Clamp((int)(Mathf.InverseLerp(source.bounds.min.z,source.bounds.max.z,midpoint.z)*4),0,3);
                    var fragment=fragments[row*2+(midpoint.x>=source.bounds.center.x?1:0)];
                    if(!fragment.Triangles.TryGetValue(materials[sub],out var triangles)) { triangles=new List<int>(); fragment.Triangles[materials[sub]]=triangles; }
                    for(int c=0;c<3;c++)
                    {
                        int old=indices[t+c];
                        if(!fragment.Indices.TryGetValue(old,out int index))
                        {
                            index=fragment.Vertices.Count; fragment.Indices[old]=index;
                            fragment.Vertices.Add(vertices[old]); fragment.Normals.Add(normals[old]);
                            fragment.UV.Add(uv[old]); fragment.UV2.Add(uv2[old]); fragment.Tangents.Add(tangents[old]); fragment.Colors.Add(colors[old]);
                        }
                        triangles.Add(index);
                    }
                }
            }
            hull.ArmorFragments=new Mesh[8]; hull.FragmentCenters=new Vector3[8];
            for(int i=0;i<fragments.Length;i++)
            {
                var fragment=fragments[i]; if(fragment.Vertices.Count==0) continue;
                var bounds=new Bounds(fragment.Vertices[0],Vector3.zero); foreach(var v in fragment.Vertices) bounds.Encapsulate(v);
                hull.FragmentCenters[i]=bounds.center;
                for(int v=0;v<fragment.Vertices.Count;v++) fragment.Vertices[v]-=bounds.center;
                ThickenArmor(fragment,Mathf.Clamp(bounds.size.magnitude*.012f,.015f,.09f));
                var mesh=new Mesh {name=source.name+" armor "+i,indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(fragment.Vertices); mesh.SetNormals(fragment.Normals); mesh.SetUVs(0,fragment.UV); mesh.SetUVs(1,fragment.UV2); mesh.SetTangents(fragment.Tangents); mesh.SetColors(fragment.Colors);
                mesh.subMeshCount=materials.Length;
                for(int sub=0;sub<materials.Length;sub++) mesh.SetTriangles(fragment.Triangles.TryGetValue(materials[sub],out var triangles)?triangles:new List<int>(),sub);
                mesh.RecalculateBounds();
                string path=directory+"/Armor"+sectionIndex+"_"+i+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null) AssetDatabase.CreateAsset(mesh,path); else { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
                hull.ArmorFragments[i]=mesh;
            }
        }
        // Duplicate the inner skin and cap open borders so armor has solid edges in flight.
        static void ThickenArmor(Section fragment,float thickness)
        {
            int count=fragment.Vertices.Count;
            var edges=new Dictionary<ulong,(int a,int b,int count,Material material)>();
            foreach(var pair in fragment.Triangles)
            {
                var indices=pair.Value;int original=indices.Count;
                for(int t=0;t<original;t+=3)
                {
                    int a=indices[t],b=indices[t+1],c=indices[t+2];
                    indices.Add(c+count);indices.Add(b+count);indices.Add(a+count);
                    foreach(var edge in new[]{(a,b),(b,c),(c,a)})
                    {
                        ulong key=((ulong)(uint)Mathf.Min(edge.Item1,edge.Item2)<<32)|(uint)Mathf.Max(edge.Item1,edge.Item2);
                        if(edges.TryGetValue(key,out var previous)) edges[key]=(previous.a,previous.b,previous.count+1,previous.material);
                        else edges[key]=(edge.Item1,edge.Item2,1,pair.Key);
                    }
                }
            }
            for(int i=0;i<count;i++)
            {
                fragment.Vertices.Add(fragment.Vertices[i]-fragment.Normals[i].normalized*thickness);fragment.Normals.Add(-fragment.Normals[i]);
                fragment.UV.Add(fragment.UV[i]);fragment.UV2.Add(fragment.UV2[i]);fragment.Tangents.Add(fragment.Tangents[i]);fragment.Colors.Add(fragment.Colors[i]*new Color(.25f,.25f,.25f,1));
            }
            foreach(var edge in edges.Values) if(edge.count==1)
            {
                var triangles=fragment.Triangles[edge.material];
                triangles.Add(edge.a);triangles.Add(edge.a+count);triangles.Add(edge.b+count);
                triangles.Add(edge.a);triangles.Add(edge.b+count);triangles.Add(edge.b);
            }
        }
        static Material OptimizeMaterial(Material source,string directory,int index,Dictionary<Texture,Texture> textures)
        {
            string path=directory+"/Material"+index+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(source); AssetDatabase.CreateAsset(material,path); }
            else material.CopyPropertiesFromMaterial(source);
            foreach(var property in source.GetTexturePropertyNames())
            {
                var texture=source.GetTexture(property); if(texture==null) continue;
                if(!textures.TryGetValue(texture,out var optimized))
                {
                    int width=texture.width,height=texture.height;
                    float factor=Mathf.Min(1,1024f/Mathf.Max(width,height));
                    width=Mathf.Max(4,(int)(width*factor)); height=Mathf.Max(4,(int)(height*factor));
                    string texturePath=directory+"/Texture"+textures.Count+".asset";
                    var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if(existing!=null) optimized=existing;
                    else
                    {
                        bool srgb=UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat);
                        var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,srgb?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
                        var previous=RenderTexture.active; Graphics.Blit(texture,target); RenderTexture.active=target;
                        var resized=new Texture2D(width,height,TextureFormat.RGBA32,true,!srgb) {name=source.name+"_"+property,filterMode=FilterMode.Trilinear,anisoLevel=2,wrapMode=texture.wrapMode};
                        resized.ReadPixels(new Rect(0,0,width,height),0,0); resized.Apply(true); resized.Compress(true); resized.Apply(false,true);
                        RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); AssetDatabase.CreateAsset(resized,texturePath); optimized=resized;
                    }
                    textures[texture]=optimized;
                }
                material.SetTexture(property,optimized);
            }
            EditorUtility.SetDirty(material); return material;
        }
    }
}
