using System;
using UnityEngine;
using UnityEngine.Events;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public class CustomFunctionBase : ScriptableObject
    {
        public UnityEvent validationCallback;
        
        private void OnValidate()
        {
            validationCallback?.Invoke();
        }

        public virtual float GetValue(int x, int y, int z)
        {
            return 0f;
        }
    }
}