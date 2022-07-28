using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XCharts;
using Object = UnityEngine.Object;


namespace Aerodynamics.CoreScripts
{
    [Serializable]
    public class MeasurementFileConfig
    {
        public Object file;
        public bool shouldMeasure;
        [HideInInspector] public StreamWriter writer;
    }
    
    public class PositionTracker : MonoBehaviour
    {
        [Header("Measurement tracker", order = 0)]
        [Header("Frequency parameters", order = 1)]
        [Range(2, 300)] public int frameRateStep = 30;
        [Range(0.1f, 5.0f)] public float timeRateStep = 0.5f;
        [Range(2, 16386)] public int maxMeasurementCount = 4096;   // 16386 ~ 4.5h with measurement taken every 60 frames (~1s)
        [Header("Object properties", order = 1)]
        public Transform objectTransform;
        public PhysicsManager physicsManager;
        [Header("Charts", order = 1)]
        public LineChart tempChart;
        public LineChart positionYChart;
        public LineChart positionXChart;
        public LineChart positionZChart;
        public LineChart rotationChart;
        [Header("Files storages", order = 1)] 
        public MeasurementFileConfig tempFile;
        public MeasurementFileConfig velFile;
        public MeasurementFileConfig positionFile;
        public MeasurementFileConfig rotationFile;
        public MeasurementFileConfig forwardFile;
        

        private int counter = 0;

        private void Awake()
        {
            tempChart.series.ClearData();
            positionYChart.series.ClearData();
            positionXChart.series.ClearData();
            positionZChart.series.ClearData();
            rotationChart.series.ClearData();

            if (tempFile.shouldMeasure)
            {
                if (tempFile.file != null)
                {
                    tempFile.writer = new StreamWriter(AssetDatabase.GetAssetPath(tempFile.file), false);
                }
                else
                {
                    tempFile.shouldMeasure = false;
                }
            }
            if (velFile.shouldMeasure)
            {
                if (velFile.file != null)
                {
                    velFile.writer = new StreamWriter(AssetDatabase.GetAssetPath(velFile.file), false);
                }
                else
                {
                    velFile.shouldMeasure = false;
                }
            }
            if (positionFile.shouldMeasure)
            {
                if (positionFile.file != null)
                {
                    positionFile.writer = new StreamWriter(AssetDatabase.GetAssetPath(positionFile.file), false);
                }
                else
                {
                    positionFile.shouldMeasure = false;
                }
            }
            if (rotationFile.shouldMeasure)
            {
                if (rotationFile.file != null)
                {
                    rotationFile.writer = new StreamWriter(AssetDatabase.GetAssetPath(rotationFile.file), false);
                }
                else
                {
                    rotationFile.shouldMeasure = false;
                }
            }
            if (forwardFile.shouldMeasure)
            {
                if (forwardFile.file != null)
                {
                    forwardFile.writer = new StreamWriter(AssetDatabase.GetAssetPath(forwardFile.file), false);
                }
                else
                {
                    forwardFile.shouldMeasure = false;
                }
            }
            
            InvokeRepeating(nameof(MeasurePositionByTime), 0.0f, timeRateStep);
        }

        private void OnApplicationQuit()
        {
            if (tempFile.shouldMeasure)
            {
                tempFile.writer.Close();
            }
            if (velFile.shouldMeasure)
            {
                velFile.writer.Close();
            }
            if (positionFile.shouldMeasure)
            {
                positionFile.writer.Close();
            }
            if (rotationFile.shouldMeasure)
            {
                rotationFile.writer.Close();
            }
            if (forwardFile.shouldMeasure)
            {
                forwardFile.writer.Close();
            }
        }

