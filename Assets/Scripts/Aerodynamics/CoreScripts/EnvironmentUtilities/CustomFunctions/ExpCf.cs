using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Exp", menuName = "Aerodynamics/Environment/Exp")]
    public class ExpCf : CustomFunctionBase
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
                return Mathf.Exp(outerArgument.GetValue(x, y, z));
            }
                        
            switch (arg)
            {
                case (SingleArg.X):
                    return Mathf.Exp(x);
                case (SingleArg.Y):
                    return Mathf.Exp(y);
                case (SingleArg.Z):
                    return Mathf.Exp(z);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}