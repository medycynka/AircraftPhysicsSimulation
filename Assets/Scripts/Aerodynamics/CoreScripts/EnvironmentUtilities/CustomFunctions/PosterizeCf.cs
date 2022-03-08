using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Posterize", menuName = "Aerodynamics/Environment/Posterize")]
    public class PosterizeCf : CustomFunctionBase
    {
        public DoubleArgs args;
        public CustomFunctionBase outerArgument2;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            return args switch
            {
                DoubleArgs.XX => x != 0 ? Mathf.Floor(x / (1f / x)) * (1f / x) : 0,
                DoubleArgs.XY => y != 0 ? Mathf.Floor(x / (1f / y)) * (1f / y) : 0,
                DoubleArgs.XZ => z != 0 ? Mathf.Floor(x / (1f / z)) * (1f / z) : 0,
                DoubleArgs.XC when outerArgument != null => outerArgument.GetValue(x, y, z) != 0
                    ? Mathf.Floor(x / (1f / outerArgument.GetValue(x, y, z))) * (1f / outerArgument.GetValue(x, y, z))
                    : 0,
                DoubleArgs.YX => x != 0 ? Mathf.Floor(y / (1f / x)) * (1f / x) : 0,
                DoubleArgs.YY => y != 0 ? Mathf.Floor(y / (1f / y)) * (1f / y) : 0,
                DoubleArgs.YZ => z != 0 ? Mathf.Floor(y / (1f / z)) * (1f / z) : 0,
                DoubleArgs.YC when outerArgument != null => outerArgument.GetValue(x, y, z) != 0
                    ? Mathf.Floor(y / (1f / outerArgument.GetValue(x, y, z))) * (1f / outerArgument.GetValue(x, y, z))
                    : 0,
                DoubleArgs.ZX => x != 0 ? Mathf.Floor(z / (1f / x)) * (1f / x) : 0,
                DoubleArgs.ZY => y != 0 ? Mathf.Floor(z / (1f / y)) * (1f / y) : 0,
                DoubleArgs.ZZ => z != 0 ? Mathf.Floor(z / (1f / z)) * (1f / z) : 0,
                DoubleArgs.ZC when outerArgument != null => outerArgument.GetValue(x, y, z) != 0
                    ? Mathf.Floor(z / (1f / outerArgument.GetValue(x, y, z))) * (1f / outerArgument.GetValue(x, y, z))
                    : 0,
                DoubleArgs.CX when outerArgument != null => x != 0
                    ? Mathf.Floor(outerArgument.GetValue(x, y, z) / (1f / x)) * (1f / x)
                    : 0,
                DoubleArgs.CY when outerArgument != null => y != 0
                    ? Mathf.Floor(outerArgument.GetValue(x, y, z) / (1f / y)) * (1f / y)
                    : 0,
                DoubleArgs.CZ when outerArgument != null => z != 0
                    ? Mathf.Floor(outerArgument.GetValue(x, y, z) / (1f / z)) * (1f / z)
                    : 0,
                DoubleArgs.CC when outerArgument != null && outerArgument2 != null =>
                    outerArgument2.GetValue(x, y, z) != 0
                        ? Mathf.Floor(outerArgument.GetValue(x, y, z) / (1f / outerArgument2.GetValue(x, y, z))) *
                          (1f / outerArgument2.GetValue(x, y, z))
                        : 0,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}