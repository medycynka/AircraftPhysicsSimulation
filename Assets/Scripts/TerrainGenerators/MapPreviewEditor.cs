#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapPreview procGen = (MapPreview) target;

        if (DrawDefaultInspector())
        {
            procGen.DrawMapInEditor();
        }

        if (GUILayout.Button("Generate map"))
        {
            procGen.DrawMapInEditor();
        }
    }
}
#endif