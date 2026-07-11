Shader "Jam24/Flow Edge Fade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _FadeOrigin ("Fade Origin", Vector) = (0,0,0,0)
        [HideInInspector] _FadeDirection ("Fade Direction", Vector) = (1,0,0,0)
        [HideInInspector] _FadeLength ("Fade Length", Float) = 1
        [HideInInspector] _FadeDistance ("Fade Distance", Float) = 0.1
        [HideInInspector] _BlurWholeFlow ("Blur Whole Flow", Float) = 0
        [HideInInspector] _BlurSize ("Blur Size", Float) = 1
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask]
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float flowPosition : TEXCOORD1; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _Color;
            float3 _FadeOrigin, _FadeDirection;
            float _FadeLength, _FadeDistance;
            float _BlurWholeFlow, _BlurSize;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.flowPosition = dot(positionWS - _FadeOrigin, _FadeDirection);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 sharp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 step = _MainTex_TexelSize.xy * _BlurSize;
                half4 blurred = sharp * 4.0;
                blurred += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(step.x, 0.0));
                blurred += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(step.x, 0.0));
                blurred += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0.0, step.y));
                blurred += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0.0, step.y));
                blurred *= 0.125;
                half4 color = lerp(sharp, blurred, saturate(_BlurWholeFlow)) * input.color;
                float distance = max(_FadeDistance, 0.0001);
                float leftFade = smoothstep(0.0, distance, input.flowPosition);
                float rightFade = 1.0 - smoothstep(_FadeLength - distance, _FadeLength, input.flowPosition);
                color.a *= saturate(leftFade * rightFade);
                return color;
            }
            ENDHLSL
        }
    }
}
