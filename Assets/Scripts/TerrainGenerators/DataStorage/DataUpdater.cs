using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DataUpdater : ScriptableObject
{
    public event Action ONValuesUpdated;
    public bool autoUpdate;

    private void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdateValues;
        }
    }

    public void NotifyOfUpdateValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdateValues;
        if (ONValuesUpdated != null)
        {
            ONValuesUpdated();
        }
    }
}
