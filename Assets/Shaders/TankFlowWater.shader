Shader "Boiler/Tank Flow Water"
{
    Properties
    {
        [Header(Water Color)]
        _Color ("Body Color", Color) = (0.035, 0.31, 0.46, 1)
        _ShallowColor ("Surface Color", Color) = (0.12, 0.56, 0.65, 1)
        _RippleColor ("Ripple Highlight Color", Color) = (0.48, 0.82, 0.87, 1)
        [Header(Flow)]
        _FlowDirection ("Flow Direction (World X Z)", Vector) = (1, 0.35, 0, 0)
        _FlowSpeed ("Flow Speed (Meters Per Second)", Range(0, 0.5)) = 0.08
        _WaveScale ("Ripple Density", Range(0.5, 12)) = 4
        _WaveStrength ("Ripple Strength", Range(0, 0.5)) = 0.16
        [Header(Lighting)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.86
        _ReflectionStrength ("Environment Reflection", Range(0, 1)) = 0.45
        [HideInInspector] _FlowTimeOverride ("Preview Time (-1 Uses Unity Time)", Float) = -1
        [HideInInspector] _TreatmentState ("Treatment Preview", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TreatmentTime ("Treatment Demo Time", Float) = 0
        [HideInInspector] _Aeration ("Aeration Preview", Float) = 0
        [HideInInspector] _Mixing ("Mixing Preview", Float) = 0
        [HideInInspector] _MixingStartTime ("Mixing Start Time", Float) = 0
        [HideInInspector] _WaterSrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _WaterDstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _WaterZWrite ("Depth Write", Float) = 1
    }
    SubShader
    {
        // An opaque liquid volume renders before the Q-transparent tank shell.
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 150
        Cull Back
        ZWrite [_WaterZWrite]
        Blend [_WaterSrcBlend] [_WaterDstBlend]

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "TankMixingFlow.cginc"

            fixed4 _Color, _ShallowColor, _RippleColor;
            float4 _FlowDirection;
            float _FlowSpeed, _WaveScale, _WaveStrength;
            half _Smoothness, _ReflectionStrength;
            float _FlowTimeOverride;
            float4 _TreatmentState;
            float _TreatmentTime;
            float _Aeration;
            float _Mixing;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float2 tankPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // No vertex movement: the level controller retains the exact top and bottom.
                output.position = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.tankPosition = input.vertex.xz;
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            // Three warped waves with analytical gradients; no normal textures or extra cameras.
            void SampleRipples(float2 position, float time, out float height, out float2 gradient)
            {
                float2 direction = _FlowDirection.xy;
                direction = dot(direction, direction) > 0.0001 ? normalize(direction) : float2(1, 0);
                float travel = time * _FlowSpeed * _WaveScale;
                float2 a = position * _WaveScale - direction * travel;
                float2 b = position * (_WaveScale * 1.37)
                    - float2(-direction.y, direction.x) * travel * 0.63;

                float warp1 = dot(a, float2(-1.3, 1.1));
                float warp2 = dot(a, float2(0.8, 1.5));
                float warp3 = dot(b, float2(1.4, 2.1));
                float s1, c1, s2, c2, s3, c3;
                sincos(dot(a, float2(1.7, 2.4)) + 0.6 * sin(warp1), s1, c1);
                sincos(dot(a, float2(-2.6, 1.8)) + 0.7 * sin(warp2), s2, c2);
                sincos(dot(b, float2(3.8, -2.3)) + 0.5 * sin(warp3), s3, c3);
                height = (s1 + s2 * 0.55 + s3 * 0.28) / 1.83;
                gradient = (c1 * (float2(1.7, 2.4) + 0.6 * cos(warp1) * float2(-1.3, 1.1))
                    + c2 * 0.55 * (float2(-2.6, 1.8) + 0.7 * cos(warp2) * float2(0.8, 1.5))
                    + c3 * 0.28 * 1.37 * (float2(3.8, -2.3) + 0.5 * cos(warp3) * float2(1.4, 2.1))) / 1.83;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float time = _FlowTimeOverride >= 0 ? _FlowTimeOverride : _Time.y;
                float wave;
                float2 slope;
                // World coordinates prevent UV stretching as the liquid cube changes height.
                SampleRipples(input.worldPosition.xz, time, wave, slope);
                half3 geometryNormal = normalize(input.worldNormal);
                half top = saturate((geometryNormal.y - 0.5) * 2);
                float activity = max(_TreatmentState.x, max(_TreatmentState.y, _TreatmentState.z));
                float waveStrength = _WaveStrength * lerp(1, 0.18 + 1.05 * activity, _TreatmentState.w);
                if (_Mixing > 0.5)
                {
                    SampleMixingSurface(input.tankPosition, MixingPhase(time), wave, slope);
                    waveStrength = max(waveStrength, 0.21);
                }
                half3 normal = normalize(geometryNormal + half3(-slope.x, 0, -slope.y) * waveStrength * top);
                half3 viewDirection = normalize(UnityWorldSpaceViewDir(input.worldPosition));
                half3 lightDirection = normalize(UnityWorldSpaceLightDir(input.worldPosition));
                half3 halfDirection = normalize(lightDirection + viewDirection);

                half tint = saturate(0.18 + top * 0.48 + wave * waveStrength * top);
                half3 water = lerp(_Color.rgb, _ShallowColor.rgb, tint);
                half3 ambient = max(ShadeSH9(half4(geometryNormal, 1)), half3(0.3, 0.3, 0.3));
                half diffuse = saturate(dot(normal, lightDirection));
                half3 color = water * (ambient + _LightColor0.rgb * (0.35 + 0.4 * diffuse));
                half ridge = pow(saturate(0.5 + wave * 0.5), 5);
                color += _RippleColor.rgb * ridge * waveStrength * top * 0.65;

                if (activity > 0.001 && top > 0.001)
                {
                    // These points depict the demo, rather than measured inlet and outlet positions.
                    float inlet = length(input.tankPosition - float2(-0.29, 0.23));
                    float outlet = length(input.tankPosition - float2(0.3, -0.24));
                    float fillRing = saturate(0.5 + 0.5 * cos(inlet * 34 - _TreatmentTime * 7));
                    float drainRing = saturate(0.5 + 0.5 * cos(outlet * 29 + _TreatmentTime * 5));
                    float2 center = input.tankPosition + float2(0.0001, 0);
                    float swirl = saturate(0.5 + 0.5 * cos(length(center) * 22 - atan2(center.y, center.x) * 2 - _TreatmentTime * 2.1));
                    float rings = fillRing * fillRing * saturate(1 - inlet * 1.25) * _TreatmentState.y
                        + drainRing * drainRing * saturate(1 - outlet * 1.5) * _TreatmentState.z;
                    color += _RippleColor.rgb * (0.26 * rings + 0.18 * swirl * swirl * _TreatmentState.x) * top;
                }

                half fresnel = 0.02 + 0.98 * pow(1 - saturate(dot(normal, viewDirection)), 5);
                half exponent = exp2(3 + _Smoothness * 8);
                half specular = pow(saturate(dot(normal, halfDirection)), exponent);
                color += _LightColor0.rgb * specular * lerp(0.15, 0.8, _Smoothness) * top;
                half3 reflectedDirection = reflect(-viewDirection, normal);
                half4 environment = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectedDirection, (1 - _Smoothness) * 6);
                color = lerp(color, DecodeHDR(environment, unity_SpecCube0_HDR),
                    saturate((0.08 + fresnel * 0.8) * _ReflectionStrength * top));

                if (_Aeration > 0.5 && top > 0.001)
                {
                    float2 cell = frac((input.tankPosition + 0.5) * float2(5, 4)) - 0.5;
                    float r = length(cell);
                    float ring = saturate(0.5 + 0.5 * cos(r * 30 - _Time.y * 8));
                    color += _RippleColor.rgb * ring * ring * (1 - smoothstep(0.14, 0.48, r)) * 0.29 * top;
                }
                fixed4 result = fixed4(color, lerp(1, lerp(0.26, 0.57, top), max(_Aeration, _Mixing)));
                UNITY_APPLY_FOG(input.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
