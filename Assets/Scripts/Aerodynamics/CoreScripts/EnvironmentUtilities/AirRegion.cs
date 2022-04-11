using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "AirRegion", menuName = "Aerodynamics/Environment/Air Region", order = 1)]
    public class AirRegion : ScriptableObject
    {
        [Header("Air Region", order = 0)] 
        [Header("Region air properties", order = 1)]
        [Tooltip("Region temperature in Celsius degrees.")] [Range(-50f, 80f)] public float temperature;
        [Tooltip("Region humility. 0 means dry air and 1 is air with 100% humility.")] [Range(0f, 1f)] public float humility;
        [Tooltip("Region altitude in meters from region's sea level.")] public float altitude;
        [Tooltip("Region air pressure in Pascals.")] public float pressureAtSeaLevel = 100000;
        [Tooltip("Region sea level.")] public float seaLevel;

        [Header("Draw properties", order = 1)]
        public Color regionBorderColor = new Color(1f, 1f, 1f, 1f);
        public Color regionSidesColor = new Color(0f, 1f, 1f, 0.5f);

        [Header("Calculated air density", order = 1)] 
        [SerializeField] private float _calculatedDensity = Single.NaN;
        private bool _isDensitySet;

        private void OnValidate()
        {
            _calculatedDensity = AirUtility.CalculateAirDensity(temperature, humility, altitude, pressureAtSeaLevel, seaLevel);
            _isDensitySet = true;
        }

        public float CalculateAirDensity()
        {
            if (!_isDensitySet)
            {
                _calculatedDensity = AirUtility.CalculateAirDensity(temperature, humility, altitude, pressureAtSeaLevel, seaLevel);
                _isDensitySet = true;
            }

            return _calculatedDensity;
        }
    }
}