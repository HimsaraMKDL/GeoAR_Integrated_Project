using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SketchDrawingPad : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Drawing Area")]
    [SerializeField] private RawImage drawingArea;

    [Header("Texture Settings")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private int brushSize = 10;

    [Header("Brush Colour")]
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField] private Color backgroundColor = Color.white;

    [Header("Sketch Size Limits")]
    [Range(0.05f, 1f)]
    [SerializeField] private float minimumDetectedSizeRatio = 0.05f;

    private Texture2D drawingTexture;
    private Vector2Int previousPixelPosition;
    private bool isDrawing;
    private bool hasDrawing;

    private int minimumDrawnX;
    private int maximumDrawnX;
    private int minimumDrawnY;
    private int maximumDrawnY;

    // Public access for other scripts.
    public bool HasDrawing => hasDrawing;

    // IMPORTANT:
    // This allows SketchImagePreprocessor and Sentis
    // to access the current sketch texture directly.
    public Texture2D DrawingTexture => drawingTexture;

    private void Awake()
    {
        if (drawingArea == null)
        {
            drawingArea = GetComponent<RawImage>();
        }

        if (drawingArea == null)
        {
            Debug.LogError(
                "SketchDrawingPad: Drawing Area RawImage is not assigned."
            );

            return;
        }

        CreateDrawingTexture();
        ClearDrawing();
    }

    private void CreateDrawingTexture()
    {
        drawingTexture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            false
        );

        drawingTexture.filterMode = FilterMode.Bilinear;
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        drawingArea.texture = drawingTexture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (drawingTexture == null)
        {
            return;
        }

        if (!TryGetTexturePixelPosition(
                eventData.position,
                eventData.pressEventCamera,
                out Vector2Int pixelPosition))
        {
            return;
        }

        isDrawing = true;
        previousPixelPosition = pixelPosition;

        DrawBrush(pixelPosition);
        drawingTexture.Apply();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing || drawingTexture == null)
        {
            return;
        }

        if (!TryGetTexturePixelPosition(
                eventData.position,
                eventData.pressEventCamera,
                out Vector2Int currentPixelPosition))
        {
            return;
        }

        DrawLine(
            previousPixelPosition,
            currentPixelPosition
        );

        previousPixelPosition =
            currentPixelPosition;

        drawingTexture.Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
    }

    private bool TryGetTexturePixelPosition(
        Vector2 screenPosition,
        Camera eventCamera,
        out Vector2Int pixelPosition)
    {
        pixelPosition = Vector2Int.zero;

        if (drawingArea == null)
        {
            return false;
        }

        RectTransform drawingRect =
            drawingArea.rectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawingRect,
                screenPosition,
                eventCamera,
                out Vector2 localPosition))
        {
            return false;
        }

        Rect rect =
            drawingRect.rect;

        float normalizedX =
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPosition.x
            );

        float normalizedY =
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPosition.y
            );

        int pixelX =
            Mathf.RoundToInt(
                normalizedX *
                (textureWidth - 1)
            );

        int pixelY =
            Mathf.RoundToInt(
                normalizedY *
                (textureHeight - 1)
            );

        pixelX =
            Mathf.Clamp(
                pixelX,
                0,
                textureWidth - 1
            );

        pixelY =
            Mathf.Clamp(
                pixelY,
                0,
                textureHeight - 1
            );

        pixelPosition =
            new Vector2Int(
                pixelX,
                pixelY
            );

        return true;
    }

    private void DrawLine(
        Vector2Int start,
        Vector2Int end)
    {
        int distance =
            Mathf.CeilToInt(
                Vector2Int.Distance(
                    start,
                    end
                )
            );

        distance =
            Mathf.Max(
                distance,
                1
            );

        for (int i = 0; i <= distance; i++)
        {
            float interpolation =
                i / (float)distance;

            int x =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start.x,
                        end.x,
                        interpolation
                    )
                );

            int y =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start.y,
                        end.y,
                        interpolation
                    )
                );

            DrawBrush(
                new Vector2Int(
                    x,
                    y
                )
            );
        }
    }

    private void DrawBrush(
        Vector2Int centrePosition)
    {
        if (drawingTexture == null)
        {
            return;
        }

        int radius =
            Mathf.Max(
                1,
                brushSize / 2
            );

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (
                    x * x +
                    y * y >
                    radius * radius
                )
                {
                    continue;
                }

                int pixelX =
                    centrePosition.x + x;

                int pixelY =
                    centrePosition.y + y;

                if (
                    pixelX < 0 ||
                    pixelX >= textureWidth ||
                    pixelY < 0 ||
                    pixelY >= textureHeight
                )
                {
                    continue;
                }

                drawingTexture.SetPixel(
                    pixelX,
                    pixelY,
                    brushColor
                );

                UpdateDrawingBounds(
                    pixelX,
                    pixelY
                );
            }
        }

        hasDrawing = true;
    }

    private void UpdateDrawingBounds(
        int pixelX,
        int pixelY)
    {
        minimumDrawnX =
            Mathf.Min(
                minimumDrawnX,
                pixelX
            );

        maximumDrawnX =
            Mathf.Max(
                maximumDrawnX,
                pixelX
            );

        minimumDrawnY =
            Mathf.Min(
                minimumDrawnY,
                pixelY
            );

        maximumDrawnY =
            Mathf.Max(
                maximumDrawnY,
                pixelY
            );
    }

    public void ClearDrawing()
    {
        if (drawingTexture == null)
        {
            return;
        }

        Color[] clearPixels =
            new Color[
                textureWidth *
                textureHeight
            ];

        for (
            int i = 0;
            i < clearPixels.Length;
            i++
        )
        {
            clearPixels[i] =
                backgroundColor;
        }

        drawingTexture.SetPixels(
            clearPixels
        );

        drawingTexture.Apply();

        hasDrawing = false;
        isDrawing = false;

        minimumDrawnX =
            textureWidth;

        maximumDrawnX =
            0;

        minimumDrawnY =
            textureHeight;

        maximumDrawnY =
            0;

        Debug.Log(
            "Sketch drawing area cleared."
        );
    }

    public float GetSketchSizeRatio()
    {
        if (!hasDrawing)
        {
            return minimumDetectedSizeRatio;
        }

        float sketchWidth =
            maximumDrawnX -
            minimumDrawnX +
            1;

        float sketchHeight =
            maximumDrawnY -
            minimumDrawnY +
            1;

        float widthRatio =
            sketchWidth /
            textureWidth;

        float heightRatio =
            sketchHeight /
            textureHeight;

        float largestRatio =
            Mathf.Max(
                widthRatio,
                heightRatio
            );

        return Mathf.Clamp(
            largestRatio,
            minimumDetectedSizeRatio,
            1f
        );
    }

    public string SaveSketchAsPng()
    {
        if (!hasDrawing)
        {
            Debug.LogWarning(
                "The sketch cannot be saved because no drawing exists."
            );

            return string.Empty;
        }

        if (drawingTexture == null)
        {
            Debug.LogError(
                "SketchDrawingPad: Drawing texture is null."
            );

            return string.Empty;
        }

        byte[] pngBytes =
            drawingTexture.EncodeToPNG();

        string folderPath =
            Path.Combine(
                Application.persistentDataPath,
                "SavedSketches"
            );

        if (!Directory.Exists(
                folderPath))
        {
            Directory.CreateDirectory(
                folderPath
            );
        }

        string fileName =
            $"sketch_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        string fullPath =
            Path.Combine(
                folderPath,
                fileName
            );

        File.WriteAllBytes(
            fullPath,
            pngBytes
        );

        Debug.Log(
            $"Sketch saved successfully: {fullPath}"
        );

        return fullPath;
    }
}