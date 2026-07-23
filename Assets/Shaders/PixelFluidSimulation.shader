Shader "Hidden/PixelFluidSimulation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VelocityTex ("Velocity", 2D) = "black" {}
        _DensityTex ("Density", 2D) = "black" {}
        _PressureTex ("Pressure", 2D) = "black" {}
        _DivergenceTex ("Divergence", 2D) = "black" {}
        _VorticityTex ("Vorticity", 2D) = "black" {}
        _Point ("Point", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.01
        _Value ("Value", Vector) = (0,0,0,0)
        _DeltaTime ("Delta Time", Float) = 0.016
        _Decay ("Decay", Float) = 0.998
        _BuoyancyStrength ("Buoyancy", Float) = 2.0
        _VorticityStrength ("Vorticity", Float) = 3.0
        _SourceTex ("Source Texture", 2D) = "white" {}
        _RestoreFactor ("Restore Factor", Float) = 0.05
        _DistortionScale ("Distortion Scale", Float) = 0.3
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Advection
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_advect
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _VelocityTex;
            float _Decay;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 hash22(float2 p) {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy) - 0.5;
            }

            fixed4 frag_advect (v2f i) : SV_Target {
                float2 vel = tex2D(_VelocityTex, i.uv).xy;
                // Tiny hash noise to break perfect symmetry and prevent dot patterns
                float2 noise = hash22(i.uv * _MainTex_TexelSize.zw + float2(_Time.y * 0.3, _Time.y * 0.2)) * 0.0008;
                float2 coord = i.uv - vel + noise;
                return tex2D(_MainTex, coord) * _Decay;
            }
            ENDCG
        }

        // Pass 1: Splat (Force / Density injection)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_splat
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float2 _Point;
            float _Radius;
            float4 _Value;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_splat (v2f i) : SV_Target {
                float2 d = i.uv - _Point;
                float dist = dot(d, d);
                float factor = exp(-dist / (_Radius * _Radius));
                return tex2D(_MainTex, i.uv) + factor * _Value;
            }
            ENDCG
        }

        // Pass 2: Buoyancy (density pushes velocity upward)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_buoyancy
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _VelocityTex;
            sampler2D _DensityTex;
            float _BuoyancyStrength;
            float _DeltaTime;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_buoyancy (v2f i) : SV_Target {
                float density = tex2D(_DensityTex, i.uv).r;
                float2 vel = tex2D(_VelocityTex, i.uv).xy;
                vel.y += density * _BuoyancyStrength * _DeltaTime;
                return float4(vel, 0, 1);
            }
            ENDCG
        }

        // Pass 3: Vorticity (calculate curl)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vorticity
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _VelocityTex;
            float4 _VelocityTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_vorticity (v2f i) : SV_Target {
                float4 vN = tex2D(_VelocityTex, i.uv + float2(0, _VelocityTex_TexelSize.y));
                float4 vS = tex2D(_VelocityTex, i.uv - float2(0, _VelocityTex_TexelSize.y));
                float4 vE = tex2D(_VelocityTex, i.uv + float2(_VelocityTex_TexelSize.x, 0));
                float4 vW = tex2D(_VelocityTex, i.uv - float2(_VelocityTex_TexelSize.x, 0));
                float curl = 0.5 * ((vE.y - vW.y) - (vN.x - vS.x));
                return float4(curl, 0, 0, 1);
            }
            ENDCG
        }

        // Pass 4: Vorticity Confinement (restore swirls)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vconf
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _VelocityTex;
            sampler2D _VorticityTex;
            float4 _VorticityTex_TexelSize;
            float _VorticityStrength;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_vconf (v2f i) : SV_Target {
                float4 vN = tex2D(_VorticityTex, i.uv + float2(0, _VorticityTex_TexelSize.y));
                float4 vS = tex2D(_VorticityTex, i.uv - float2(0, _VorticityTex_TexelSize.y));
                float4 vE = tex2D(_VorticityTex, i.uv + float2(_VorticityTex_TexelSize.x, 0));
                float4 vW = tex2D(_VorticityTex, i.uv - float2(_VorticityTex_TexelSize.x, 0));

                float2 eta = 0.5 * float2(abs(vE.x) - abs(vW.x), abs(vN.x) - abs(vS.x));
                float len = length(eta) + 1e-5;
                float2 N = eta / len;

                float vort = tex2D(_VorticityTex, i.uv).x;
                float2 force = _VorticityStrength * float2(N.y * vort, -N.x * vort);

                float2 vel = tex2D(_VelocityTex, i.uv).xy + force;
                return float4(vel, 0, 1);
            }
            ENDCG
        }

        // Pass 5: Divergence
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_div
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _VelocityTex;
            float4 _VelocityTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_div (v2f i) : SV_Target {
                float4 vN = tex2D(_VelocityTex, i.uv + float2(0, _VelocityTex_TexelSize.y));
                float4 vS = tex2D(_VelocityTex, i.uv - float2(0, _VelocityTex_TexelSize.y));
                float4 vE = tex2D(_VelocityTex, i.uv + float2(_VelocityTex_TexelSize.x, 0));
                float4 vW = tex2D(_VelocityTex, i.uv - float2(_VelocityTex_TexelSize.x, 0));
                float div = 0.5 * ((vE.x - vW.x) + (vN.y - vS.y));
                return float4(div, 0, 0, 1);
            }
            ENDCG
        }

        // Pass 6: Jacobi (Pressure solve)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_jacobi
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _PressureTex;
            sampler2D _DivergenceTex;
            float4 _PressureTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_jacobi (v2f i) : SV_Target {
                float4 pN = tex2D(_PressureTex, i.uv + float2(0, _PressureTex_TexelSize.y));
                float4 pS = tex2D(_PressureTex, i.uv - float2(0, _PressureTex_TexelSize.y));
                float4 pE = tex2D(_PressureTex, i.uv + float2(_PressureTex_TexelSize.x, 0));
                float4 pW = tex2D(_PressureTex, i.uv - float2(_PressureTex_TexelSize.x, 0));
                float div = tex2D(_DivergenceTex, i.uv).x;
                float p = (pN.x + pS.x + pE.x + pW.x - div) * 0.25;
                return float4(p, 0, 0, 1);
            }
            ENDCG
        }

        // Pass 7: Gradient Subtraction
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_grad
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _VelocityTex;
            sampler2D _PressureTex;
            float4 _PressureTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_grad (v2f i) : SV_Target {
                float4 pN = tex2D(_PressureTex, i.uv + float2(0, _PressureTex_TexelSize.y));
                float4 pS = tex2D(_PressureTex, i.uv - float2(0, _PressureTex_TexelSize.y));
                float4 pE = tex2D(_PressureTex, i.uv + float2(_PressureTex_TexelSize.x, 0));
                float4 pW = tex2D(_PressureTex, i.uv - float2(_PressureTex_TexelSize.x, 0));
                float2 grad = 0.5 * float2(pE.x - pW.x, pN.x - pS.x);
                float2 vel = tex2D(_VelocityTex, i.uv).xy - grad;
                return float4(vel, 0, 1);
            }
            ENDCG
        }

        // Pass 8: Boundary (zero out edges)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_boundary
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_boundary (v2f i) : SV_Target {
                float2 uv = i.uv;
                float2 texel = _MainTex_TexelSize.xy;
                if (uv.x <= texel.x || uv.x >= 1.0 - texel.x || uv.y <= texel.y || uv.y >= 1.0 - texel.y)
                {
                    return float4(0,0,0,1);
                }
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }

        // Pass 9: Advect Background Texture (distort image with fluid velocity)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_advect_bg
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _VelocityTex;
            sampler2D _SourceTex;
            float _RestoreFactor;
            float _DistortionScale;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_advect_bg (v2f i) : SV_Target {
                float2 vel = tex2D(_VelocityTex, i.uv).xy;
                // NO hash noise for background texture — noise causes snow/accumulated grain
                float2 coord = i.uv - vel * _DistortionScale;
                fixed4 advected = tex2D(_MainTex, coord);
                fixed4 source = tex2D(_SourceTex, i.uv);
                return lerp(advected, source, _RestoreFactor);
            }
            ENDCG
        }
    }
}
