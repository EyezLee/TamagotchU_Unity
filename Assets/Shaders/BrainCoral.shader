Shader "Custom/BrainCoral"
{
Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.8, 0.6, 1)
        _PatternScale ("Pattern Scale", Float) = 10.0
        _ReflectionIntensity ("Reflection Intensity", Range(0,1)) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.6
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _LightDir ("Light Direction", Vector) = (0.5, 0.8, 0.6, 0)
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200
        Cull Front

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
            };

            sampler2D _NormalMap;
            float4 _BaseColor;
            float _PatternScale;
            float _ReflectionIntensity;
            float _Smoothness;
            float4 _LightDir;

            // Brain coral procedural pattern function using sine and noise
            float brainCoralPattern(float2 uv, float scale)
            {
                float pattern = sin(uv.x * scale) * sin(uv.y * scale);
                pattern += (sin(uv.x * scale * 2.0) * sin(uv.y * scale * 2.0)) * 0.5;
                return saturate(pattern * 0.5 + 0.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base pattern color modulation
                float patternValue = brainCoralPattern(IN.uv, _PatternScale);

                // Sample and apply normal map
                float3 normalWS = IN.normalWS;
                #ifdef _NORMALMAP
                float3 tangentWS = normalize(IN.tangentWS);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentOS.w;
                float3 normalTS = UnpackNormal(tex2D(_NormalMap, IN.uv));
                normalWS = normalize(normalTS.x * tangentWS + normalTS.y * bitangentWS + normalTS.z * normalWS);
                #endif

                // Simple diffuse base color modulated by pattern
                float3 albedo = _BaseColor.rgb * patternValue;

                // Reflection vector calculation
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 reflVector = reflect(-viewDirWS, normalWS);
                float roughness = 1 - _Smoothness; // example
                float specularOcclusion = 1.0;
                float2 screenUV = IN.uv; // screen space UV if available

                float3 reflection = GlossyEnvironmentReflection(reflVector, IN.positionWS, roughness, specularOcclusion, screenUV);

                // Final color blending with reflection
                float3 finalColor = lerp(albedo, reflection, _ReflectionIntensity) * 1.2;

                // Apply smoothness as specular highlight strength
                float smoothness = _Smoothness;

                // Simple Blinn-Phong specular
                float3 lightDir = normalize(_LightDir.xyz);
                float3 halfDir = normalize(lightDir + viewDirWS);
                float spec = pow(max(dot(normalWS, halfDir), 0.0), smoothness * 128);
                
                finalColor += spec * _ReflectionIntensity;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Forward"
}
