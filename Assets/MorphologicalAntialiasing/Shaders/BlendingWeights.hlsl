#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Using a define to use a descriptive name.
#define EDGES_TEXTURE _BlitTexture
#define MAX_PX_DISTANCE 9
#define AREA_SIZE (MAX_PX_DISTANCE * 5)

TEXTURE2D_X(_AreaLookupTexture);
float2 _TexelSize;
uint _MaxSearchDistance;

// Here we use bilinear filtering to check 2 pixels at once.
float Search(in float2 uv, in float2 stepMul, in uint channel)
{
    uv += stepMul * (1.5).xx * _TexelSize;

    float e = 0;

    // TODO do-while?
    uint i;
    UNITY_LOOP
    for (i = 0; i != _MaxSearchDistance; ++i)
    {
        float2 edge = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_LinearClamp, uv, 0).rg;
        //e = edge[channel];
        // TODO better way?
        e = lerp(edge.x, edge.y, channel);
        
        UNITY_FLATTEN
        if (e < 0.9)
        {
            break;
        }

        uv += stepMul * (2.0).xx * _TexelSize;
    }

    return 2.0 * min(i - e, MAX_PX_DISTANCE);
}

float SearchXLeft(float2 uv)
{
    return -Search(uv, float2(-1, 0), 1);
}

float SearchXRight(float2 uv)
{
    return Search(uv, float2(1, 0), 1);
}

float SearchYUp(float2 uv)
{
    return -Search(uv, float2(0, -1), 0);
}

float SearchYDown(float2 uv)
{
    return Search(uv, float2(0, 1), 0);
}

float2 Area(float2 dist, float e1, float e2)
{
    float2 pxCoords = MAX_PX_DISTANCE * round(float2(e1, e2) * 4.0) + dist;
    float2 texCoords = pxCoords / (AREA_SIZE - 1);
    return SAMPLE_TEXTURE2D_LOD(_AreaLookupTexture, sampler_PointClamp, texCoords, 0).rg;
}

half4 Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;

    float4 weights = 0.0;
    
    float2 e = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_PointClamp, uv, 0).rg;

    // Edge at north.
    UNITY_BRANCH
    if (e.g > 0.0)
    {
        float2 d = float2(SearchXLeft(uv), SearchXRight(uv));

        float4 coords = mad(float4(d.x, -0.25, d.y + 1, -0.25), _TexelSize.xyxy, uv.xyxy);

        float e1 = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_LinearClamp, coords.xy, 0).r;
        float e2 = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_LinearClamp, coords.zw, 0).r;
        weights.rg = Area(abs(d), e1, e2);
    }

    // Edge at north.
    UNITY_BRANCH
    if (e.r > 0.0)
    {
        float2 d = float2(SearchYUp(uv), SearchYDown(uv));

        float4 coords = mad(float4(-0.25, d.x, -0.25, d.y + 1), _TexelSize.xyxy, uv.xyxy);

        float e1 = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_LinearClamp, coords.xy, 0).g;
        float e2 = SAMPLE_TEXTURE2D_X_LOD(EDGES_TEXTURE, sampler_LinearClamp, coords.zw, 0).g;
        weights.ba = Area(abs(d), e1, e2);
    }

    return weights;
}