        public void MeasurePosition()
        {
            if (Time.frameCount % frameRateStep == 0 && counter < maxMeasurementCount)
            {
                Vector3 currPos = objectTransform.position;
                Vector3 currRot = MapRotation(objectTransform.rotation.eulerAngles);
                float currVel = physicsManager.rb.velocity.magnitude * 2;
                float timeCounter = counter * (frameRateStep / 60.0f);
                
                tempChart.AddData(0, counter, physicsManager.currentTemperature);
                positionYChart.AddData(0, counter, currPos.y);
                positionYChart.AddData(1, counter, currVel);
                positionXChart.AddData(0, counter, currPos.x);
                positionZChart.AddData(0, counter, currPos.z);
                rotationChart.AddData(0, counter, currRot.x);
                rotationChart.AddData(1, counter, currRot.y);
                rotationChart.AddData(2, counter, currRot.z);
                
                if (tempFile.shouldMeasure)
                {
                    tempFile.writer.WriteLine($"{timeCounter:F1} {physicsManager.currentTemperature:F8}");
                }
                if (velFile.shouldMeasure)
                {
                    velFile.writer.WriteLine($"{timeCounter:F1} {currVel:F8}");
                }
                if (positionFile.shouldMeasure)
                {
                    positionFile.writer.WriteLine($"{timeCounter:F1} {currPos.x:F8} {currPos.y:F8} {currPos.z:F8}");
                }
                if (rotationFile.shouldMeasure)
                {
                    rotationFile.writer.WriteLine($"{timeCounter:F1} {currRot.x:F8} {currRot.y:F8} {currRot.z:F8}");
                }
                if (forwardFile.shouldMeasure)
                {
                    Vector3 currForward = objectTransform.forward;
                    
                    forwardFile.writer.WriteLine($"{currPos.x:F6} {currPos.y:F6} {currPos.z:F6} {currForward.x:F6} {currForward.y:F6} {currForward.z:F6}");
                }
                
                counter++;
            }
        }
        
        public void MeasurePositionByTime()
        {
            if (counter < maxMeasurementCount)
            {
                Vector3 currPos = objectTransform.position;
                Vector3 currRot = MapRotation(objectTransform.rotation.eulerAngles);
                float currVel = physicsManager.rb.velocity.magnitude * 2;
                float timeCounter = counter * timeRateStep;
                
                tempChart.AddData(0, counter, physicsManager.currentTemperature);
                positionYChart.AddData(0, counter, currPos.y);
                positionYChart.AddData(1, counter, currVel);
                positionXChart.AddData(0, counter, currPos.x);
                positionZChart.AddData(0, counter, currPos.z);
                rotationChart.AddData(0, counter, currRot.x);
                rotationChart.AddData(1, counter, currRot.y);
                rotationChart.AddData(2, counter, currRot.z);
                
                if (tempFile.shouldMeasure)
                {
                    tempFile.writer.WriteLine($"{timeCounter:F1} {physicsManager.currentTemperature:F8}");
                }
                if (velFile.shouldMeasure)
                {
                    velFile.writer.WriteLine($"{timeCounter:F1} {currVel:F8}");
                }
                if (positionFile.shouldMeasure)
                {
                    positionFile.writer.WriteLine($"{timeCounter:F1} {currPos.x:F8} {currPos.y:F8} {currPos.z:F8}");
                }
                if (rotationFile.shouldMeasure)
                {
                    rotationFile.writer.WriteLine($"{timeCounter:F1} {currRot.x:F8} {currRot.y:F8} {currRot.z:F8}");
                }
                if (forwardFile.shouldMeasure)
                {
                    Vector3 currForward = objectTransform.forward;
                    
                    forwardFile.writer.WriteLine($"{currPos.x:F6} {currPos.y:F6} {currPos.z:F6} {currForward.x:F6} {currForward.y:F6} {currForward.z:F6}");
                }
                
                counter++;
            }
        }

        private Vector3 MapRotation(Vector3 eulerAngles)
        {
            float x = eulerAngles.x;
            float y = eulerAngles.y;
            float z = eulerAngles.z;

            if (x > 180)
            {
                x -= 360;
            }
            if (y > 180)
            {
                y -= 360;
            }
            if (z > 180)
            {
                z -= 360;
            }

            return new Vector3(x, y, z);
        }
    }
}