Shader "Custom/PipeWaterShader"
{
    Properties
    {
        // 管壁参数
        _PipeColor ("管壁颜色", Color) = (0.7,0.7,0.7,1)
        _PipeTex ("管壁贴图", 2D) = "white" {}
        _PipeAlpha ("管壁透明度", Range(0,1)) = 1.0
        
        // 水流参数
        _WaterColor ("水颜色", Color) = (0.2,0.6,1.0,1.0)
        _WaterTex ("水纹贴图", 2D) = "white" {}
        _FlowSpeed ("水流速度XY", Vector) = (0,0.6,0,0)
        _WaterIntensity ("水流显示强度", Range(0,1)) = 0.0
        
        _Glossiness ("光滑度", Range(0,1)) = 0.6
        _Metallic ("金属度", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:blend
        #pragma target 3.0

        sampler2D _PipeTex;
        sampler2D _WaterTex;

        struct Input
        {
            float2 uv_PipeTex;
            float2 uv_WaterTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _PipeColor;
        fixed4 _WaterColor;
        float _PipeAlpha;
        float _WaterIntensity;
        float4 _FlowSpeed;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. 基础管壁金属层
            fixed4 pipeLayer = tex2D (_PipeTex, IN.uv_PipeTex) * _PipeColor;
            
            // 2. 水流层 + 时间驱动UV自动滚动
            float2 flowUV = IN.uv_WaterTex + _Time.y * _FlowSpeed.xy;
            fixed4 waterLayer = tex2D (_WaterTex, flowUV) * _WaterColor;

            // 3. 管壁基础属性
            o.Albedo = pipeLayer.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // 4. 水流放入自发光通道，不受金属高光干扰
            o.Emission = waterLayer.rgb * _WaterIntensity;

            // 5. 整体透明通道
            o.Alpha = _PipeAlpha;
        }
        ENDCG
    }
    FallBack "Standard"
}
