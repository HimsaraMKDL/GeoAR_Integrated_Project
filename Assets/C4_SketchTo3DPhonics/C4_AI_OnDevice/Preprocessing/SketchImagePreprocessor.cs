using UnityEngine;

public static class SketchImagePreprocessor
{
    private const int TargetSize = 64;
    private const float InvertedThreshold = 30f;

    public static float[] Preprocess(Texture2D sourceTexture)
    {
        if (sourceTexture == null)
        {
            Debug.LogError(
                "SketchImagePreprocessor: Source texture is null."
            );

            return null;
        }

        int width = sourceTexture.width;
        int height = sourceTexture.height;

        Color32[] sourcePixels =
            sourceTexture.GetPixels32();

        byte[] invertedImage =
            new byte[width * height];

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        // -----------------------------------------
        // STEP 1
        // Convert Unity image orientation to the
        // same orientation used by Python/OpenCV.
        //
        // Unity Texture2D:
        // Y = 0 is bottom
        //
        // OpenCV image:
        // Y = 0 is top
        // -----------------------------------------

        for (int y = 0; y < height; y++)
        {
            // IMPORTANT FIX
            int unityY =
                height - 1 - y;

            for (int x = 0; x < width; x++)
            {
                int unityIndex =
                    unityY * width + x;

                int outputIndex =
                    y * width + x;

                Color32 pixel =
                    sourcePixels[unityIndex];

                float gray =
                    (
                        pixel.r +
                        pixel.g +
                        pixel.b
                    ) / 3f;

                // Same as Python:
                // image = 255 - image
                float inverted =
                    255f - gray;

                // Same idea as:
                // cv2.THRESH_TOZERO
                byte value =
                    inverted > InvertedThreshold
                        ? (byte)Mathf.Clamp(
                            Mathf.RoundToInt(inverted),
                            0,
                            255
                        )
                        : (byte)0;

                invertedImage[outputIndex] =
                    value;

                if (value > 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;

                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        // -----------------------------------------
        // STEP 2
        // Make sure a drawing exists
        // -----------------------------------------

        if (maxX < minX ||
            maxY < minY)
        {
            Debug.LogWarning(
                "SketchImagePreprocessor: No drawing pixels found."
            );

            return null;
        }

        // -----------------------------------------
        // STEP 3
        // Crop drawing bounds
        // -----------------------------------------

        int cropWidth =
            maxX - minX + 1;

        int cropHeight =
            maxY - minY + 1;

        int squareSize =
            Mathf.Max(
                cropWidth,
                cropHeight
            );

        byte[] squareImage =
            new byte[
                squareSize *
                squareSize
            ];

        int offsetX =
            (squareSize - cropWidth) / 2;

        int offsetY =
            (squareSize - cropHeight) / 2;

        // -----------------------------------------
        // STEP 4
        // Put cropped sketch in center of
        // black square canvas
        // -----------------------------------------

        for (int y = 0;
             y < cropHeight;
             y++)
        {
            for (int x = 0;
                 x < cropWidth;
                 x++)
            {
                int sourceIndex =
                    (minY + y) *
                    width +
                    (minX + x);

                int destinationIndex =
                    (offsetY + y) *
                    squareSize +
                    (offsetX + x);

                squareImage[
                    destinationIndex
                ] =
                    invertedImage[
                        sourceIndex
                    ];
            }
        }

        // -----------------------------------------
        // STEP 5
        // Resize using CPU bilinear interpolation
        //
        // Avoid Graphics.Blit because graphics
        // APIs can introduce another Y flip.
        // -----------------------------------------

        float[] result =
            ResizeBilinear(
                squareImage,
                squareSize,
                squareSize,
                TargetSize,
                TargetSize
            );

        float minValue = 255f;
        float maxValue = 0f;

        for (int i = 0;
             i < result.Length;
             i++)
        {
            if (result[i] < minValue)
                minValue = result[i];

            if (result[i] > maxValue)
                maxValue = result[i];
        }

        Debug.Log(
            "Sketch preprocessing completed."
        );

        Debug.Log(
            "AI Input Length: " +
            result.Length
        );

        Debug.Log(
            "AI Input Range: " +
            minValue.ToString("F0") +
            " - " +
            maxValue.ToString("F0")
        );

        Debug.Log(
            "Crop Size: " +
            cropWidth +
            " x " +
            cropHeight
        );

        return result;
    }

    private static float[] ResizeBilinear(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight)
    {
        float[] output =
            new float[
                targetWidth *
                targetHeight
            ];

        float xScale =
            sourceWidth /
            (float)targetWidth;

        float yScale =
            sourceHeight /
            (float)targetHeight;

        for (int y = 0;
             y < targetHeight;
             y++)
        {
            float sourceY =
                (y + 0.5f) *
                yScale -
                0.5f;

            int y0 =
                Mathf.Clamp(
                    Mathf.FloorToInt(sourceY),
                    0,
                    sourceHeight - 1
                );

            int y1 =
                Mathf.Clamp(
                    y0 + 1,
                    0,
                    sourceHeight - 1
                );

            float fy =
                sourceY -
                Mathf.Floor(sourceY);

            for (int x = 0;
                 x < targetWidth;
                 x++)
            {
                float sourceX =
                    (x + 0.5f) *
                    xScale -
                    0.5f;

                int x0 =
                    Mathf.Clamp(
                        Mathf.FloorToInt(sourceX),
                        0,
                        sourceWidth - 1
                    );

                int x1 =
                    Mathf.Clamp(
                        x0 + 1,
                        0,
                        sourceWidth - 1
                    );

                float fx =
                    sourceX -
                    Mathf.Floor(sourceX);

                float topLeft =
                    source[
                        y0 *
                        sourceWidth +
                        x0
                    ];

                float topRight =
                    source[
                        y0 *
                        sourceWidth +
                        x1
                    ];

                float bottomLeft =
                    source[
                        y1 *
                        sourceWidth +
                        x0
                    ];

                float bottomRight =
                    source[
                        y1 *
                        sourceWidth +
                        x1
                    ];

                float top =
                    Mathf.Lerp(
                        topLeft,
                        topRight,
                        fx
                    );

                float bottom =
                    Mathf.Lerp(
                        bottomLeft,
                        bottomRight,
                        fx
                    );

                output[
                    y *
                    targetWidth +
                    x
                ] =
                    Mathf.Lerp(
                        top,
                        bottom,
                        fy
                    );
            }
        }

        return output;
    }
}