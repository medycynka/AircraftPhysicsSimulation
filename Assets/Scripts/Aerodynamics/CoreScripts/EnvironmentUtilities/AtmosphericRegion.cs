using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "AtmosphericRegion", menuName = "Aerodynamics/Environment/Atmospheric Region", order = 0)]
    public class AtmosphericRegion : ScriptableObject
    {
        public string regionName;
        [TextArea] public string description;
        [Tooltip("Size of the region in 3D space.")] public Vector3 size;
        public AirRegion airRegion;
        public WindRegionVectorField windRegionVectorField;

        public float GetAirDensity()
        {
            return airRegion ? airRegion.CalculateAirDensity() : 1.207f;
        }

        public Vector3 GetWind(Vector3 position)
        {
            return windRegionVectorField ? windRegionVectorField.GetWindVector(position) : Vector3.zero;
        }
        
        public void DrawRegionBoundaries(Vector3 center)
        {
            if (airRegion)
            {
                Gizmos.color = airRegion.regionSidesColor;
                Gizmos.DrawCube(center, size);
                Gizmos.color = airRegion.regionBorderColor;
                Gizmos.DrawWireCube(center, size);
            }
        }
        
        public void DrawRegionWindDirection()
        {
            if (windRegionVectorField)
            {
                windRegionVectorField.DrawInGizmos();
            }
        }
    }
}