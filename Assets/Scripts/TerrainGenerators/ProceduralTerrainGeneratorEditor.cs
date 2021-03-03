#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ProceduralTerrainGenerator))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ProceduralTerrainGenerator procGen = (ProceduralTerrainGenerator) target;

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