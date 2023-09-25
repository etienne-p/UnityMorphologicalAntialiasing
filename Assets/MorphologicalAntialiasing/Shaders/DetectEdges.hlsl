#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float2 _TexelSize;
float _Threshold;

float SampleDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0);
}

half4 Frag (Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;

    float depth =       SampleDepth(input.texcoord.xy);
    float depthLeft =   SampleDepth(uv + _TexelSize * float2(-1, 0));
    float depthRight =  SampleDepth(uv + _TexelSize * float2( 1, 0));
    float depthTop =    SampleDepth(uv + _TexelSize * float2(0, -1));
    float depthBottom = SampleDepth(uv + _TexelSize * float2(0,  1));

    float4 delta = abs(depth.xxxx - float4(depthLeft, depthTop, depthRight, depthBottom));
    float4 edges = step(_Threshold.xxxx, delta);

    // Saves a clear target, but we don;t need to write alot of pixels, maybe better to clear explicitely.
    if (dot(edges, (1.0).xxxx) == 0.0)
    {
        discard;
    }
    
    return float4(edges.xy, 0, 1);
}