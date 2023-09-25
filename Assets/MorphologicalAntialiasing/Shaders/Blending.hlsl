#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#define COLOR_TEXTURE _BlitTexture
TEXTURE2D_X(_BlendTexture);

float2 _TexelSize;

half4 Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float4 topLeft = SAMPLE_TEXTURE2D_X(_BlendTexture, sampler_PointClamp, input.texcoord);
    float right = SAMPLE_TEXTURE2D_X(_BlendTexture, sampler_PointClamp, input.texcoord + _TexelSize * float2(0, 1)).g;
    float bottom = SAMPLE_TEXTURE2D_X(_BlendTexture, sampler_PointClamp, input.texcoord + _TexelSize * float2(1, 0)).a;

    float4 a = float4(topLeft.r, right, topLeft.b, bottom);
    float sum = dot(a, 1.0);

    UNITY_BRANCH
    if (sum > 0.0)
    {
        float4 color = 0;
        float4 o = a * _TexelSize.yyxx;

        color = mad(SAMPLE_TEXTURE2D_X(COLOR_TEXTURE, sampler_LinearClamp, input.texcoord + float2(0, -o.r)), a.r, color);
        color = mad(SAMPLE_TEXTURE2D_X(COLOR_TEXTURE, sampler_LinearClamp, input.texcoord + float2(0,  o.g)), a.g, color);
        color = mad(SAMPLE_TEXTURE2D_X(COLOR_TEXTURE, sampler_LinearClamp, input.texcoord + float2(-o.b, 0)), a.b, color);
        color = mad(SAMPLE_TEXTURE2D_X(COLOR_TEXTURE, sampler_LinearClamp, input.texcoord + float2( o.a, 0)), a.a, color);

        return color / sum;
    }
    else
    {
        return SAMPLE_TEXTURE2D_X(COLOR_TEXTURE, sampler_LinearClamp, input.texcoord);
    }
}
