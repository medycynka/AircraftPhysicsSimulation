using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "WindRegion", menuName = "Aerodynamics/Environment/Wind Region", order = 0)]
    public class WindRegion : ScriptableObject
    {
        [Header("Air Region", order = 0)] 
        [Header("Region air properties", order = 1)]
        public string regionName;
        [TextArea] public string description;
        [Tooltip("Size of the region in 3D space.")] public Vector3 size;
        [Tooltip("Direction of the wind in current region.")] public Vector3 windDirection;
        [Tooltip("Power of the wind in current region.")] public float windPower;

        [Header("Draw properties", order = 1)]
        public Color regionArrowColor = new Color(1f, 1f, 1f, 1f);
        [Range(1, 10)] public int regionArrowsCount = 1;

        private void OnValidate()
        {
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
        }

        public Vector3 GetWindVector()
        {
            return windDirection * windPower;
        }
    }
}