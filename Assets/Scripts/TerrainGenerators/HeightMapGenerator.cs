using UnityEngine;


public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
    {
        float[,] values = Generator.GenerateNoiseMap(width, height, settings.meshNoiseSettings, sampleCenter);
        AnimationCurve heightCurveThreadSave = new AnimationCurve(settings.heightCurve.keys);
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= heightCurveThreadSave.Evaluate(values[i, j]) * settings.heightMultiplier;

                if (values[i, j] < minVal)
                {
                    minVal = values[i, j];
                }
                if (values[i, j] > maxVal)
                {
                    maxVal = values[i, j];
                }
            }
        }

        return new HeightMap(values, minVal, maxVal);
    }
}

[System.Serializable]
public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] heightValues, float min, float max)
    {
        values = heightValues;
        minValue = min;
        maxValue = max;
    }
}