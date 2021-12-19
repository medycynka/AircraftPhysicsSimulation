using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public enum WindArrowDrawShape
    {
        Single,
        Cube,
        Sphere
    }
    
    [CreateAssetMenu(fileName = "WindRegion", menuName = "Aerodynamics/Environment/Wind Region", order = 2)]
    public class WindRegion : ScriptableObject
    {
        [Header("Air Region", order = 0)] 
        [Header("Region air properties", order = 1)]
        [Tooltip("Direction of the wind in current region.")] public Vector3 windDirection;
        [Tooltip("Power of the wind in current region.")] public float windPower;

        [Header("Draw properties", order = 1)]
        public Color regionArrowColor = new Color(1f, 1f, 1f, 1f);
        public WindArrowDrawShape regionArrowsDrawShape = WindArrowDrawShape.Single;

        private void OnValidate()
        {
            if (windPower < 0)
            {
                windPower = 0;
            }
            if (windDirection.x > 1)
            {
                windDirection.x = 1;
            }
            if (windDirection.x < -1)
            {
                windDirection.x = -1;
            }
            if (windDirection.y > 1)
            {
                windDirection.y = 1;
            }
            if (windDirection.y < -1)
            {
                windDirection.y = -1;
            }
            if (windDirection.z > 1)
            {
                windDirection.z = 1;
            }
            if (windDirection.z < -1)
            {
                windDirection.z = -1;
            }

            if (windDirection.sqrMagnitude > 1)
            {
                windDirection.Normalize();
            }
        }

        public Vector3 GetWindVector()
        {
            return windDirection * windPower;
        }
    }
}