Shader "BoatGame/Ocean Gerstner URP"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.015, 0.16, 0.22, 1)
        _ShallowColor ("Shallow Color", Color) = (0.08, 0.42, 0.48, 1)
        _FoamColor ("Foam Color", Color) = (0.86, 0.96, 0.92, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.82
        _Smoothness ("Specular Tightness", Range(0.05, 1)) = 0.55
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.7
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.5
        _FresnelStrength ("Fresnel Strength", Range(0, 1.5)) = 0.45

        _NormalMapA ("Scrolling Normal A", 2D) = "white" {}
        _NormalMapB ("Scrolling Normal B", 2D) = "white" {}
        _NormalTilingA ("Normal Tiling A", Float) = 0.055
        _NormalTilingB ("Normal Tiling B", Float) = 0.13
        _NormalSpeedA ("Normal Speed A", Vector) = (0.035, 0.018, 0, 0)
        _NormalSpeedB ("Normal Speed B", Vector) = (-0.025, 0.031, 0, 0)
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.38

        _MicroWaveScale ("Micro Wave Scale", Float) = 1.35
        _MicroWaveSpeed ("Micro Wave Speed", Float) = 1.2
        _MicroWaveStrength ("Micro Wave Strength", Range(0, 0.35)) = 0.08

        _FoamIntensity ("Foam Intensity", Range(0, 1)) = 0.22
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.78
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MAX_WATER_WAVES 8
            #define WATER_TWO_PI 6.28318530718

            int _WaterWaveCount;
            float _WaterLevel;
            float _OceanTime;
            float4 _WaterWaveDirection[MAX_WATER_WAVES];
            float _WaterWaveAmplitude[MAX_WATER_WAVES];
            float _WaterWaveWavelength[MAX_WATER_WAVES];
            float _WaterWaveSpeed[MAX_WATER_WAVES];
            float _WaterWaveSteepness[MAX_WATER_WAVES];

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float _Alpha;
                float _Smoothness;
                float _SpecularStrength;
                float _FresnelPower;
                float _FresnelStrength;
                float _NormalTilingA;
                float _NormalTilingB;
                float4 _NormalSpeedA;
                float4 _NormalSpeedB;
                float _NormalStrength;
                float _MicroWaveScale;
                float _MicroWaveSpeed;
                float _MicroWaveStrength;
                float _FoamIntensity;
                float _FoamThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float crest : TEXCOORD3;
            };

            void EvaluateWaterSurface(float2 sourceXZ, out float3 displacement, out float3 normalWS, out float crest)
            {
                displacement = float3(0.0, 0.0, 0.0);
                float3 tangentX = float3(1.0, 0.0, 0.0);
                float3 tangentZ = float3(0.0, 0.0, 1.0);
                float crestSum = 0.0;
                float amplitudeSum = 0.0;
                int waveCount = max(_WaterWaveCount, 1);

                [unroll]
                for (int i = 0; i < MAX_WATER_WAVES; i++)
                {
                    if (i >= _WaterWaveCount)
                    {
                        break;
                    }

                    float amplitude = _WaterWaveAmplitude[i];
                    float wavelength = max(_WaterWaveWavelength[i], 0.1);
                    float speed = _WaterWaveSpeed[i];
                    float steepness = saturate(_WaterWaveSteepness[i]);
                    float2 direction = normalize(_WaterWaveDirection[i].xy);
                    float waveNumber = WATER_TWO_PI / wavelength;
                    float phase = waveNumber * (dot(direction, sourceXZ) - speed * _OceanTime);
                    float sinPhase = sin(phase);
                    float cosPhase = cos(phase);
                    float steepnessShare = steepness / waveCount;
                    float horizontalAmplitude = steepnessShare / waveNumber;

                    displacement.x += direction.x * horizontalAmplitude * cosPhase;
                    displacement.y += amplitude * sinPhase;
                    displacement.z += direction.y * horizontalAmplitude * cosPhase;

                    tangentX.x += -direction.x * direction.x * steepnessShare * sinPhase;
                    tangentX.y += direction.x * amplitude * waveNumber * cosPhase;
                    tangentX.z += -direction.x * direction.y * steepnessShare * sinPhase;

                    tangentZ.x += -direction.x * direction.y * steepnessShare * sinPhase;
                    tangentZ.y += direction.y * amplitude * waveNumber * cosPhase;
                    tangentZ.z += -direction.y * direction.y * steepnessShare * sinPhase;

                    crestSum += (sinPhase * 0.5 + 0.5) * amplitude;
                    amplitudeSum += amplitude;
                }

                normalWS = normalize(cross(tangentZ, tangentX));
                normalWS = normalWS.y < 0.0 ? -normalWS : normalWS;
                crest = amplitudeSum > 0.0001 ? saturate(crestSum / amplitudeSum) : 0.0;
            }

            float3 DecodeRgbNormal(float4 packedNormal)
            {
                float3 normal = packedNormal.xyz * 2.0 - 1.0;
                return normalize(normal);
            }

            float3 ApplyVisualNormals(float3 geometricNormal, float3 positionWS)
            {
                float2 uvA = positionWS.xz * _NormalTilingA + _OceanTime * _NormalSpeedA.xy;
                float2 uvB = positionWS.xz * _NormalTilingB + _OceanTime * _NormalSpeedB.xy;
                float3 normalA = DecodeRgbNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                float3 normalB = DecodeRgbNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));

                float microA = sin(dot(positionWS.xz, float2(0.83, 0.56)) * _MicroWaveScale + _OceanTime * _MicroWaveSpeed);
                float microB = sin(dot(positionWS.xz, float2(-0.38, 0.92)) * (_MicroWaveScale * 1.37) - _OceanTime * (_MicroWaveSpeed * 0.73));
                float2 microSlope = float2(microA, microB) * _MicroWaveStrength;

                float2 detailSlope = (normalA.xy + normalB.xy) * (0.5 * _NormalStrength) + microSlope;
                return normalize(geometricNormal + float3(detailSlope.x, 0.0, detailSlope.y));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 sourceWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacement;
                float3 normalWS;
                float crest;
                EvaluateWaterSurface(sourceWS.xz, displacement, normalWS, crest);

                float3 displacedWS = float3(sourceWS.x + displacement.x, _WaterLevel + displacement.y, sourceWS.z + displacement.z);
                output.positionCS = TransformWorldToHClip(displacedWS);
                output.positionWS = displacedWS;
                output.uv = input.uv;
                output.normalWS = normalWS;
                output.crest = crest;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = ApplyVisualNormals(normalize(input.normalWS), input.positionWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                Light mainLight = GetMainLight();

                float nDotL = saturate(dot(normalWS, mainLight.direction));
                float wrapLight = nDotL * 0.72 + 0.28;
                float fresnel = pow(1.0 - saturate(dot(viewDirWS, normalWS)), _FresnelPower) * _FresnelStrength;
                float specularPower = lerp(24.0, 160.0, saturate(_Smoothness));
                float specular = pow(saturate(dot(reflect(-mainLight.direction, normalWS), viewDirWS)), specularPower) * _SpecularStrength;

                float heightTint = saturate((input.positionWS.y - _WaterLevel + 1.4) / 3.0);
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, heightTint);
                float3 litColor = waterColor * (0.22 + wrapLight * mainLight.color);
                litColor += specular * mainLight.color;
                litColor += fresnel * float3(0.55, 0.84, 0.92);

                float foamMask = saturate((input.crest - _FoamThreshold) / max(1.0 - _FoamThreshold, 0.001)) * _FoamIntensity;
                litColor = lerp(litColor, _FoamColor.rgb, foamMask);

                return half4(litColor, _Alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
