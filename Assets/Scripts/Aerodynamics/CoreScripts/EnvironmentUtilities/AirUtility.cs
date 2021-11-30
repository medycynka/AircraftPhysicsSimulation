using UnityEngine;
using System;


public static class AirUtility
{
    public static readonly float gravitationForce = 9.80665f;
    public static readonly float airMolarMass = 0.0289644f;
    public static readonly float universalGasConstant = 8.31432f;
    public static readonly float dryAirGasConstant = 287.058f;
    public static readonly float waterGasConstant = 461.495f;
    public static readonly float kelvinsAtZeroCelsius = 273.15f;

    public static float CelsiusesToKelvins(float celsiusDegrees)
    {
        return kelvinsAtZeroCelsius + celsiusDegrees;
    }

    public static float KelvinsToCelsiuses(float kelvinDegrees)
    {
        return kelvinDegrees - kelvinsAtZeroCelsius;
    }

    public static float CalculateAirPressureAtAltitude(float altitude, float celsiusDegrees, float pressureAtSeaLevel, float seaLevel = 0f)
    {
        return pressureAtSeaLevel * Mathf.Exp((-gravitationForce * airMolarMass * (altitude - seaLevel)) /
                                              (universalGasConstant * CelsiusesToKelvins(celsiusDegrees)));
    }

    public static float CalculateSaturationVapor(float celsiusDegrees)
    {
        return 6.1078f * Mathf.Pow(10, (7.5f * celsiusDegrees) / CelsiusesToKelvins(celsiusDegrees));
    }
    
    public static float CalculateSaturationVaporW(float celsiusDegrees)
    {
        float saturation = 0.99999683f + celsiusDegrees * (-0.90826951E-02f + celsiusDegrees * (0.78736169E-04f +
            celsiusDegrees * (-0.61117958E-06f + celsiusDegrees * (0.43884187E-08f + celsiusDegrees *
                (-0.29883885E-10f + celsiusDegrees * (0.21874425E-12f + celsiusDegrees * (-0.17892321E-14f +
                    celsiusDegrees * (0.11112018E-16f + celsiusDegrees * (-0.30994571E-19f)))))))));
        
        return 6.1078f / Mathf.Pow(saturation, 8);
    }

    public static float CalculateVaporPressure(float celsiusDegrees, float relativeHumility)
    {
        return relativeHumility * CalculateSaturationVapor(celsiusDegrees);
    }

    public static float CalculateAirDensity(float celsiusDegrees, float relativeHumility, float altitude,
        float pressureAtSeaLevel, float seaLevel = 0f)
    {
        float airPressure = CalculateAirPressureAtAltitude(altitude, celsiusDegrees, pressureAtSeaLevel, seaLevel);
        float vaporPressure = CalculateVaporPressure(celsiusDegrees, relativeHumility);
        float deltaPressure = airPressure - vaporPressure;

        return (deltaPressure / (dryAirGasConstant * celsiusDegrees)) +
               (vaporPressure / (waterGasConstant * celsiusDegrees));
    }
}