using UnityEngine;
using UnityEngineInternal;

public class ColorGenerator
{
    private ColorSettings _settings;
    private Texture2D _texture;
    private const int TextureResolution = 50;
    private INoiseFilter _biomeNoiseFilter;

    public void UpdateSettings(ColorSettings settings)
    {
        _settings = settings;
        
        if (_texture == null || _texture.height != settings.biomeColorSettings.biomes.Length)
        {
            _texture = new Texture2D(TextureResolution*2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32, false);
        }
        
        _biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColorSettings.noise);
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        _settings.planetMaterial.SetVector("_ElevationMinMax", new Vector4(elevationMinMax.minVal, elevationMinMax.maxVal));
    }

    public float BiomePercentFromPoint(Vector3 pointOnUnitSphere)
    {
        float heightPercent = (pointOnUnitSphere.y + 1) / 2f;
        heightPercent += (_biomeNoiseFilter.Evaluate(pointOnUnitSphere) - _settings.biomeColorSettings.noiseOffset) *
                         _settings.biomeColorSettings.noiseStrength;
        float biomeIndex = 0;
        int numBiomes = _settings.biomeColorSettings.biomes.Length;
        float blendRange = _settings.biomeColorSettings.blendAmount / 2f + .001f;

        for (int i = 0; i < numBiomes; i++)
        {
            float dst = heightPercent - _settings.biomeColorSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, dst);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }

        return biomeIndex / Mathf.Max(1, numBiomes - 1);
    }

    public void UpdateColours()
    {
        Color[] colours = new Color[_texture.width * _texture.height];
        int colourIndex = 0;
        
        foreach (var biome in _settings.biomeColorSettings.biomes)
        {
            for (int i = 0; i < TextureResolution * 2; i++)
            {
                Color gradientCol;
                
                if (i < TextureResolution) 
                {
                    gradientCol = _settings.oceanColor.Evaluate(i / (TextureResolution - 1f));
                }
                else 
                {
                    gradientCol = biome.gradient.Evaluate((i-TextureResolution) / (TextureResolution - 1f));
                }
                
                Color tintCol = biome.tint;
                colours[colourIndex] = gradientCol * (1 - biome.tintPercent) + tintCol * biome.tintPercent;
                colourIndex++;
            }
        }
        
        _texture.SetPixels(colours);
        _texture.Apply();
        _settings.planetMaterial.SetTexture("_PlanetTexture", _texture);
    }
}
