Shader "Hidden/MorphologicalAntialiasing/BlendingWeights" 
{
    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
            }
            
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "BlendingWeights.hlsl"

            ENDHLSL
        }
    }
}