Shader "Custom/BasicToonShader"
{
    Properties
    {
        [HDR] _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Range(1, 50)) = 0
        _ShadowColor("Shadow Color", Color) = (0, 0, 0, 1)
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _AmbientColor("Ambient Color", Color) = (1,1,1,1)
        [HDR] _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
        _ShadingThreshold1("Shading Threshold 1", Range(0.0, 1.0)) = 0.33
        _ShadingThreshold2("Shading Threshold 2", Range(0.0, 1.0)) = 0.66
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0.0, 1)) = 0
        _BaseMap("Base Map", 2D) = "white"

        //_RmapMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "LitPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RmapMap);
            SAMPLER(sampler_RmapMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _RmapMap_ST;
            float4 _ShadowColor;
            float _ShadingThreshold1;
            float _ShadingThreshold2;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Glossiness;
            float4 _SpecularColor;
            float4 _AmbientColor;
            float _RimThreshold;
            float4 _RimColor;
            float _RimAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv           : TEXCOORD0;
                half   NdotL        : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 shadowCoords : TEXCOORD3;
            };

            // To make the Unity shader SRP Batcher compatible, declare all
            // properties related to a Material in a a single CBUFFER block with 
            // the name UnityPerMaterial.

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                Light mainLight= GetMainLight();
                half3 lightDir = normalize(mainLight.direction);

                OUT.positionHCS = positionHCS;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                half NdotL = saturate(dot(OUT.normalWS, lightDir));//0~1
                OUT.NdotL = NdotL;
                float3 positionWS= TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                OUT.viewDir = viewDirWS;

                // Get the VertexPositionInputs for the vertex position  
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                // Convert the vertex position to a position on the shadow map
                float4 shadowCoordinates = GetShadowCoord(positions);
                // Pass the shadow coordinates to the fragment shader
                OUT.shadowCoords = shadowCoordinates;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight= GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                //half NdotL = saturate(dot(IN.normalWS, lightDir));//0~1
                half NdotL=IN.NdotL;
                // Get the value from the shadow map at the shadow coordinates
                half shadowAmount = MainLightRealtimeShadow(IN.shadowCoords);
                // The SAMPLE_TEXTURE2D marco samples the texture with the given
                // sampler.
                half3 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                float3 viewDir = normalize(IN.viewDir);

                float lightIntensity = smoothstep(0, 0.01, NdotL );
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = dot(IN.normalWS, halfVector);
                float specularIntensity =pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                float3 specular = specularIntensitySmooth * _SpecularColor.rgb;

                float shadowMask = lerp(0.5,1,1-shadowAmount);

                // 使用 step 函數將亮度分為三個區段
                float step1 = smoothstep (_ShadingThreshold1 - 0.01, _ShadingThreshold1, NdotL);//if NdotL > _ShadingThreshold1, lightStep = 1, else 0
                float step2 = smoothstep (_ShadingThreshold2 - 0.01, _ShadingThreshold2, NdotL * shadowMask);//if NdotL > _ShadingThreshold2, lightStep = 1, else 0
                half3 color = texColor * _BaseColor.rgb;
                half3 shadowcolor = color * _ShadowColor.rgb;

                // 計算最終顏色：三個區域的顏色
                color = lerp(color* 1, color, step1);//  添加一個中間顏色區域
                color = lerp(shadowcolor.rgb, color, step2);//添加陰影顏色
                color = color;
             
                float rimDot = 1 - dot(viewDir, IN.normalWS);
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.05, _RimAmount, rimIntensity);
                float3 rim = rimIntensity * _RimColor.rgb;
                return float4(color*(_AmbientColor)+rim+specular, 1) ;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags {  }

            Cull Front
            ColorMask RGB
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
            };
            
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _RmapMap_ST;
            float4 _ShadowColor;
            float _ShadingThreshold1;
            float _ShadingThreshold2;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Glossiness;
            float4 _SpecularColor;
            float4 _AmbientColor;
            float _RimThreshold;
            float4 _RimColor;
            float _RimAmount;
            CBUFFER_END

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                float3 normalWS =  TransformObjectToWorldNormal(IN.normalOS);
                float3 normalHCS = TransformWorldToViewDir(normalWS);
                float2 offset = normalize(normalHCS.xy) * _OutlineWidth;
                offset.y *= _ProjectionParams.x;///!!!!
                float4 positionHCS  = TransformObjectToHClip(IN.positionOS.xyz);
                positionHCS.xy = positionHCS.xy + offset;
                OUT.positionHCS = positionHCS ;
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                // 這裡的顏色可以根據需要進行調整
                return _OutlineColor;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
      
    }
}
