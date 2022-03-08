using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Abs", menuName = "Aerodynamics/Environment/Abs")]
    public class AbsCf : CustomFunctionBase
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
                return Mathf.Abs(outerArgument.GetValue(x, y, z));
            }

            switch (arg)
            {
                case (SingleArg.X):
                    return Mathf.Abs(x);
                case (SingleArg.Y):
                    return Mathf.Abs(y);
                case (SingleArg.Z):
                    return Mathf.Abs(z);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}