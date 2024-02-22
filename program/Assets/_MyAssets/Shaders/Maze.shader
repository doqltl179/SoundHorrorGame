// HLSL에서는 함수에 배열 인자를 직접적으로 전달하는건 지원하지 않음

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
        _RimColor_None ("Rim Color (None)", Color) = (1, 1, 1, 1)
        _RimColor_Player ("Rim Color (Player)", Color) = (1, 1, 1, 1)
        _RimColor_Monster ("Rim Color (Monster)", Color) = (1, 1, 1, 1)
        _RimColor_Item ("Rim Color (Item)", Color) = (1, 1, 1, 1)
        _RimThickness ("Rim Thickness", Float) = 0.2

        [Header(Player)]
        _PlayerPastPosColor ("Player Past Pos Color", Color) = (1, 1, 1, 1)
        _PlayerPastPosRadius ("Player Past Pos Radius", float) = 0.3

        [Header(Util Properties)]
        _ColorStrengthMax ("Color Strength Max", Float) = 1.5
        [Toggle(USE_BASE_COLOR)] _UseBaseColor ("Use Base Color", float) = 1.0

        [HideInInspector] _RimArrayLength_None ("Rim Array (None) Length", Integer) = 0
        [HideInInspector] _RimArrayLength_Player ("Rim Array Length (Player)", Integer) = 0
        [HideInInspector] _RimArrayLength_Monster ("Rim Array Length (Monster)", Integer) = 0
        [HideInInspector] _RimArrayLength_Item ("Rim Array Length (Item)", Integer) = 0

        [HideInInspector] _PlayerPastPosArrayLength ("Player Past Pos Array Length (Item)", Integer) = 0
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                // float4 vertex : SV_POSITION;
                // float2 uv : TEXCOORD0;
                // float3 localPos : TEXCOORD1;
                // float3 worldNormal : TEXCOORD2;
                // float4 worldPos : TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // o.uv = v.uv;
                // o.localPos = UnityObjectToClipPos(v.vertex).xyz;
                // o.worldNormal = UnityObjectToWorldNormal(v.normal);
                // o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));
                // o.worldPos.xyz += o.worldNormal * 0.05;
                // o.vertex = UnityWorldToClipPos(o.worldPos);

                return o;
            }

            sampler2D _MainTex;
            sampler2D _MetallicMap;
            sampler2D _NormalMap;
            sampler2D _OcclusionMap;

            float _MetallicStrength;
            float _OcclusionStrength;

            fixed4 _BaseColor;

            fixed4 _RimColor_None;
            fixed4 _RimColor_Player;
            fixed4 _RimColor_Monster;
            fixed4 _RimColor_Item;
            float _RimThickness;

            fixed4 _PlayerPastPosColor;
            float _PlayerPastPosRadius;

            float _ColorStrengthMax;

            int _RimArrayLength_None;
            int _RimArrayLength_Player;
            int _RimArrayLength_Monster;
            int _RimArrayLength_Item;
            uniform fixed4 _RimPosArray_None[256];
            uniform fixed4 _RimPosArray_Player[256];
            uniform fixed4 _RimPosArray_Monster[256];
            uniform fixed4 _RimPosArray_Item[256];
            uniform float _RimRadiusArray_None[256];
            uniform float _RimRadiusArray_Player[256];
            uniform float _RimRadiusArray_Monster[256];
            uniform float _RimRadiusArray_Item[256];
            uniform float _RimAlphaArray_None[256];
            uniform float _RimAlphaArray_Player[256];
            uniform float _RimAlphaArray_Monster[256];
            uniform float _RimAlphaArray_Item[256];

            int _PlayerPastPosArrayLength;
            uniform float4 _PlayerPastPosArray[256];
            uniform float _PlayerPastPosAlphaArray[256];

            float getRimRatio(float min, float max, float d) {
                return (d - min) / (max - min);
            }

            float getRimColorRatio_none(float3 worldPos, float rimThicknessOffset = 1.0) {
                if(_RimArrayLength_None <= 0) {
                    return 0.0;
                }

                const float blurOffset = 0.3;
                float distanceRatioMin = 0.0 - blurOffset;
                float distanceRatioMax = 1.0 + blurOffset;
                float halfOfDistanceRatio = (distanceRatioMax + distanceRatioMin) * 0.5;

                float dist = 0.0;
                float distanceRatio = 0.0;
                float r = 0.0;
                for(int j = 0; j < _RimArrayLength_None; j++) {
                    dist = distance(worldPos, _RimPosArray_None[j].xyz);
                    distanceRatio = getRimRatio(_RimRadiusArray_None[j] - _RimThickness * rimThicknessOffset, _RimRadiusArray_None[j], dist);
                    if(distanceRatioMin < distanceRatio && distanceRatio < distanceRatioMax) {
                        float newRatio = abs(distanceRatio - halfOfDistanceRatio);
                        r += _RimAlphaArray_None[j] * smoothstep(halfOfDistanceRatio, halfOfDistanceRatio - blurOffset, newRatio);
                    }
                }
                if(r > _ColorStrengthMax) r = _ColorStrengthMax;

                return r;
            }

            float getRimColorRatio_player(float3 worldPos, float rimThicknessOffset = 1.0) {
                if(_RimArrayLength_Player <= 0) {
                    return 0.0;
                }

                const float blurOffset = 0.3;
                float distanceRatioMin = 0.0 - blurOffset;
                float distanceRatioMax = 1.0 + blurOffset;
                float halfOfDistanceRatio = (distanceRatioMax + distanceRatioMin) * 0.5;

                float dist = 0.0;
                float distanceRatio = 0.0;
                float r = 0.0;
                for(int j = 0; j < _RimArrayLength_Player; j++) {
                    dist = distance(worldPos, _RimPosArray_Player[j].xyz);
                    distanceRatio = getRimRatio(_RimRadiusArray_Player[j] - _RimThickness * rimThicknessOffset, _RimRadiusArray_Player[j], dist);
                    if(distanceRatioMin < distanceRatio && distanceRatio < distanceRatioMax) {
                        float newRatio = abs(distanceRatio - halfOfDistanceRatio);
                        r += _RimAlphaArray_Player[j] * smoothstep(halfOfDistanceRatio, halfOfDistanceRatio - blurOffset, newRatio);
                    }
                }
                if(r > _ColorStrengthMax) r = _ColorStrengthMax;

                return r;
            }

            float getRimColorRatio_monster(float3 worldPos, float rimThicknessOffset = 1.0) {
                if(_RimArrayLength_Monster <= 0) {
                    return 0.0;
                }

                const float blurOffset = 0.3;
                float distanceRatioMin = 0.0 - blurOffset;
                float distanceRatioMax = 1.0 + blurOffset;
                float halfOfDistanceRatio = (distanceRatioMax + distanceRatioMin) * 0.5;

                float dist = 0.0;
                float distanceRatio = 0.0;
                float r = 0.0;
                for(int j = 0; j < _RimArrayLength_Monster; j++) {
                    dist = distance(worldPos, _RimPosArray_Monster[j].xyz);
                    distanceRatio = getRimRatio(_RimRadiusArray_Monster[j] - _RimThickness * rimThicknessOffset, _RimRadiusArray_Monster[j], dist);
                    if(distanceRatioMin < distanceRatio && distanceRatio < distanceRatioMax) {
                        float newRatio = abs(distanceRatio - halfOfDistanceRatio);
                        r += _RimAlphaArray_Monster[j] * smoothstep(halfOfDistanceRatio, halfOfDistanceRatio - blurOffset, newRatio);
                    }
                }
                if(r > _ColorStrengthMax) r = _ColorStrengthMax;

                return r;
            }

            float getRimColorRatio_item(float3 worldPos, float rimThicknessOffset = 1.0) {
                if(_RimArrayLength_Item <= 0) {
                    return 0.0;
                }

                const float blurOffset = 0.3;
                float distanceRatioMin = 0.0 - blurOffset;
                float distanceRatioMax = 1.0 + blurOffset;
                float halfOfDistanceRatio = (distanceRatioMax + distanceRatioMin) * 0.5;

                float dist = 0.0;
                float distanceRatio = 0.0;
                float r = 0.0;
                for(int j = 0; j < _RimArrayLength_Item; j++) {
                    dist = distance(worldPos, _RimPosArray_Item[j].xyz);
                    distanceRatio = getRimRatio(_RimRadiusArray_Item[j] - _RimThickness * rimThicknessOffset, _RimRadiusArray_Item[j], dist);
                    if(distanceRatioMin < distanceRatio && distanceRatio < distanceRatioMax) {
                        float newRatio = abs(distanceRatio - halfOfDistanceRatio);
                        r += _RimAlphaArray_Item[j] * smoothstep(halfOfDistanceRatio, halfOfDistanceRatio - blurOffset, newRatio);
                    }
                }
                if(r > _ColorStrengthMax) r = _ColorStrengthMax;

                return r;
            }

            float getPlayerPastPosColorRatio(float3 worldPos) {
                if(_PlayerPastPosArrayLength <= 0) {
                    return 0.0;
                }

                const float blurOffset = 0.3;
                float distanceRatioMax = 1.0 + blurOffset;

                float dist = 0.0;
                float distanceRatio = 0.0;
                float r = 0.0;
                for(int j = 0; j < _PlayerPastPosArrayLength; j++) {
                    dist = distance(worldPos, _PlayerPastPosArray[j].xyz);
                    distanceRatio = dist / _PlayerPastPosRadius;
                    if(distanceRatio < distanceRatioMax) {
                        r += _PlayerPastPosAlphaArray[j] * smoothstep(distanceRatioMax, 1.0, distanceRatio);
                    }
                }
                if(r > _ColorStrengthMax) r = _ColorStrengthMax;

                return r;
            }

            // RenderType을 Transparent로 설정하지 않았기 때문에 투명도가 적용되지 않음
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c;

                #if USE_BASE_COLOR

                float rimColorRatio_none = getRimColorRatio_none(i.worldPos);
                float rimColorRatio_player = getRimColorRatio_player(i.worldPos);
                float rimColorRatio_monster = getRimColorRatio_monster(i.worldPos);
                float rimColorRatio_item = getRimColorRatio_item(i.worldPos);
                float playerPastPosColorRatio = getPlayerPastPosColorRatio(i.worldPos);
                c = lerp(_BaseColor, _RimColor_None, rimColorRatio_none) +
                    lerp(_BaseColor, _RimColor_Player, rimColorRatio_player) +
                    lerp(_BaseColor, _RimColor_Monster, rimColorRatio_monster) +
                    lerp(_BaseColor, _RimColor_Item, rimColorRatio_item) + 
                    lerp(_BaseColor, _PlayerPastPosColor, playerPastPosColorRatio);

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

                float rimColorRatio_none = getRimColorRatio_none(i.worldPos, 20.0);
                float rimColorRatio_player = getRimColorRatio_player(i.worldPos, 20.0);
                float rimColorRatio_monster = getRimColorRatio_monster(i.worldPos, 20.0);
                float rimColorRatio_item = getRimColorRatio_item(i.worldPos, 20.0);
                float rimColorRatio = 
                    rimColorRatio_none + 
                    rimColorRatio_player + 
                    rimColorRatio_monster + 
                    rimColorRatio_item;
                if(rimColorRatio > _ColorStrengthMax) rimColorRatio = _ColorStrengthMax;
                c = fixed4(finalColor * rimColorRatio, 1.0);

                #endif

                return c;
            }
            ENDCG
        }
    }
}
