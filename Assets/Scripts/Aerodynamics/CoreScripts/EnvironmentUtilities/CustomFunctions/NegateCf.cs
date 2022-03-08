using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Negate", menuName = "Aerodynamics/Environment/Negate")]
    public class NegateCf : CustomFunctionBase
    {
        public SingleArg arg = SingleArg.X;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            if (outerArgument != null && arg == SingleArg.C)
            {
                return -outerArgument.GetValue(x, y, z);
            }
                        
            return arg switch
            {
                SingleArg.X => -x,
                SingleArg.Y => -y,
                SingleArg.Z => -z,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}