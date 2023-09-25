Shader "Hidden/MorphologicalAntialiasing/Blending" 
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
            #include "Blending.hlsl"

            ENDHLSL
        }
    }
}