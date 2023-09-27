using System;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MorphologicalAntialiasing
{
    static class ReflectionUtility
    {
        static Type GetType(string fullname)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullname);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        static readonly PropertyInfo k_DeferredLightsInfo =
            typeof(UniversalRenderer).GetProperty("deferredLights", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly Type k_DeferredLightsType = GetType("UnityEngine.Rendering.Universal.Internal.DeferredLights");

        static readonly FieldInfo k_GbufferRTHandlesInfo =
            k_DeferredLightsType.GetField("GbufferRTHandles", BindingFlags.Instance | BindingFlags.NonPublic);

        public static RTHandle GetNormalsBuffer(ScriptableRenderer renderer)
        {
            var universalRenderer = renderer as UniversalRenderer;
            var deferredLights = k_DeferredLightsInfo.GetValue(universalRenderer);
            if (deferredLights == null)
            {
                return null;
            }

            var gBufferRTHandles = k_GbufferRTHandlesInfo.GetValue(deferredLights) as RTHandle[];
            if (gBufferRTHandles == null)
            {
                return null;
            }

            return gBufferRTHandles[2];
        }
    }
}
