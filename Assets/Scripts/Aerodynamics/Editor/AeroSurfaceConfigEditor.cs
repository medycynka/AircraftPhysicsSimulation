#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AerodynamicSurface)), CanEditMultipleObjects()]
public class AeroSurfaceConfigEditor : Editor
{
    private SerializedProperty _position;
    private SerializedProperty _rotation;
    private SerializedProperty _scale;
    private SerializedProperty _liftSlope;
    private SerializedProperty _skinFriction;
    private SerializedProperty _zeroLiftAoA;
    private SerializedProperty _stallAngleHigh;
    private SerializedProperty _stallAngleLow;
    private SerializedProperty _chord;
    private SerializedProperty _flapFraction;
    private SerializedProperty _span;
    private SerializedProperty _autoAspectRatio;
    private SerializedProperty _aspectRatio;
    private AerodynamicSurface _config;

    private void OnEnable()
    {
        _position = serializedObject.FindProperty("position");
        _rotation = serializedObject.FindProperty("rotation");
        _scale = serializedObject.FindProperty("scale");
        _liftSlope = serializedObject.FindProperty("liftSlope");
        _skinFriction = serializedObject.FindProperty("skinFriction");
        _zeroLiftAoA = serializedObject.FindProperty("zeroLiftAoA");
        _stallAngleHigh = serializedObject.FindProperty("stallAngleHigh");
        _stallAngleLow = serializedObject.FindProperty("stallAngleLow");
        _chord = serializedObject.FindProperty("chord");
        _flapFraction = serializedObject.FindProperty("flapFraction");
        _span = serializedObject.FindProperty("span");
        _autoAspectRatio = serializedObject.FindProperty("autoAspectRatio");
        _aspectRatio = serializedObject.FindProperty("aspectRatio");
        _config = target as AerodynamicSurface;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(_position);
        EditorGUILayout.PropertyField(_rotation);
        EditorGUILayout.PropertyField(_scale);
        EditorGUILayout.PropertyField(_liftSlope);
        EditorGUILayout.PropertyField(_skinFriction);
        EditorGUILayout.PropertyField(_zeroLiftAoA);
        EditorGUILayout.PropertyField(_stallAngleHigh);
        EditorGUILayout.PropertyField(_stallAngleLow);
        EditorGUILayout.PropertyField(_chord);
        EditorGUILayout.PropertyField(_flapFraction);
        EditorGUILayout.PropertyField(_span);
        EditorGUILayout.PropertyField(_autoAspectRatio);
        
        if (_config.autoAspectRatio)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(_aspectRatio);
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.PropertyField(_aspectRatio);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif