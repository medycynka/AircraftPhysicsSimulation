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
        public WindRegion windRegion;

        public float GetAirDensity()
        {
            return airRegion ? airRegion.CalculateAirDensity() : 1.207f;
        }

        public Vector3 GetWind()
        {
            return windRegion ? windRegion.GetWindVector() : Vector3.zero;
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
        
        public void DrawRegionWindDirection(Vector3 center)
        {
            if (windRegion)
            {
                switch (windRegion.regionArrowsDrawShape)
                {
                    case WindArrowDrawShape.Single:
                        DrawArrow(center, center + windRegion.windDirection, windRegion.windPower, 
                            windRegion.regionArrowColor);
                        break;
                    case WindArrowDrawShape.Cube:
                        DrawArrow(center, center + windRegion.windDirection, windRegion.windPower, 
                            windRegion.regionArrowColor);
                        foreach (Vector3 cubeVertex in GetCubeVertices(windRegion.windPower * 4))
                        {
                            DrawArrow(center + cubeVertex, center + cubeVertex + windRegion.windDirection, 
                                windRegion.windPower, windRegion.regionArrowColor);
                        }
                        break;
                    case WindArrowDrawShape.Sphere:
                        DrawArrow(center, center + windRegion.windDirection, windRegion.windPower, 
                            windRegion.regionArrowColor);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void DrawArrow(Vector3 a, Vector3 b, float arrowSize, Color color)
        {
            Vector3 vecDir = (b - a).normalized;
            Vector3 vecDirPerpendicular = new Vector3(vecDir.z, 0f, -vecDir.x);
            Vector3 arrowTipStart = b - vecDir * arrowSize;
            Vector3 arrowL = (arrowTipStart - a).normalized;
            Vector3 legStart = vecDirPerpendicular * arrowSize * 0.25f;

            Gizmos.color = color;
            
            // Head
            Gizmos.DrawLine(arrowTipStart + legStart, arrowTipStart + vecDirPerpendicular * arrowSize * 0.75f);
            Gizmos.DrawLine(arrowTipStart + vecDirPerpendicular * arrowSize * 0.75f, b);
            Gizmos.DrawLine(b, arrowTipStart - vecDirPerpendicular * arrowSize * 0.75f);
            Gizmos.DrawLine(arrowTipStart - vecDirPerpendicular * arrowSize * 0.75f, arrowTipStart - legStart);

            // Leg
            Gizmos.DrawLine(arrowTipStart + legStart, arrowTipStart + legStart - vecDir * arrowSize);
            Gizmos.DrawLine(arrowTipStart + legStart - vecDir * arrowSize, arrowTipStart - legStart - vecDir * arrowSize);
            Gizmos.DrawLine(arrowTipStart - legStart - vecDir * arrowSize, arrowTipStart - legStart);
        }

        private Vector3[] GetCubeVertices(float arrowSize)
        {
            return new[]
            {
                new Vector3(-arrowSize * 0.5f, -arrowSize * 0.5f, -arrowSize * 0.5f),
                new Vector3(arrowSize * 0.5f, -arrowSize * 0.5f, -arrowSize * 0.5f),
                new Vector3(arrowSize * 0.5f, arrowSize * 0.5f, -arrowSize * 0.5f),
                new Vector3(-arrowSize * 0.5f, arrowSize * 0.5f, -arrowSize * 0.5f),
                new Vector3(-arrowSize * 0.5f, arrowSize * 0.5f, arrowSize * 0.5f),
                new Vector3(-arrowSize * 0.5f, -arrowSize * 0.5f, arrowSize * 0.5f),
                new Vector3(arrowSize * 0.5f, -arrowSize * 0.5f, arrowSize * 0.5f),
                new Vector3(arrowSize * 0.5f, arrowSize * 0.5f, arrowSize * 0.5f)
            };
        }
    }
}