using System;
using UnityEngine;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class ConditionalHideAttribute : PropertyAttribute
{
    public string conditionalSourceField;
    public int enumIndex;

    public ConditionalHideAttribute(string boolVariableName)
    {
        conditionalSourceField = boolVariableName;
    }

    public ConditionalHideAttribute(string enumVariableName, int enumIndex)
    {
        conditionalSourceField = enumVariableName;
        this.enumIndex = enumIndex;
    }
}
