using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "LengthSqrt", menuName = "Aerodynamics/Environment/Length Sqrt")]
    public class LengthSqrtCf : CustomFunctionBase
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
                    return new Vector2(x, y).sqrMagnitude;
                case VectorArgs.XZ:
                    return new Vector2(x, z).sqrMagnitude;
                case VectorArgs.XC when outerArgument != null:
                    return new Vector2(x, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.YZ:
                    return new Vector2(y, z).sqrMagnitude;
                case VectorArgs.YC when outerArgument != null:
                    return new Vector2(y, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.ZC when outerArgument != null:
                    return new Vector2(z, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XYZ:
                    return new Vector3(x, y, z).sqrMagnitude;
                case VectorArgs.XYC when outerArgument != null:
                    return new Vector3(x, y, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XZC when outerArgument != null:
                    return new Vector3(x, z, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XYZC when outerArgument != null:
                    return new Vector4(x, y, z, outerArgument.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.CC when outerArgument != null && outerArgument2 != null:
                    return new Vector2(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(x, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.YCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.ZCC when outerArgument != null && outerArgument2 != null:
                    return new Vector3(z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.CCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector3(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XYCC when outerArgument != null && outerArgument2 != null:
                    return new Vector4(x, y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XZCC when outerArgument != null && outerArgument2 != null:
                    return new Vector4(x, z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.XCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(x, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.YCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(y, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.ZCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null:
                    return new Vector4(z, outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z)).sqrMagnitude;
                case VectorArgs.CCCC when outerArgument != null && outerArgument2 != null && outerArgument3 != null && outerArgument4 != null:
                    return new Vector4(outerArgument.GetValue(x, y, z), outerArgument2.GetValue(x, y, z), outerArgument3.GetValue(x, y, z), outerArgument4.GetValue(x, y, z)).sqrMagnitude;
                default:
                    return 0f;
            }
        }
    }
}