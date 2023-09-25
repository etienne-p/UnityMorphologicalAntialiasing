﻿using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MorphologicalAntialiasing
{
    class MorphologicalAntialiasingPass : ScriptableRenderPass
    {
        static class ShaderIds
        {
            public static readonly int _Threshold = Shader.PropertyToID("_Threshold");
            public static readonly int _TexelSize = Shader.PropertyToID("_TexelSize");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");
            public static readonly int _AreaLookupTexture = Shader.PropertyToID("_AreaLookupTexture");
            public static readonly int _MaxDistance = Shader.PropertyToID("_MaxDistance");
        }

        const string k_Name = "MorphologicalAntialiasing";

        readonly ProfilingSampler m_ProfilingSampler = new(k_Name);

        Material m_DetectEdgesMaterial;
        Material m_BlendingWeightsMaterial;
        Material m_BlendingMaterial;
        RTHandle m_ColorTarget;
        RTHandle m_CopyColorTarget;
        RTHandle m_EdgesTarget;
        RTHandle m_BlendingWeightsTarget;
        RTHandle m_StencilTarget;
        SubPass m_SubPass;

        public Vector2 Yolo;
        
        public MorphologicalAntialiasingPass(Material detectEdgesMaterial, Material blendingWeightsMaterial,
            Material blendingMaterial)
        {
            m_DetectEdgesMaterial = detectEdgesMaterial;
            m_BlendingWeightsMaterial = blendingWeightsMaterial;
            m_BlendingMaterial = blendingMaterial;
        }

        public void SetPassData(PassData data)
        {
            m_ColorTarget = data.ColorHandle;
            m_CopyColorTarget = data.CopyColorHandle;
            m_EdgesTarget = data.EdgesHandle;
            m_BlendingWeightsTarget = data.BlendingWeightsHandle;
            m_StencilTarget = data.StencilHandle;
            m_SubPass = data.SubPass;

            m_DetectEdgesMaterial.SetFloat(ShaderIds._Threshold, data.Threshold);
            m_BlendingWeightsMaterial.SetTexture(ShaderIds._AreaLookupTexture, data.AreaLookupTexture);
            // Prevent zero iteration, would lead to infinite loop in shader.
            m_BlendingWeightsMaterial.SetInt(ShaderIds._MaxDistance, math.max(1, data.MaxDistance));
            m_BlendingMaterial.SetTexture(ShaderIds._BlendTexture, data.BlendingWeightsHandle);
            
            var rt = data.ColorHandle.rt;
            var texelSize = new Vector2(1f / rt.width, 1f / rt.height);
            m_DetectEdgesMaterial.SetVector(ShaderIds._TexelSize, texelSize);
            m_BlendingWeightsMaterial.SetVector(ShaderIds._TexelSize, texelSize);
            m_BlendingMaterial.SetVector(ShaderIds._TexelSize, texelSize);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_ColorTarget);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_BlendingMaterial.SetVector("_YOLO", Yolo);
            
            Assert.IsNotNull(m_DetectEdgesMaterial);

            var cmd = CommandBufferPool.Get(k_Name);
            //using (new ProfilingScope(cmd, m_ProfilingSampler))

            // Detect edges.
            CoreUtils.SetRenderTarget(cmd, m_EdgesTarget, m_StencilTarget, ClearFlag.ColorStencil);
            BlitCameraTexture(cmd, m_ColorTarget, m_DetectEdgesMaterial, 0);

            // Evaluate blending weights.
            CoreUtils.SetRenderTarget(cmd, m_BlendingWeightsTarget, m_StencilTarget, ClearFlag.Color);
            BlitCameraTexture(cmd, m_EdgesTarget, m_BlendingWeightsMaterial, 0);
            
            // Blend with neighborhood.
            Blitter.BlitCameraTexture(cmd, m_ColorTarget, m_CopyColorTarget, m_BlendingMaterial, 0);


            var finalBlitSrc = m_CopyColorTarget;
            
            switch (m_SubPass)
            {
                case SubPass.Default:
                    break;
                case SubPass.DetectEdges:
                    finalBlitSrc = m_EdgesTarget;
                    break;
                case SubPass.BlendWeights:
                    finalBlitSrc = m_BlendingWeightsTarget;
                    break;
            }

            Blitter.BlitCameraTexture(cmd, finalBlitSrc, m_ColorTarget);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        static void BlitCameraTexture(CommandBuffer cmd, RTHandle source, Material material, int pass)
        {
            var viewportScale = source.useScaling
                ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y)
                : Vector2.one;
            Blitter.BlitTexture(cmd, source, viewportScale, material, pass);
        }
    }
}