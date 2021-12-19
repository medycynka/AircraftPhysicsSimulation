﻿#if UNITY_EDITOR
using Aerodynamics.CoreScripts.EnvironmentUtilities;
using UnityEngine;
using UnityEditor;


namespace Aerodynamics.Editor
{
    [CustomPropertyDrawer(typeof(AirRegion), true)]
    [CustomPropertyDrawer(typeof(WindRegion), true)]
    [CustomPropertyDrawer(typeof(AtmosphericRegion), true)]
    public class ScriptableObjectsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                ScriptableObject data = property.objectReferenceValue as ScriptableObject;

                if (!data)
                {
                    return EditorGUIUtility.singleLineHeight;
                }

                SerializedObject serializedObject = new SerializedObject(data);
                SerializedProperty prop = serializedObject.GetIterator();

                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script")
                        {
                            continue;
                        }

                        SerializedProperty subProp = serializedObject.FindProperty(prop.name);
                        float height = EditorGUI.GetPropertyHeight(subProp, null, true) +
                                       EditorGUIUtility.standardVerticalSpacing;
                        totalHeight += height;
                    } while (prop.NextVisible(false));
                }

                totalHeight += EditorGUIUtility.standardVerticalSpacing * 2;
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.objectReferenceValue != null)
            {
                property.isExpanded =
                    EditorGUI.Foldout(
                        new Rect(position.x, position.y, EditorGUIUtility.labelWidth,
                            EditorGUIUtility.singleLineHeight),
                        property.isExpanded, property.displayName, true);
                EditorGUI.PropertyField(
                    new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y,
                        position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), property,
                    GUIContent.none, true);

                if (GUI.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }

                if (property.objectReferenceValue == null)
                {
                    EditorGUIUtility.ExitGUI();
                }

                if (property.isExpanded)
                {
                    GUI.Box(
                        new Rect(0,
                            position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing -
                            1,
                            Screen.width,
                            position.height - EditorGUIUtility.singleLineHeight -
                            EditorGUIUtility.standardVerticalSpacing),
                        "");
                    EditorGUI.indentLevel++;
                    ScriptableObject data = (ScriptableObject) property.objectReferenceValue;
                    SerializedObject serializedObject = new SerializedObject(data);
                    SerializedProperty prop = serializedObject.GetIterator();
                    float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            if (prop.name == "m_Script")
                            {
                                continue;
                            }

                            float height = EditorGUI.GetPropertyHeight(prop, new GUIContent(prop.displayName), true);
                            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, true);
                            y += height + EditorGUIUtility.standardVerticalSpacing;
                        } while (prop.NextVisible(false));
                    }

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUI.ObjectField(
                    new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property,
                    label);
            }

            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }
    }
}
#endif
