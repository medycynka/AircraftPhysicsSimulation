using System;
using UnityEngine;


[CreateAssetMenu(fileName = "ShapeSettings", menuName = "Procedural Planets/Shape Settings")]
public class ShapeSettings : ScriptableObject
{
    public float planetRadius = 1;
    public NoiseLayer[] noiseLayers;

    [Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;
        public bool useFirstLayerAsMask;
        public PlanetNoiseSettings noiseSettings;
    }
}
