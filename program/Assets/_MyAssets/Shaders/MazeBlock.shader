Shader "MyCustomShader/MazeBlock" {
	Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0, 0, 0, 1)

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimThickness ("Rim Thickness", Float) = 0.2

        [Header(Util Properties)]
        [HideInInspector] _RimArrayLength ("Rim Array Length", Integer) = 50
        _ColorStrengthMax ("Color Strength Max", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 _BaseColor;

            fixed4 _RimColor;
            float _RimThickness;

            float _ColorStrengthMax;

            int _RimArrayLength;
            uniform fixed4 _RimPosArray[256];
            uniform float _RimRadiusArray[256];
            //uniform float _RimThicknessArray[256];
            uniform float _RimAlphaArray[256];

            // RenderType을 Transparent로 설정하지 않았기 때문에 투명도가 적용되지 않음
            fixed4 frag (v2f i) : SV_Target
            {
                float dist = 0.0;
                float l = 0.0;
                for(int j = 0; j < _RimArrayLength; j++) {
                    dist = distance(i.worldPos, _RimPosArray[j].xyz);
                    if(_RimRadiusArray[j] - _RimThickness < dist && dist < _RimRadiusArray[j]) {
                        //c = fixed4(_RimColor.xyz, _RimAlphaArray[j]);
                        //c += lerp(_BaseColor, _RimColor, _RimAlphaArray[j]);
                        l += _RimAlphaArray[j];
                    }
                }

                if(l > _ColorStrengthMax) l = _ColorStrengthMax;
                fixed4 c = lerp(_BaseColor, _RimColor, l);

                return c;
            }
            ENDCG
        }
    }
}
