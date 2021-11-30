#if UNITY_EDITOR
using UnityEditor;
using Aerodynamics.CoreScripts;


namespace Aerodynamics.Editor
{
    [CustomEditor(typeof(AerodynamicSurfaceManager)), CanEditMultipleObjects()]
    public class AeroSurfaceEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _isControlSurface;
        private SerializedProperty _inputType;
        private SerializedProperty _inputMultiplier;
        private AerodynamicSurfaceManager _surfaceManager;

        private void OnEnable()
        {
            _config = serializedObject.FindProperty("config");
            _isControlSurface = serializedObject.FindProperty("isControlSurface");
            _inputType = serializedObject.FindProperty("inputType");
            _inputMultiplier = serializedObject.FindProperty("inputMultiplier");
            _surfaceManager = target as AerodynamicSurfaceManager;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_config);
            EditorGUILayout.PropertyField(_isControlSurface);

            if (_surfaceManager.isControlSurface)
            {
                EditorGUILayout.PropertyField(_inputType);
                EditorGUILayout.PropertyField(_inputMultiplier);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif