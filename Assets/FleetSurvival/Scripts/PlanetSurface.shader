Shader "FleetSurvival/PlanetSurface"
{
    Properties
    {
        _MainTex ("Surface and land mask", 2D) = "white" {}
        _LightsTex ("Night settlements", 2D) = "black" {}
        _CloudTex ("Cloud shadows", 2D) = "black" {}
        _RimColor ("Horizon color", Color) = (.025,.15,.27,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_LightsTex,_CloudTex;
            float3 _FleetSunDirection;
            float _FleetCloudOffset;
            float4 _RimColor;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.uv=v.uv; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos-i.world),sun=normalize(_FleetSunDirection);
                float light=dot(n,sun),day=smoothstep(-.06,.24,light);
                float4 surface=tex2D(_MainTex,i.uv);
                float cloud=tex2D(_CloudTex,i.uv+float2(_FleetCloudOffset,0)).r;
                // Height gradients give the existing desert texture small ridges and crater relief.
                float height=dot(surface.rgb,float3(.3,.5,.2));
                float hx=dot(tex2D(_MainTex,i.uv+float2(.0008,0)).rgb,float3(.3,.5,.2))-height;
                float hy=dot(tex2D(_MainTex,i.uv+float2(0,.0008)).rgb,float3(.3,.5,.2))-height;
                float3 east=normalize(cross(float3(0,1,0),n)+float3(.0001,0,0));
                float3 north=normalize(cross(n,east));
                float3 relief=normalize(n-east*hx*4-north*hy*4);
                float diffuse=max(0,dot(relief,sun));
                float3 color=surface.rgb*(.045+diffuse*1.5)*(1-cloud*.25*day);
                float spec=pow(saturate(dot(n,normalize(view+sun))),90)*(1-surface.a)*day;
                color+=float3(.8,.9,1)*spec*.38;
                float cities=tex2D(_LightsTex,i.uv).r*(1-smoothstep(-.15,.15,light));
                color+=float3(1,.57,.19)*cities*.85;
                float rim=pow(1-saturate(dot(n,view)),3.5);
                color+=_RimColor.rgb*rim*day;
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
