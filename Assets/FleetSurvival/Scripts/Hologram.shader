Shader "FleetSurvival/Hologram"
{
    Properties { _Color ("Tint", Color) = (.1,.7,1,.2) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct Output { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 view:TEXCOORD1; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.view=WorldSpaceViewDir(v.vertex); return o; }
            float4 frag(Output i):SV_Target { float rim=1-abs(dot(normalize(i.normal),normalize(i.view))); return float4(_Color.rgb,_Color.a+rim*.4); }
            ENDCG
        }
    }
}
