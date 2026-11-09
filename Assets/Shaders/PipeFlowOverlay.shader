Shader "Boiler/Pipe Water Ripples"
{
    Properties
    {
        _Color ("Cold Water Tint", Color) = (0.08, 0.65, 0.95, 1)
        _HighlightColor ("Cold Ripple Highlight", Color) = (0.55, 0.92, 1, 1)
        _HotColor ("Hot Water Tint", Color) = (0.95, 0.21, 0.045, 1)
        _HotHighlightColor ("Hot Ripple Highlight", Color) = (1, 0.68, 0.22, 1)
        _NeutralColor ("Neutral Water Tint", Color) = (0.35, 0.49, 0.55, 1)
        _NeutralHighlightColor ("Neutral Ripple Highlight", Color) = (0.72, 0.82, 0.86, 1)
        _FlowVector ("World Flow Direction", Vector) = (1, 1, 1, 0)
        _Speed ("Visual Flow Speed", Float) = 0.35
        _Spacing ("Ripple Spacing", Float) = 0.55
        _Opacity ("Flow Strength", Range(0, 1)) = 0.8
        _SurfaceOffset ("Surface Offset", Float) = 0.0015
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color, _HighlightColor;
            fixed4 _HotColor, _HotHighlightColor;
            fixed4 _NeutralColor, _NeutralHighlightColor;
            float4 _FlowVector;
            float _Speed, _Spacing, _Opacity, _SurfaceOffset;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct v2f
            {
                float4 position:SV_POSITION;
                float3 worldPosition:TEXCOORD0;
                half3 worldNormal:TEXCOORD1;
                UNITY_FOG_COORDS(2)
                half2 medium:TEXCOORD3;
            };
            v2f vert(appdata input)
            {
                v2f output;
                float3 normal = UnityObjectToWorldNormal(input.normal);
                float3 position = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldPosition = position;
                output.worldNormal = normal;
                output.medium = input.color.rb * (1 - input.color.g);
                output.position = UnityWorldToClipPos(position + normal * _SurfaceOffset);
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }
            fixed4 frag(v2f input):SV_Target
            {
                // World-space ripples remain continuous across adjoining CAD pipe pieces.
                float travel = dot(input.worldPosition, _FlowVector.xyz) - _Time.y * _Speed;
                float phase = travel * 6.2831853 / max(_Spacing, 0.05);
                float warp = sin(dot(input.worldPosition, float3(11.7, 8.3, 13.1)) - _Time.y * 1.8);
                half wave = saturate(0.5 + 0.5 * sin(phase + 0.22 * warp));
                half hot = input.medium.x;
                half cold = input.medium.y;
                half squared = wave * wave;
                half fourth = squared * squared;
                half crest = lerp(lerp(fourth, fourth * fourth, cold), squared, hot);
                half smallRipples = 0.5 + 0.5 * sin(phase * 2.3 + warp * 1.2);
                half3 normal = normalize(input.worldNormal);
                half3 view = normalize(UnityWorldSpaceViewDir(input.worldPosition));
                half rim = 1 - saturate(abs(dot(normal, view)));
                half strength = (0.045 + crest * 0.68 + smallRipples * 0.045 + rim * rim * 0.08) * _Opacity;
                half3 tint = lerp(lerp(_NeutralColor.rgb, _Color.rgb, cold), _HotColor.rgb, hot);
                half3 highlight = lerp(lerp(_NeutralHighlightColor.rgb, _HighlightColor.rgb, cold), _HotHighlightColor.rgb, hot);
                half3 color = lerp(tint, highlight, crest * 0.85);
                fixed4 result = fixed4(color, saturate(strength));
                UNITY_APPLY_FOG_COLOR(input.fogCoord, result, fixed4(0, 0, 0, 0));
                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
