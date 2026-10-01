Shader "FleetSurvival/PlanetClouds"
{
    Properties { _MainTex ("Cloud density",2D)="black" {} _Tint ("Weather tint",Color)=(.87,.94,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float3 _FleetSunDirection; float _FleetCloudOffset;float4 _Tint;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float light=dot(normalize(i.normal),normalize(_FleetSunDirection));
                float density=tex2D(_MainTex,i.uv+float2(_FleetCloudOffset,0)).r;
                float3 lit=lerp(_Tint.rgb*.04,_Tint.rgb,smoothstep(-.12,.75,light));
                float horizon=saturate(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))*4);
                return fixed4(lit,density*.78*horizon);
            }
            ENDCG
        }
    }
}
