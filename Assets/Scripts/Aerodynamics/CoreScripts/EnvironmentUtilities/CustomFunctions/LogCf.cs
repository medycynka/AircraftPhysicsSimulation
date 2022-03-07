using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Log", menuName = "Aerodynamics/Environment/Log")]
    public class LogCf : CustomFunctionBase
    {
        public SingleArg arg = SingleArg.X;
        public LogBase logBase = LogBase.E;
        public float customBase = 4.0f;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            if (outerArgument != null)
            {
                return logBase switch
                {
                    LogBase.E => Mathf.Log(outerArgument.GetValue(x, y, z)),
                    LogBase._2_ => Mathf.Log(outerArgument.GetValue(x, y, z), 2f),
                    LogBase._10_ => Mathf.Log10(outerArgument.GetValue(x, y, z)),
                    LogBase.Custom => Mathf.Log(outerArgument.GetValue(x, y, z), customBase),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            switch (logBase)
            {
                case LogBase.E:
                    return arg switch
                    {
                        SingleArg.X => Mathf.Log(x),
                        SingleArg.Y => Mathf.Log(y),
                        SingleArg.Z => Mathf.Log(z),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                case LogBase._2_:
                    return arg switch
                    {
                        SingleArg.X => Mathf.Log(x, 2f),
                        SingleArg.Y => Mathf.Log(y, 2f),
                        SingleArg.Z => Mathf.Log(z, 2f),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                case LogBase._10_:
                    return arg switch
                    {
                        SingleArg.X => Mathf.Log10(x),
                        SingleArg.Y => Mathf.Log10(y),
                        SingleArg.Z => Mathf.Log10(z),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                case LogBase.Custom:
                    return arg switch
                    {
                        SingleArg.X => Mathf.Log(x, customBase),
                        SingleArg.Y => Mathf.Log(y, customBase),
                        SingleArg.Z => Mathf.Log(z, customBase),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}