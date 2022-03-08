using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Modulo", menuName = "Aerodynamics/Environment/Modulo")]
    public class ModuloCf : CustomFunctionBase
    {
        public DoubleArgs args = DoubleArgs.XY;
        public CustomFunctionBase outerArgument2;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            return args switch
            {
                DoubleArgs.XX => x % x,
                DoubleArgs.XY => x % y,
                DoubleArgs.XZ => x % z,
                DoubleArgs.XC when outerArgument != null => x % (int)outerArgument.GetValue(x, y, z),
                DoubleArgs.YX => y % x,
                DoubleArgs.YY => y % y,
                DoubleArgs.YZ => y % z,
                DoubleArgs.YC when outerArgument != null => y % (int)outerArgument.GetValue(x, y, z),
                DoubleArgs.ZX => z % x,
                DoubleArgs.ZY => z % y,
                DoubleArgs.ZZ => z % z,
                DoubleArgs.ZC when outerArgument != null => z % (int)outerArgument.GetValue(x, y, z),
                DoubleArgs.CX when outerArgument != null => (int)outerArgument.GetValue(x, y, z) % x,
                DoubleArgs.CY when outerArgument != null => (int)outerArgument.GetValue(x, y, z) % y,
                DoubleArgs.CZ when outerArgument != null => (int)outerArgument.GetValue(x, y, z) % z,
                DoubleArgs.CC when outerArgument != null && outerArgument2 != null => (int)outerArgument.GetValue(x, y, z) % (int)outerArgument2.GetValue(x, y, z),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}