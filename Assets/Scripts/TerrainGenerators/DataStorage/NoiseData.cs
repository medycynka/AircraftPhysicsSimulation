using UnityEngine;


[CreateAssetMenu(fileName = "NoiseDataConfiguration", menuName = "Procedural Terrain/Noise Data")]
public class NoiseData : DataUpdater
{
    public int seed;
    [Range(0, 50)] public float perlinNoiseScale;
    [Range(1, 10)] public int octaves = 1;
    [Range(0, 1)] public float persistence;
    [Range(1, 25)]public float lacunarity;
    public Vector2 offset;
    public NoiseGenerator.NormalizeMode normalizeMode;
}
