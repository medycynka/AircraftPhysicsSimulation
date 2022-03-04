using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "SingleNumber", menuName = "Aerodynamics/Environment/Single Number")]
    public class SingleNumberSO : CustomFunctionBase
    {
        [Range(-1f, 1f), Tooltip("Select return value in [-1, 1] ([-1, 1], because vector will be normalized anyway)")] 
        public float returnValue;

        public override float GetValue(int x, int y, int z)
        {
            return returnValue;
        }
    }
}