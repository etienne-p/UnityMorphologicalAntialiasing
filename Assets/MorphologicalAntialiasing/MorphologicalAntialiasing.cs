using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MorphologicalAntialiasing
{
    enum IntermediateBufferType
    {
        Default,
        DetectEdges,
        BlendWeights
    }

    enum EdgeDetectMode
    {
        Depth = 0,
        Luminance = 1,
        Normals = 2,
        DepthAndNormals = 3
    }

    struct PassData
    {
        public RTHandle ColorHandle;
        public RTHandle CopyColorHandle;
        public RTHandle EdgesHandle;
        public RTHandle BlendingWeightsHandle;
        public RTHandle StencilHandle;
        public Texture AreaLookupTexture;
        public float Threshold;
        public int MaxDistance;
        public int MaxSearchSteps;
        public EdgeDetectMode EdgeDetectMode;
        public IntermediateBufferType IntermediateBufferType;
    }

    class MorphologicalAntialiasing : ScriptableRendererFeature
    {
        [SerializeField] RenderPassEvent m_RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField] IntermediateBufferType m_IntermediateBufferType;
        [SerializeField] EdgeDetectMode m_EdgeDetectMode;
        [SerializeField, Range(0, 1)] float m_Threshold;
        [SerializeField, Range(4, 32)] int m_MaxDistance = 18;

        Material m_DetectEdgesMaterial;
        Material m_BlendingWeightsMaterial;
        Material m_BlendingMaterial;
        RTHandle m_CopyColorHandle;
        RTHandle m_EdgesHandle;
        RTHandle m_BlendingWeightsHandle;
        RTHandle m_StencilHandle;
        MorphologicalAntialiasingPass m_RenderPass;
        Texture2D m_AreaLookupTexture;

        public override void Create()
        {
            AreaLookup.GenerateLookup(ref m_AreaLookupTexture, math.max(2, m_MaxDistance));

            m_DetectEdgesMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/DetectEdges"));
            m_BlendingWeightsMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/BlendingWeights"));
            m_BlendingMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/Blending"));

            m_RenderPass = new MorphologicalAntialiasingPass(
                m_DetectEdgesMaterial, m_BlendingWeightsMaterial, m_BlendingMaterial);
            m_RenderPass.renderPassEvent = m_RenderPassEvent;
        }

        protected override void Dispose(bool disposing)
        {
            m_CopyColorHandle?.Release();
            m_EdgesHandle?.Release();
            m_BlendingWeightsHandle?.Release();
            m_StencilHandle?.Release();
            CoreUtils.Destroy(m_DetectEdgesMaterial);
            CoreUtils.Destroy(m_BlendingWeightsMaterial);
            CoreUtils.Destroy(m_BlendingMaterial);
            CoreUtils.Destroy(m_AreaLookupTexture);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.msaaSamples = 1;
            cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;

            var edgesDescriptor = cameraTargetDescriptor;
            edgesDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            var weightsDescriptor = cameraTargetDescriptor;
            weightsDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // A bit wasteful as we only need the stencil buffer (not depth).
            var stencilDescriptor = cameraTargetDescriptor;
            stencilDescriptor.graphicsFormat = GraphicsFormat.None;
            stencilDescriptor.depthBufferBits = (int)DepthBits.Depth32;
            stencilDescriptor.stencilFormat = GraphicsFormat.R8_UInt;

            RenderingUtils.ReAllocateIfNeeded(ref m_CopyColorHandle, cameraTargetDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "CopyColor");
            RenderingUtils.ReAllocateIfNeeded(ref m_EdgesHandle, edgesDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "Edges");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlendingWeightsHandle, weightsDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "BlendingWeights");
            RenderingUtils.ReAllocateIfNeeded(ref m_StencilHandle, stencilDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "Stencil");

            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);

            // Gives more precise control for small values, while preserving range.
            var threshold = math.pow(m_Threshold, 4);

            var passData = new PassData
            {
                ColorHandle = renderer.cameraColorTargetHandle,
                CopyColorHandle = m_CopyColorHandle,
                EdgesHandle = m_EdgesHandle,
                BlendingWeightsHandle = m_BlendingWeightsHandle,
                StencilHandle = m_StencilHandle,
                AreaLookupTexture = m_AreaLookupTexture,
                EdgeDetectMode = m_EdgeDetectMode,
                MaxDistance = m_MaxDistance,
                MaxSearchSteps = m_MaxDistance / 2 - 2,
                Threshold = threshold,
                IntermediateBufferType = m_IntermediateBufferType
            };

            m_RenderPass.SetPassData(passData);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(m_RenderPass);
            }
        }

        static Shader LoadShader(string name)
        {
            var shader = Shader.Find(name);
            if (shader == null)
            {
                throw new InvalidOperationException($"Could not find shader \"{name}\".");
            }

            return shader;
        }
    }
}
