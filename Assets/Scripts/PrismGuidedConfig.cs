using System;
using UnityEngine;

public enum GuidedTriangleType
{
    None,
    RightTriangle,
    IsoscelesTriangle
}

public enum GuidedPrismDepth
{
    None,
    Short,
    Medium,
    Long
}

[Serializable]
public class PrismGuidedConfig
{
    public GuidedTriangleType triangleType =
        GuidedTriangleType.None;

    public GuidedPrismDepth depthType =
        GuidedPrismDepth.None;

    public float sideA;
    public float sideB;
    public float sideC;

    public float depth;

    public Vector2 trianglePointA;
    public Vector2 trianglePointB;
    public Vector2 trianglePointC;

    public bool IsReady =>
        triangleType != GuidedTriangleType.None &&
        depthType != GuidedPrismDepth.None;

    public void SetTriangle(
        GuidedTriangleType selectedType)
    {
        triangleType = selectedType;

        switch (selectedType)
        {
            case GuidedTriangleType.RightTriangle:

                /*
                 * 3-4-5 right triangle
                 *
                 * C
                 * |\
                 * | \
                 * |__\
                 * A   B
                 */

                sideA = 3f;
                sideB = 4f;
                sideC = 5f;

                trianglePointA =
                    new Vector2(0f, 0f);

                trianglePointB =
                    new Vector2(3f, 0f);

                trianglePointC =
                    new Vector2(0f, 4f);

                break;

            case GuidedTriangleType.IsoscelesTriangle:

                /*
                 * 5-5-6 isosceles triangle.
                 *
                 *      C
                 *     / \
                 *    /   \
                 * A-------B
                 *
                 * Base = 6
                 * Height = 4
                 * Equal sides = 5
                 */

                sideA = 5f;
                sideB = 5f;
                sideC = 6f;

                trianglePointA =
                    new Vector2(-3f, 0f);

                trianglePointB =
                    new Vector2(3f, 0f);

                trianglePointC =
                    new Vector2(0f, 4f);

                break;

            default:

                sideA = 0f;
                sideB = 0f;
                sideC = 0f;

                trianglePointA =
                    Vector2.zero;

                trianglePointB =
                    Vector2.zero;

                trianglePointC =
                    Vector2.zero;

                break;
        }
    }

    public void SetDepth(
        GuidedPrismDepth selectedDepth)
    {
        depthType = selectedDepth;

        switch (selectedDepth)
        {
            case GuidedPrismDepth.Short:
                depth = 1f;
                break;

            case GuidedPrismDepth.Medium:
                depth = 2f;
                break;

            case GuidedPrismDepth.Long:
                depth = 3f;
                break;

            default:
                depth = 0f;
                break;
        }
    }

    public void Reset()
    {
        triangleType =
            GuidedTriangleType.None;

        depthType =
            GuidedPrismDepth.None;

        sideA = 0f;
        sideB = 0f;
        sideC = 0f;
        depth = 0f;

        trianglePointA =
            Vector2.zero;

        trianglePointB =
            Vector2.zero;

        trianglePointC =
            Vector2.zero;
    }
}