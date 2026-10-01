Shader "FleetSurvival/PlanetAtmosphere"
{
    Properties { _Tint ("Atmosphere tint",Color)=(.1,.48,.95,1) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _FleetSunDirection;
            float4 _Tint;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            v2f vert(appdata v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);float light=dot(n,normalize(_FleetSunDirection));
                float facing=saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world)));
                float rim=pow(1-facing,5.5)*smoothstep(-.23,.35,light)*.32;
                float sunset=exp(-abs(light)*14)*pow(1-facing,3)*.045;
                return fixed4(_Tint.rgb*rim+float3(1,.28,.08)*sunset,1);
            }
            ENDCG
        }
    }
}
