using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "ReciprocalSqrt", menuName = "Aerodynamics/Environment/Reciprocal")]
    public class ReciprocalSqrtCf : CustomFunctionBase
    {
        public SingleArg arg;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            return arg switch
            {
                SingleArg.X => x != 0 ? 1f / (x * x) : 0f,
                SingleArg.Y => y != 0 ? 1f / (y * y) : 0f,
                SingleArg.Z => z != 0 ? 1f / (z * z) : 0f,
                SingleArg.C when outerArgument != null => outerArgument.GetValue(x, y, z) != 0
                    ? 1f / (outerArgument.GetValue(x, y, z) * outerArgument.GetValue(x, y, z))
                    : 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}