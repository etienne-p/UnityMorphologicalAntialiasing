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
            #pragma fragment FragDepth
            #include "DetectEdges.hlsl"
            ENDHLSL
        }
        
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
            #pragma fragment FragLuminance
            #include "DetectEdges.hlsl"
            ENDHLSL
        }
    }
}