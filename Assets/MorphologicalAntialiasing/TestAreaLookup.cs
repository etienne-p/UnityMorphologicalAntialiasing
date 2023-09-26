using UnityEngine;
using UnityEngine.Rendering;

namespace MorphologicalAntialiasing
{
    /// <summary>
    /// A simple component meant to visualize the lookup texture.
    /// </summary>
    [ExecuteAlways]
    class TestAreaLookup : MonoBehaviour
    {
        [SerializeField] float m_GuiScale;
        [SerializeField] int m_MaxDist;

        Texture2D m_Lookup;

        void OnGUI()
        {
            if (m_Lookup != null)
            {
                var rect = new Rect(0, 0, m_Lookup.width * m_GuiScale, m_Lookup.height * m_GuiScale);
                GUI.DrawTexture(rect, m_Lookup);
            }
        }

        void OnEnable()
        {
            AreaLookup.GenerateLookup(ref m_Lookup, m_MaxDist);
        }

        void OnDisable()
        {
            CoreUtils.Destroy(m_Lookup);
            m_Lookup = null;
        }

        void OnValidate()
        {
            AreaLookup.GenerateLookup(ref m_Lookup, m_MaxDist);
        }
    }
}