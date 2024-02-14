Shader "MyCustomShader/ButtonMenu"
{
    Properties
    {
        _BasicColor ("Basic Color", Color) = (0.0, 1.0, 1.0, 1.0)
        [Toggle(USE_HIGHLIGHT)] _Highlight ("Highlight", float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature USE_HIGHLIGHT

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

            fixed4 _BasicColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _BasicColor;

                #ifdef USE_HIGHLIGHT
                    float2 center = float2(0.5, 0.5);
                    float dist = distance(center, i.uv);
                    float alpha = 0.0;
                    if(dist > 0.47) {
                        alpha = 1.0 - smoothstep(0.4, 0.3, dist);
                        col = col * (1.0 - smoothstep(0.5, 0.47, dist));
                    }
                    else if(dist > 0.3) {
                        alpha = 1.0 - smoothstep(0.47, 0.3, dist);
                    }

                    col = fixed4(col.rgb, alpha);
                #else

                #endif

                return col;
            }
            ENDCG
        }
    }
}
