using UnityEngine;

public enum CylinderGeneratedFaceType
{
    Circle,
    Rectangle
}

public class CylinderGeneratedFace : MonoBehaviour
{
    [Header("Face")]
    public int faceId;

    public CylinderGeneratedFaceType faceType;

    [Header("Circle Geometry")]
    public float radius;

    [Header("Rectangle Geometry")]
    public float circumference;
    public float cylinderHeight;

    public bool IsCircle =>
        faceType ==
        CylinderGeneratedFaceType.Circle;

    public bool IsRectangle =>
        faceType ==
        CylinderGeneratedFaceType.Rectangle;
}