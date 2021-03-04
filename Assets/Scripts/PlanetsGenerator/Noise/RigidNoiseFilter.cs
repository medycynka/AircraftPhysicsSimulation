using UnityEngine;


public class RigidNoiseFilter : INoiseFilter
{
    private PlanetNoiseSettings.RigidNoiseSettings _settings;
    private Noise _noise = new Noise();

    public RigidNoiseFilter(PlanetNoiseSettings.RigidNoiseSettings settings)
    {
        _settings = settings;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = _settings.baseRoughness;
        float amplitude = 1;
        float weight = 1;

        for (int i = 0; i < _settings.numLayers; i++)
        {
            float v = 1-Mathf.Abs(_noise.Evaluate(point * frequency + _settings.centre));
            v *= v;
            v *= weight;
            weight = Mathf.Clamp01(v * _settings.weightMultiplier);

            noiseValue += v * amplitude;
            frequency *= _settings.roughness;
            amplitude *= _settings.persistence;
        }

        noiseValue -= _settings.minValue; 
        
        return noiseValue * _settings.strength;
    }
}
