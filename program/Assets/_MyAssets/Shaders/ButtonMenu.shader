Shader "MyCustomShader/ButtonMenu"
{
    Properties
    {
        [HDR] _BasicColor ("Basic Color", Color) = (0.0, 1.0, 1.0, 1.0)
        [Toggle(USE_PRESS)] _Press ("Press", float) = 0.0
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

            #pragma shader_feature USE_PRESS
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
                fixed4 col = (0.0, 0.0, 0.0, 0.0);

                #ifdef USE_HIGHLIGHT
                    const float maxHighlightRadio = 0.85;
                    const float minHighlightRadio = 0.65;

                    float normalizedUvX = abs(i.uv.x - 0.5) * 2.0;
                    if(maxHighlightRadio < normalizedUvX) {
                        col = _BasicColor * (1.0 - smoothstep(maxHighlightRadio, 1.0, normalizedUvX));
                    }
                    else if(minHighlightRadio < normalizedUvX) {
                        col = _BasicColor * smoothstep(minHighlightRadio, maxHighlightRadio, normalizedUvX);
                    }

                    float normalizedUvY = abs(i.uv.y - 0.5) * 2.0;
                    if(normalizedUvY > maxHighlightRadio) {
                        col *= (1.0 - smoothstep(maxHighlightRadio, 1.0, normalizedUvY));
                    }
                #else
                #endif

                #ifdef USE_PRESS 

                #else
                #endif

                return col;
            }
            ENDCG
        }
    }
}
