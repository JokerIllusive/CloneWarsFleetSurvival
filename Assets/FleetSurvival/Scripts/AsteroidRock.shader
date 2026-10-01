Shader "FleetSurvival/AsteroidRock"
{
    Properties {_Color ("Rock color",Color)=(.3,.23,.18,1)}
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float3 _FleetSunDirection;float4 _Color;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;};
            v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float light=max(0,dot(normalize(i.normal),normalize(_FleetSunDirection)));
                float rough=frac(sin(dot(floor(i.local*45),float3(12.9898,78.233,43.719)))*43758.5453);
                return fixed4(_Color.rgb*(.18+light*1.25)*(.78+rough*.3),1);
            }
            ENDCG
        }
    }
}
