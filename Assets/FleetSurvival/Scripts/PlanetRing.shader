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
            float noise(float2 p)
            {float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float edge=smoothstep(0,.08,i.uv.y)*(1-smoothstep(.85,1,i.uv.y));
                float bands=.25+.5*noise(float2(i.uv.y*64,3));
                float clumps=noise(i.uv*float2(210,38));
                float dust=.5+.5*hash(floor(i.uv*float2(1800,250)));
                float gap=1-smoothstep(.34,.36,i.uv.y)*(1-smoothstep(.4,.42,i.uv.y));
                float3 offset=i.world-_FleetPlanetCenter;float along=dot(offset,normalize(_FleetSunDirection));
                float eclipse=along<0 && dot(offset,offset)-along*along<_FleetPlanetRadius*_FleetPlanetRadius?.15:1;
                return fixed4(lerp(float3(.2,.14,.09),float3(.43,.32,.22),clumps)*eclipse,edge*bands*dust*gap*(.35+clumps*.6));
            }
            ENDCG
        }
    }
}
