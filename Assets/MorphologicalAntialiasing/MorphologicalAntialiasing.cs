using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MorphologicalAntialiasing
{
    enum SubPass
    {
        Default,
        DetectEdges,
        BlendWeights
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
        public SubPass SubPass;
    }

// TODO Handle domain reload? (resources issue)

    public class MorphologicalAntialiasing : ScriptableRendererFeature
    {
        [SerializeField, Range(0, 1)] float m_Threshold;
        [SerializeField] int m_MaxSmooth = 9;
        [SerializeField] int m_MaxDistance = 9;
        [SerializeField] SubPass m_SubPass;
        [SerializeField] RenderPassEvent m_RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField] Vector2 m_Yolo;

        Material m_DetectEdgesMaterial;
        Material m_BlendingWeightsMaterial;
        Material m_BlendingMaterial;
        RTHandle m_CopyColorHandle;
        RTHandle m_EdgesHandle;
        RTHandle m_BlendingWeightsHandle;
        RTHandle m_StencilHandle;
        MorphologicalAntialiasingPass m_RenderPass;
        Texture2D m_AreaLookupTexture;

        /// <inheritdoc/>
        public override void Create()
        {
            m_MaxDistance = math.max(2, m_MaxDistance);
            m_MaxSmooth = math.max(2, m_MaxSmooth);
            AreaLookup.GenerateLookup(ref m_AreaLookupTexture, m_MaxDistance, m_MaxSmooth);

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
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.msaaSamples = 1;
            cameraTargetDescriptor.depthBufferBits = (int)DepthBits.None;
            var edgesDescriptor = cameraTargetDescriptor;
            edgesDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var weightsDescriptor = cameraTargetDescriptor;
            weightsDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var stencilDescriptor = cameraTargetDescriptor;
            stencilDescriptor.graphicsFormat = GraphicsFormat.None;
            stencilDescriptor.depthBufferBits = (int)DepthBits.Depth32;

            RenderingUtils.ReAllocateIfNeeded(ref m_CopyColorHandle, cameraTargetDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "CopyColor");
            RenderingUtils.ReAllocateIfNeeded(ref m_EdgesHandle, edgesDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "Edges");
            RenderingUtils.ReAllocateIfNeeded(ref m_BlendingWeightsHandle, weightsDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "BlendingWeights");
            RenderingUtils.ReAllocateIfNeeded(ref m_StencilHandle, stencilDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "Stencil");

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);

                var passData = new PassData
                {
                    ColorHandle = renderer.cameraColorTargetHandle,
                    CopyColorHandle = m_CopyColorHandle,
                    EdgesHandle = m_EdgesHandle,
                    BlendingWeightsHandle = m_BlendingWeightsHandle,
                    StencilHandle = m_StencilHandle,
                    AreaLookupTexture = m_AreaLookupTexture,
                    MaxDistance = m_MaxDistance,
                    Threshold = m_Threshold,
                    SubPass = m_SubPass
                };

                m_RenderPass.SetPassData(passData);

                m_RenderPass.Yolo = m_Yolo;
            }
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