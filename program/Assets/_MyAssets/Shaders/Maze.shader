Shader "MyCustomShader/Maze" {
	Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0, 0, 0, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MetallicMap ("Metallic", 2D) = "white" {}
        _MetallicStrength ("Metallic Strength", Range(0.0, 1.0)) = 1.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimThickness ("Rim Thickness", Float) = 0.2

        [Header(Util Properties)]
        [HideInInspector] _RimArrayLength ("Rim Array Length", Integer) = 50
        _ColorStrengthMax ("Color Strength Max", Float) = 1.5
        [Toggle(USE_BASE_COLOR)] _UseBaseColor ("Use Base Color", float) = 1.0
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

            #pragma shader_feature USE_BASE_COLOR

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            sampler2D _MainTex;
            sampler2D _MetallicMap;
            sampler2D _NormalMap;
            sampler2D _OcclusionMap;

            float _MetallicStrength;
            float _OcclusionStrength;

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
                fixed4 c;

                #if USE_BASE_COLOR

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
                c = lerp(_BaseColor, _RimColor, l);

                #else

                fixed3 albedo = tex2D(_MainTex, i.uv).rgb;
                float3 normal = normalize(UnpackNormal(tex2D(_NormalMap, i.uv))); // 정확한 방향을 유지
                float metallic = tex2D(_MetallicMap, i.uv).a * _MetallicStrength; // 금속성 값을 조절
                float occlusion = tex2D(_OcclusionMap, i.uv).r * _OcclusionStrength; // 가려짐 처리
                
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(float3(0.5, 0.5, 1.0));
                float diff = max(dot(normal, lightDir), 0.0);
                
                float3 halfwayDir = normalize(lightDir + viewDir);
                float specular = pow(max(dot(normal, halfwayDir), 0.0), 16.0) * metallic;
                float3 diffuse = albedo * (1.0 - metallic) * diff;
                
                float3 ambient = albedo * 0.05; // Ambient light contribution
                float3 finalColor = ambient + (diffuse + specular) * occlusion;
                
                c = fixed4(finalColor, 1.0);

                #endif

                return c;
            }
            ENDCG
        }
    }
}
