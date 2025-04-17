Shader "Custom/BasicFresnelEdgeGlow_URP"
{
    Properties
    {
        [HDR]_GlowColor("Glow Color", Color) = (0, 0, 0, 1)
        _FresnelPower("Power", Range(0.0, 10.0)) = 1.0
        _Strength("Strength ", Range(0.0,1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalRenderPipeline" }

        LOD 100

        Pass
        {
            Name "LitPass"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 worldPos : TEXCOORD0; // World position for view direction
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _GlowColor;
            float _FresnelPower;
            float _Strength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                return OUT;
            }

            // Fresnel effect calculation
            half4 FresnelEffect(float3 normal, float3 viewDir, float power)
            {
                float3 halfVec = normalize(viewDir + normal); // Half vector between view direction and normal
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), power); // Fresnel equation
                return half4(fresnel * _GlowColor.rgb, 1);
            }
            
            // Fragment shader
            half4 frag(Varyings i) : SV_Target
            {
                // Calculate view direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Apply Fresnel effect
                half4 fresnel = FresnelEffect(i.normalWS, viewDir, _FresnelPower);

                return float4(fresnel.rgb*_Strength, 0);
            }

            ENDHLSL
        }
    }
}
