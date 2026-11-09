Shader "Boiler/Tank Aeration Bubbles"
{
    Properties
    {
        [HideInInspector] _Mixing ("Mixing Preview", Float) = 0
        [HideInInspector] _MixingStartTime ("Mixing Start Time", Float) = 0
        [HideInInspector] _MixingWaterTint ("Water Tint", Color) = (0.035, 0.31, 0.46, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-5" "RenderType"="Transparent" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TankMixingFlow.cginc"
            float _Mixing;
            half4 _MixingWaterTint;

            struct appdata { float4 vertex:POSITION; float2 corner:TEXCOORD0; float4 seed:TEXCOORD1; };
            struct v2f { float4 position:SV_POSITION; float2 corner:TEXCOORD0; half fade:TEXCOORD1; half clarity:TEXCOORD2; };

            v2f vert(appdata input)
            {
                v2f output;
                float phase = frac(input.seed.z + _Time.y * lerp(0.27, 0.52, input.seed.w));
                float sway = sin(_Time.y * 3.1 + input.seed.z * 30 + phase * 7);
                float3 local = float3(input.seed.x + sway * 0.037, lerp(-0.47, 0.46, phase),
                    input.seed.y + cos(phase * 10 + input.seed.w * 21) * 0.03);
                float3 tangent = float3(1, 0, 0);
                if (_Mixing > 0.5)
                {
                    float circulation = MixingPhase(_Time.y);
                    local = MixingPosition(input.seed, circulation);
                    tangent = MixingPosition(input.seed, circulation + 0.025) - local;
                }
                float3 view = UnityObjectToViewPos(local);
                float waterHeight = length(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                float clearance = (0.5 - abs(local.y)) * waterHeight;
                float sizePulse = _Mixing > 0.5 ? 1 : (0.76 + 0.24 * phase);
                float radius = min(lerp(0.009, 0.025, input.seed.w) * sizePulse, clearance / lerp(1, 1.6, _Mixing));
                float2 offset = input.corner * radius;
                if (_Mixing > 0.5)
                {
                    float2 projected = mul((float3x3)UNITY_MATRIX_MV, tangent).xy;
                    float2 direction = projected / max(length(projected), 0.0001);
                    offset = (direction * input.corner.x * 1.55 + float2(-direction.y, direction.x) * input.corner.y * 0.32) * radius;
                }
                view.xy += offset;
                output.position = mul(UNITY_MATRIX_P, float4(view, 1));
                output.corner = input.corner;
                output.fade = _Mixing > 0.5 ? 0.7 : smoothstep(0, 0.07, phase) * (1 - smoothstep(0.85, 1, phase));
                output.clarity = 1;
                if (_Mixing > 0.5)
                {
                    float3 world = mul(unity_ObjectToWorld, float4(local, 1)).xyz;
                    float3 toCamera = _WorldSpaceCameraPos.xyz - world;
                    float cameraDistance = length(toCamera);
                    float3 direction = mul((float3x3)unity_WorldToObject, toCamera / max(cameraDistance, 0.0001));
                    float3 signs = lerp(-1, 1, step(0, direction));
                    float3 safeDirection = signs * max(abs(direction), 0.0001);
                    float3 exitDistances = (signs * 0.5 - local) / safeDirection;
                    float immersedDistance = max(0, min(cameraDistance, min(exitDistances.x, min(exitDistances.y, exitDistances.z))));
                    float depth = saturate(0.5 - local.y);
                    output.clarity = exp(-immersedDistance * 1.15) * lerp(1, 0.66, depth);
                }
                return output;
            }

            fixed4 frag(v2f input):SV_Target
            {
                float r = length(input.corner);
                float edge = 1 - smoothstep(0.82, 1.0, r);
                float rim = smoothstep(0.48, 0.82, r);
                float shine = 1 - smoothstep(0.08, 0.42, length(input.corner - float2(-0.31, 0.32)));
                if (_Mixing > 0.5)
                {
                    half3 submerged = lerp(_MixingWaterTint.rgb, half3(0.38, 0.86, 0.94), input.clarity);
                    float softEdge = exp(-r * r * lerp(3.7, 2.1, input.clarity)) * edge;
                    return fixed4(submerged, softEdge * lerp(0.16, 0.53, input.clarity) * input.fade);
                }
                return fixed4(lerp(half3(0.30, 0.76, 0.92), half3(0.91, 0.99, 1), shine),
                    edge * (0.10 + rim * 0.46 + shine * 0.32) * input.fade);
            }
            ENDCG
        }
    }
}
