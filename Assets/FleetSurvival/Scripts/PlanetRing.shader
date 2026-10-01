Shader "FleetSurvival/PlanetRing"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _FleetSunDirection,_FleetPlanetCenter;float _FleetPlanetRadius;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            float hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            fixed4 frag(v2f i):SV_Target
            {
                float edge=smoothstep(0,.08,i.uv.y)*(1-smoothstep(.85,1,i.uv.y));
                float bands=.3+.7*hash(float2(floor(i.uv.y*140),3));
                float dust=.7+.3*hash(floor(i.uv*float2(1000,160)));
                float3 offset=i.world-_FleetPlanetCenter;float along=dot(offset,normalize(_FleetSunDirection));
                float eclipse=along<0 && dot(offset,offset)-along*along<_FleetPlanetRadius*_FleetPlanetRadius?.15:1;
                return fixed4(float3(.37,.27,.19)*eclipse,edge*bands*dust*.68);
            }
            ENDCG
        }
    }
}
