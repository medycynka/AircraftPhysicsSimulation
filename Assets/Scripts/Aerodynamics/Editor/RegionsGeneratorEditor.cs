using UnityEditor;
using Aerodynamics.Utils;
using UnityEngine;


namespace Aerodynamics.Editor
{
    [CustomEditor(typeof(RegionsGenerator))]
    public class RegionsGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            RegionsGenerator generator = target as RegionsGenerator;

            if (GUILayout.Button("Generate regions"))
            {
                if (generator != null)
                {
                    generator.GenerateRegions();
                }
            }
            
            if (GUILayout.Button("Clear regions"))
            {
                if (generator != null)
                {
                    generator.ClearRegions();
                }
            }
        }
    }
}