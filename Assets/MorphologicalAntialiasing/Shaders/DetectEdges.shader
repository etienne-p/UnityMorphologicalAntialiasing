Shader "Hidden/MorphologicalAntialiasing/DetectEdges" 
{
    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "DetectEdges.hlsl"
            ENDHLSL
        }
    }
}