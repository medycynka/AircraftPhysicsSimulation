using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AerodynamicSurfaceManager)), CanEditMultipleObjects()]
public class AeroSurfaceEditor : Editor
{
    SerializedProperty config;
    SerializedProperty isControlSurface;
    SerializedProperty inputType;
    SerializedProperty inputMultiplyer;
    AerodynamicSurfaceManager _surfaceManager;

    private void OnEnable()
    {
        config = serializedObject.FindProperty("config");
        isControlSurface = serializedObject.FindProperty("isControlSurface");
        inputType = serializedObject.FindProperty("inputType");
        inputMultiplyer = serializedObject.FindProperty("inputMultiplier");
        _surfaceManager = target as AerodynamicSurfaceManager;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(config);
        EditorGUILayout.PropertyField(isControlSurface);
        if (_surfaceManager.isControlSurface)
        {
            EditorGUILayout.PropertyField(inputType);
            EditorGUILayout.PropertyField(inputMultiplyer);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
