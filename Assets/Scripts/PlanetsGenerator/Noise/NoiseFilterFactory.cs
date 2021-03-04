


public static class NoiseFilterFactory {
    public static INoiseFilter CreateNoiseFilter(PlanetNoiseSettings settings)
    {
        return settings.filterType switch
        {
            PlanetNoiseSettings.FilterType.Simple => new SimpleNoiseFilter(settings.simpleNoiseSettings),
            PlanetNoiseSettings.FilterType.Rigid => new RigidNoiseFilter(settings.rigidNoiseSettings),
            _ => null
        };
    }
}
