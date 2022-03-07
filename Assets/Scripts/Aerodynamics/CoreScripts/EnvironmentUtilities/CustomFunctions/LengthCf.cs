using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "Length", menuName = "Aerodynamics/Environment/Length")]
    public class LengthCf : CustomFunctionBase
    {
        public CustomFunctionBase outerArgument2;
        public CustomFunctionBase outerArgument3;
        public CustomFunctionBase outerArgument4;
        public VectorArgs args = VectorArgs.XY;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }
                
        public override float GetValue(int x, int y, int z)
        {
            switch (args)
            {
                case VectorArgs.XY:
                    return new Vector2(x, y).magnitude;
                case VectorArgs.XZ:
                    return new Vector2(x, z).magnitude;
                case VectorArgs.XC when outerArgument != null:
                    return new Vector2(x, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.YZ:
                    return new Vector2(y, z).magnitude;
                case VectorArgs.YC when outerArgument != null:
                    return new Vector2(y, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.ZC when outerArgument != null:
                    return new Vector2(z, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.XYZ:
                    return new Vector3(x, y, z).magnitude;
                case VectorArgs.XYC when outerArgument != null:
                    return new Vector3(x, y, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.XZC when outerArgument != null:
                    return new Vector3(x, z, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.XYZC when outerArgument != null:
                    return new Vector4(x, y, z, outerArgument.GetValue(x, y, z)).magnitude;
                case VectorArgs.CC when outerArgument != null && outerArgument2 != null:
                    return new Vector2(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.XCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(x, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.YCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.ZCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.CCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector3(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).magnitude;
                case VectorArgs.XYCC when outerArgument != null && outerArgument2 != null:
                    return new Vector4(x, y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.XZCC when outerArgument != null && outerArgument2 != null:
                    return new Vector4(x, z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).magnitude;
                case VectorArgs.XCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(x, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).magnitude;
                case VectorArgs.YCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).magnitude;
                case VectorArgs.ZCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).magnitude;
                case VectorArgs.CCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null && outerArgument4 != null:
                    return new Vector4(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z), outerArgument4.GetValue(x, y, z)).magnitude;
                default:
                    return 0f;
            }
        }
    }
}