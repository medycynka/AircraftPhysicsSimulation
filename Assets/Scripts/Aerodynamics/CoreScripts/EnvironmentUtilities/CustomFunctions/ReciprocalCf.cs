using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Reciprocal", menuName = "Aerodynamics/Environment/Reciprocal")]
    public class ReciprocalCf : CustomFunctionBase
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
                SingleArg.X => x != 0 ? 1f / x : 0f,
                SingleArg.Y => y != 0 ? 1f / y : 0f,
                SingleArg.Z => z != 0 ? 1f / z : 0f,
                SingleArg.C when outerArgument != null => outerArgument.GetValue(x, y, z) != 0
                    ? 1f / outerArgument.GetValue(x, y, z)
                    : 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}