using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(DataUpdater), true)]
public class DataUpdaterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DataUpdater data = (DataUpdater)target;

        if (GUILayout.Button("Update"))
        {
            data.NotifyOfUpdateValues();
        }
    }
}
