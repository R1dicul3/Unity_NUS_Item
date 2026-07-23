Shader "UI/PixelFluidDisplay"
{
    Properties
    {
        _MainTex ("Fluid Density", 2D) = "white" {}
        _FluidColor ("Fluid Color", Color) = (1,1,1,1)
        _Threshold ("Pixel Threshold", Range(0,1)) = 0.15
        _EdgeSoftness ("Edge Softness", Range(0,1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _FluidColor;
            float _Threshold;
            float _EdgeSoftness;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float density = tex2D(_MainTex, i.uv).r;
                float alpha = smoothstep(_Threshold - _EdgeSoftness, _Threshold + _EdgeSoftness, density);
                return float4(_FluidColor.rgb, _FluidColor.a * alpha);
            }
            ENDCG
        }
    }
}
