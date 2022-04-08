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
    public class PositionTracker : MonoBehaviour
    {
        [Header("Measurement tracker", order = 0)]
        [Header("Frequency parameters", order = 1)]
        [Range(2, 300)] public int frameRateStep = 60;
        [Range(2, 16386)] public int maxMeasurementCount = 4096;   // 16386 ~ 4.5h with measurement taken every 60 (~1s)
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
        public Object tempChartFile;
        public Object positionYChartFile;
        public Object positionXChartFile;
        public Object positionZChartFile;
        public Object rotationChartFile;

        private StreamWriter _tempChartFileWriter;
        private StreamWriter _positionYChartFileWriter;
        private StreamWriter _positionXChartFileWriter;
        private StreamWriter _positionZChartFileWriter;
        private StreamWriter _rotationChartFileWriter;
        

        private int counter = 0;

        private void Awake()
        {
            tempChart.series.ClearData();
            positionYChart.series.ClearData();
            positionXChart.series.ClearData();
            positionZChart.series.ClearData();
            rotationChart.series.ClearData();

            _tempChartFileWriter = new StreamWriter(AssetDatabase.GetAssetPath(tempChartFile), false);
            _positionYChartFileWriter = new StreamWriter(AssetDatabase.GetAssetPath(positionYChartFile), false);
            _positionXChartFileWriter = new StreamWriter(AssetDatabase.GetAssetPath(positionXChartFile), false);
            _positionZChartFileWriter = new StreamWriter(AssetDatabase.GetAssetPath(positionZChartFile), false);
            _rotationChartFileWriter = new StreamWriter(AssetDatabase.GetAssetPath(rotationChartFile), false);
        }

        private void OnApplicationQuit()
        {
            _tempChartFileWriter.Close();
            _positionYChartFileWriter.Close();
            _positionXChartFileWriter.Close();
            _positionZChartFileWriter.Close();
            _rotationChartFileWriter.Close();
        }

        public void MeasurePosition()
        {
            if (Time.frameCount % frameRateStep == 0 && counter < maxMeasurementCount)
            {
                Vector3 currPos = objectTransform.position;
                Vector3 currRot = MapRotation(objectTransform.rotation.eulerAngles);
                float currVel = physicsManager.rb.velocity.magnitude * 2;
                
                tempChart.AddData(0, counter, physicsManager.currentTemperature);
                positionYChart.AddData(0, counter, currPos.y);
                positionYChart.AddData(1, counter, currVel);
                positionXChart.AddData(0, counter, currPos.x);
                positionZChart.AddData(0, counter, currPos.z);
                rotationChart.AddData(0, counter, currRot.x);
                rotationChart.AddData(1, counter, currRot.y);
                rotationChart.AddData(2, counter, currRot.z);
                
                _tempChartFileWriter.WriteLine($"{counter} {physicsManager.currentTemperature:F8}");
                _positionYChartFileWriter.WriteLine($"{counter} {currPos.y:F8} {currVel:F8}");
                _positionXChartFileWriter.WriteLine($"{counter} {currPos.x:F8}");
                _positionZChartFileWriter.WriteLine($"{counter} {currPos.z:F8}");
                _rotationChartFileWriter.WriteLine($"{counter} {currRot.x:F8} {currRot.y:F8} {currRot.z:F8}");
                
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