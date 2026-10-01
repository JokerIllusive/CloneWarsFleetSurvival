Shader "Fleet/Localized Damaged Hull"
{
    Properties
    {
        baseColorTexture("Hull texture",2D)="white"{}
        baseColorFactor("Hull tint",Color)=(1,1,1,1)
        baseColorTexture_texCoord("Color UV set",Float)=0
        metallicRoughnessTexture("Metal / roughness",2D)="white"{}
        metallicFactor("Metallic",Float)=0.35
        roughnessFactor("Roughness",Float)=0.6
        normalTexture("Normal",2D)="bump"{}
        normalTexture_scale("Normal strength",Float)=1
        normalTexture_texCoord("Normal UV set",Float)=0
        metallicRoughnessTexture_texCoord("Metal UV set",Float)=0
        emissiveTexture_texCoord("Emission UV set",Float)=0
        occlusionTexture_texCoord("Occlusion UV set",Float)=0
        _HasNormal("Has normal map",Float)=0
        emissiveTexture("Emission",2D)="white"{}
        emissiveFactor("Emission tint",Color)=(0,0,0,1)
        occlusionTexture("Occlusion",2D)="white"{}
        occlusionTexture_strength("Occlusion strength",Float)=1
        _Inner("Inner lining",Float)=0
        _WreckTint("Wreck tint",Float)=1
        _BreachCount("Breach count",Float)=0
        _Breach0("Breach 0",Vector)=(0,0,0,0)
        _Breach1("Breach 1",Vector)=(0,0,0,0)
        _Breach2("Breach 2",Vector)=(0,0,0,0)
        _Breach3("Breach 3",Vector)=(0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        sampler2D baseColorTexture,metallicRoughnessTexture,normalTexture,emissiveTexture,occlusionTexture;
        fixed4 baseColorFactor,emissiveFactor;
        float metallicFactor,roughnessFactor,normalTexture_scale,occlusionTexture_strength,_HasNormal,_Inner,_WreckTint,_BreachCount;
        float4 _Breach0,_Breach1,_Breach2,_Breach3;
        float baseColorTexture_texCoord,normalTexture_texCoord,metallicRoughnessTexture_texCoord,emissiveTexture_texCoord,occlusionTexture_texCoord;
        struct Input { float2 hullUV0; float2 hullUV1; float3 hullPosition; };
        void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.hullPosition=v.vertex.xyz;o.hullUV0=v.texcoord.xy;o.hullUV1=v.texcoord1.xy;}
        float crater(float3 p,float4 hit)
        {
            float3 q=(p-hit.xyz)/max(hit.w,.001);
            float jagged=1+.10*sin(q.x*17+q.z*11)+.06*sin(q.y*19-q.z*23);
            return length(q)/jagged;
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float d=100;
            if(_BreachCount>0)d=min(d,crater(IN.hullPosition,_Breach0));
            if(_BreachCount>1)d=min(d,crater(IN.hullPosition,_Breach1));
            if(_BreachCount>2)d=min(d,crater(IN.hullPosition,_Breach2));
            if(_BreachCount>3)d=min(d,crater(IN.hullPosition,_Breach3));
            if(_Inner<.5)clip(d-.63);
            fixed4 tex=tex2D(baseColorTexture,lerp(IN.hullUV0,IN.hullUV1,step(.5,baseColorTexture_texCoord)))*baseColorFactor;
            float soot=1-smoothstep(.66,1.55,d);
            o.Albedo=tex.rgb*lerp(1,.13,soot)*_WreckTint;
            fixed4 metal=tex2D(metallicRoughnessTexture,lerp(IN.hullUV0,IN.hullUV1,step(.5,metallicRoughnessTexture_texCoord)));
            o.Metallic=metal.b*metallicFactor;o.Smoothness=1-metal.g*roughnessFactor;
            o.Metallic*=lerp(.25,1,_WreckTint);o.Smoothness*=lerp(.2,1,_WreckTint);
            if(_HasNormal>.5){float3 normal=tex2D(normalTexture,lerp(IN.hullUV0,IN.hullUV1,step(.5,normalTexture_texCoord))).xyz*2-1;normal.xy*=normalTexture_scale;o.Normal=normalize(normal);}
            o.Occlusion=lerp(1,tex2D(occlusionTexture,lerp(IN.hullUV0,IN.hullUV1,step(.5,occlusionTexture_texCoord))).r,occlusionTexture_strength);
            float hot=(1-smoothstep(.66,.88,d))*smoothstep(.60,.66,d);
            o.Emission=tex2D(emissiveTexture,lerp(IN.hullUV0,IN.hullUV1,step(.5,emissiveTexture_texCoord))).rgb*emissiveFactor.rgb+float3(.75,.14,.025)*hot;
            o.Emission*=_WreckTint;
            if(_Inner>.5)
            {
                float ribs=step(.82,frac((IN.hullPosition.x+IN.hullPosition.z)*11));
                o.Albedo=lerp(float3(.035,.045,.05),float3(.12,.14,.15),ribs)*_WreckTint;
                o.Metallic=.6;o.Smoothness=.15;o.Emission=float3(.3,.05,.008)*ribs*(1-smoothstep(.2,1.2,d));
            }
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
