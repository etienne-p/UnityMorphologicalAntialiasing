using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

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
        public int MaxSearchDistance;
        public SubPass SubPass;
    }

// TODO Handle domain reload? (resources issue)

    public class MorphologicalAntialiasing : ScriptableRendererFeature
    {
        [SerializeField, Range(0, 1)] float m_Threshold;
        [SerializeField] float m_MaxSmooth = 9;
        [SerializeField] int m_MaxSearchDistance = 9;
        [SerializeField] SubPass m_SubPass;
        [SerializeField] RenderPassEvent m_RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

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
            // TODO rename "Custom"
            m_DetectEdgesMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/DetectEdges"));
            m_BlendingWeightsMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/BlendingWeights"));
            m_BlendingMaterial = CoreUtils.CreateEngineMaterial(
                LoadShader("Hidden/MorphologicalAntialiasing/Blending"));

            // TODO Use same format as camera.
            m_CopyColorHandle = RTHandles.Alloc(Vector2.one, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                dimension: TextureDimension.Tex2D, name: "CopyColor");
            // TODO No need for 4 channels
            m_EdgesHandle = RTHandles.Alloc(Vector2.one, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                dimension: TextureDimension.Tex2D, name: "Edges");
            m_BlendingWeightsHandle = RTHandles.Alloc(Vector2.one, colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                dimension: TextureDimension.Tex2D, name: "BlendingWeights");
            m_StencilHandle = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth32,
                dimension: TextureDimension.Tex2D, name: "Stencil");

            AreaLookup.GenerateLookup(ref m_AreaLookupTexture, 9, m_MaxSmooth);

            m_RenderPass = new MorphologicalAntialiasingPass(m_DetectEdgesMaterial, m_BlendingWeightsMaterial, m_BlendingMaterial);
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
                    MaxSearchDistance = m_MaxSearchDistance,
                    Threshold = m_Threshold,
                    SubPass = m_SubPass
                };

                m_RenderPass.SetPassData(passData);
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