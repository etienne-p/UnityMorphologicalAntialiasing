#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D_X(_CameraOpaqueTexture);
TEXTURE2D_X(_CameraNormalsTexture);

float2 _TexelSize;
float _Threshold;

#define PI 3.14159265359
#define PI_REC 0.31830988618 // PI reciprocal.

// Angle between 2 vectors.
float GetAngle(float3 from, float3 to)
{
    return acos(dot(from, to) / (length(from) * length(to)));
}

float SampleDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0);
}

float SampleLuminance(float2 uv)
{
    float3 color = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_PointClamp, uv, 0).rgb;
    return 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b;
}

float3 SampleNormal(float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CameraNormalsTexture, sampler_PointClamp, uv, 0).rgb;
}

float4 GetDepthDelta(float2 uv)
{
    float depth =       SampleDepth(uv);
    float depthLeft =   SampleDepth(uv + _TexelSize * float2(-1,  0));
    float depthTop =    SampleDepth(uv + _TexelSize * float2( 0, -1));
    float depthRight =  SampleDepth(uv + _TexelSize * float2( 1,  0));
    float depthBottom = SampleDepth(uv + _TexelSize * float2( 0,  1));

    return abs(depth.xxxx - float4(depthLeft, depthTop, depthRight, depthBottom));
}

float4 FragDepth(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float4 delta = GetDepthDelta(input.texcoord.xy);
    float4 edges = step(_Threshold.xxxx, delta);

    if (dot(edges, 1.0) == 0.0)
    {
        discard;
    }
    
    return float4(edges.xy, 0, 1);
}

float4 FragLuminance(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;

    float lum =       SampleLuminance(uv);
    float lumLeft =   SampleLuminance(uv + _TexelSize * float2(-1,  0));
    float lumTop =    SampleLuminance(uv + _TexelSize * float2( 0, -1));
    float lumRight =  SampleLuminance(uv + _TexelSize * float2( 1,  0));
    float lumBottom = SampleLuminance(uv + _TexelSize * float2( 0,  1));

    float4 delta = abs(lum.xxxx - float4(lumLeft, lumTop, lumRight, lumBottom));
    float4 edges = step(_Threshold.xxxx, delta);

    if (dot(edges, 1.0) == 0.0)
    {
        discard;
    }
    
    return float4(edges.xy, 0, 1);
}

float4 GetNormalsDelta(float2 uv)
{
    float3 normal =       SampleNormal(uv);
    float3 normalLeft =   SampleNormal(uv + _TexelSize * float2(-1,  0));
    float3 normalTop =    SampleNormal(uv + _TexelSize * float2( 0, -1));
    float3 normalRight =  SampleNormal(uv + _TexelSize * float2( 1,  0));
    float3 normalBottom = SampleNormal(uv + _TexelSize * float2( 0,  1));

    return  abs(float4(
        GetAngle(normal, normalLeft), 
        GetAngle(normal, normalTop), 
        GetAngle(normal, normalRight), 
        GetAngle(normal, normalBottom))) * PI_REC; // Scaling with respect to threshold range.
}

float4 FragNormal(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float4 delta = GetNormalsDelta(input.texcoord.xy);
    
    float4 edges = step(_Threshold.xxxx, delta);

    if (dot(edges, 1.0) == 0.0)
    {
        discard;
    }
    
    return float4(edges.xy, 0, 1);
}

float4 FragDepthAndNormal(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    float4 normalsDelta = GetNormalsDelta(uv);
    float4 depthDelta = GetDepthDelta(uv);
    float4 delta = max(normalsDelta * 0.1, depthDelta);
    
    float4 edges = step(_Threshold.xxxx, delta);

    if (dot(edges, 1.0) == 0.0)
    {
        discard;
    }
    
    return float4(edges.xy, 0, 1);
}