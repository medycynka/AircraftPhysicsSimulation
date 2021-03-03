Shader "Unlit/ProceduralTerrainShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            const static int maxColorCount = 8;

            int _BaseColorCount;
            float3 _BaseColours[maxColorCount];
            float _BaseStartHeights[maxColorCount];
            float _MinHeight;
            float _MaxHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float inverseLerp(float a, float b, float value)
            {
                return saturate((value - a) / (b - a));
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float heightPercent = inverseLerp(_MinHeight, _MaxHeight, i.vertex.y);

                for (int i_ = 0; i_ < _BaseColorCount; i_++)
                {
                    float drawStrength = saturate(sign(heightPercent - _BaseStartHeights[i_]));
                    col.rgb = col.rgb * (1 - drawStrength) + _BaseColours[i_] * drawStrength;
                }
                
                return col;
            }
            ENDCG
        }
    }
}
