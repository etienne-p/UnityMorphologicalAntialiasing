using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace MorphologicalAntialiasing
{
    static class AreaLookup
    {
        readonly struct Line
        {
            readonly float m_YLeft;
            readonly float m_YRight;
            readonly float m_ClampMin;
            readonly float m_ClampMax;

            public Line(float yLeft, float yRight)
            {
                m_YLeft = yLeft;
                m_YRight = yRight;
                m_ClampMin = float.MinValue;
                m_ClampMax = float.MaxValue;
            }

            public Line(float yLeft, float yRight, float clampMin, float clampMax)
            {
                m_YLeft = yLeft;
                m_YRight = yRight;
                m_ClampMin = clampMin;
                m_ClampMax = clampMax;
            }

            public float2 GetArea(float left, float right)
            {
                // x span of the edge.
                var l = left + right + 1;

                // Line equation: ax + b, a is slope.
                var slope = (m_YRight - m_YLeft) / l;

                // Line Y on left and right side of the pixel.
                var pxLeftY = m_YLeft + slope * left;
                var pxRightY = pxLeftY + slope;
                var area = PixelCoverage(pxLeftY, pxRightY);
                var areaOpp = PixelCoverage(-pxLeftY, -pxRightY);

                // The purpose of clamp is to limit area.
                // But in some cases the calculation below can lead to increased area,
                // when triangles become trapezoids.
                pxLeftY = math.clamp(pxLeftY, m_ClampMin, m_ClampMax);
                pxRightY = math.clamp(pxRightY, m_ClampMin, m_ClampMax);
                area = math.min(area, PixelCoverage(pxLeftY, pxRightY));
                areaOpp = math.min(areaOpp, PixelCoverage(-pxLeftY, -pxRightY));

                return new float2(area, areaOpp);
            }
        }

        // Helps with code readability, designed to be temporary. (see Allocator.Temp)
        struct PixelData
        {
            readonly int m_MaxDist;
            readonly int m_Size;
            NativeArray<byte> m_Data;
            int m_OffsetX;
            int m_OffsetY;

            public PixelData(int maxDist)
            {
                m_MaxDist = maxDist;
                m_Size = maxDist * 5;
                m_Data = new NativeArray<byte>(m_Size * m_Size * 4, Allocator.Temp);
                m_OffsetX = 0;
                m_OffsetY = 0;

                // Set solid alpha.
                for (var i = 0; i != m_Size * m_Size; ++i)
                {
                    m_Data[i * 4 + 3] = 255;
                }
            }

            public void SetPattern(int x, int y)
            {
                m_OffsetY = y * m_MaxDist;
                m_OffsetX = x * m_MaxDist;
            }

            // The lookup texture is symmetric along its diagonal, so we set 2 pixels at once.
            public void SetPixel(int x, int y, float2 value)
            {
                var idx0 = (m_OffsetY + y) * m_Size + m_OffsetX + x;
                m_Data[idx0 * 4] = (byte)(value.x * 255f);
                m_Data[idx0 * 4 + 1] = (byte)(value.y * 255f);

                var idx1 = (m_OffsetX + x) * m_Size + m_OffsetY + y;
                m_Data[idx1 * 4] = (byte)(value.x * 255f);
                m_Data[idx1 * 4 + 1] = (byte)(value.y * 255f);
            }

            public void UpdateTexture(ref Texture2D tex)
            {
                if (tex == null || tex.width != m_Size || tex.height != m_Size)
                {
                    CoreUtils.Destroy(tex);
                    tex = new Texture2D(m_Size, m_Size, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
                    tex.filterMode = FilterMode.Point;
                }

                tex.SetPixelData(m_Data, 0);
                tex.Apply();
            }
        }

        static void FillSlope(PixelData pxData, int maxDist, Line line)
        {
            for (var left = 0; left != maxDist; ++left)
            for (var right = 0; right != maxDist; ++right)
            {
                pxData.SetPixel(left, right, line.GetArea(left, right));
            }
        }

        static void FillSlopes(PixelData pxData, int maxDist, Line line1, Line line2)
        {
            for (var left = 0; left != maxDist; ++left)
            for (var right = 0; right != maxDist; ++right)
            {
                var area1 = line1.GetArea(left, right);
                var area2 = line2.GetArea(left, right);

                pxData.SetPixel(left, right, (area1 + area2) * .5f);
            }
        }

        static float PixelCoverage(float left, float right)
        {
            // Two triangles, a: triangle side along edge.
            if (left * right < 0)
            {
                var pos = math.max(left, right);
                var neg = math.min(left, right);
                var a = pos / (pos - neg);
                return math.max(0, pos * a * .5f);
            }

            // A trapezoid.
            return math.max(0, left + right) * .5f;
        }

        public static void GenerateLookup(ref Texture2D tex, int maxDist)
        {
            var pxData = new PixelData(maxDist);

            // Z Patterns.
            pxData.SetPattern(3, 1);
            FillSlope(pxData, maxDist, new Line(.5f, -.5f));

            // L Patterns.
            pxData.SetPattern(1, 0);
            FillSlope(pxData, maxDist, new Line(-.5f, .5f, -5f, 0));
            pxData.SetPattern(3, 0);
            FillSlope(pxData, maxDist, new Line(.5f, -.5f, 0, .5f));

            // U Patterns.
            pxData.SetPattern(1, 1);
            FillSlopes(pxData, maxDist,
                new Line(-.5f, .5f, -5f, 0),
                new Line(.5f, -.5f, -5f, 0));
            pxData.SetPattern(3, 3);
            FillSlopes(pxData, maxDist,
                new Line(.5f, -.5f, 0, .5f),
                new Line(-.5f, .5f, 0, .5f));

            // T Patterns.
            pxData.SetPattern(0, 4);
            FillSlopes(pxData, maxDist,
                new Line(-.5f, .5f, 0, .5f),
                new Line(.5f, -.5f, -.5f, 0f));

            // L-T Patterns.
            pxData.SetPattern(4, 1);
            FillSlopes(pxData, maxDist,
                new Line(.5f, -.5f),
                new Line(-.5f, .5f, -.5f, 0));
            pxData.SetPattern(4, 3);
            FillSlopes(pxData, maxDist,
                new Line(-.5f, .5f),
                new Line(.5f, -.5f, 0, .5f));

            // T-T Patterns.
            pxData.SetPattern(4, 4);
            FillSlopes(pxData, maxDist,
                new Line(-.5f, .5f),
                new Line(.5f, -.5f));

            pxData.UpdateTexture(ref tex);
        }
    }
}