using System;
using UnityEngine;


[CreateAssetMenu(fileName = "ColorSettings", menuName = "Procedural Planets/Color Settings")]
public class ColorSettings : ScriptableObject
{
    public Material planetMaterial;
    public BiomeColorSettings biomeColorSettings;
    public Gradient oceanColor;
    
    [Serializable]
    public class BiomeColorSettings
    {
        public Biome[] biomes;
        public PlanetNoiseSettings noise;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0, 1)] public float blendAmount;
        
        [Serializable]
        public class Biome
        {
            public Gradient gradient;
            public Color tint;
            [Range(0, 1)] public float startHeight;
            [Range(0, 1)] public float tintPercent;
        }
    }
}
