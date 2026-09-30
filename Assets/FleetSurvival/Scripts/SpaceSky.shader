Shader "FleetSurvival/SpaceSky"
{
    Properties { _MainTex ("Distant nebula",2D)="black" {} _NebulaStrength ("Nebula strength",Float)=2.4 }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _NebulaStrength;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            v2f vert(appdata v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float3 stars(float2 uv,float2 grid,float density,float brightness)
            {
                float2 cell=floor(uv*grid),p=frac(uv*grid);
                float chance=hash(cell+19);float2 center=float2(hash(cell+1.7),hash(cell+8.4))*.7+.15;
                float distance=length(p-center),radius=lerp(.016,.045,hash(cell+31));
                float aa=max(length(fwidth(uv*grid))*.25,.003);
                float core=1-smoothstep(radius-aa,radius+aa,distance);
                float halo=exp(-distance*distance/(radius*radius*6))*.09;
                float3 tint=lerp(float3(.63,.78,1),float3(1,.86,.67),hash(cell+82));
                return tint*(core+halo)*step(chance,density)*brightness*min(1,radius/aa);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);
                float2 uv=float2(atan2(d.z,d.x)/(2*UNITY_PI)+.5,acos(clamp(d.y,-1,1))/UNITY_PI);
                float3 sky=tex2D(_MainTex,uv).rgb*_NebulaStrength;
                sky+=stars(uv,float2(420,210),.07,.60);
                sky+=stars(uv+float2(.03,.07),float2(190,95),.035,.80);
                sky+=stars(uv+float2(.31,.12),float2(80,40),.015,.95);
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
}
