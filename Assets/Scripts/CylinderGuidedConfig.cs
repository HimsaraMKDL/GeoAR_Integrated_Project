using UnityEngine;

[System.Serializable]
public class CylinderGuidedConfig
{
    [Header("Cylinder Geometry")]
    public float radius;
    public float height;

    public float Circumference
    {
        get
        {
            return 2f * Mathf.PI * radius;
        }
    }

    public float Diameter
    {
        get
        {
            return radius * 2f;
        }
    }

    public bool IsReady
    {
        get
        {
            return radius > 0f &&
                   height > 0f;
        }
    }

    public CylinderGuidedConfig(
        float radius,
        float height)
    {
        this.radius = radius;
        this.height = height;
    }
}