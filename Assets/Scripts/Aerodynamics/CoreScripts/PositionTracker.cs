using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts;


namespace Aerodynamics.CoreScripts
{
    public class PositionTracker : MonoBehaviour
    {
        [Range(2, 300)] public int frameRateStep = 60;
        [Range(2, 10000)] public int maxMeasurementCount = 1000;
        public Transform objectTransform;
        public PhysicsManager physicsManager;
        public LineChart tempChart;
        public LineChart positionYChart;
        public LineChart positionXChart;
        public LineChart positionZChart;

        private int counter = 0;

        private void Awake()
        {
            tempChart.series.ClearData();
            positionYChart.series.ClearData();
            positionXChart.series.ClearData();
            positionZChart.series.ClearData();
        }

        public void MeasurePosition()
        {
            if (Time.frameCount % frameRateStep == 0 && counter < maxMeasurementCount)
            {
                Vector3 currPos = objectTransform.position;
                tempChart.AddData(0, counter, physicsManager.currentTemperature);
                positionYChart.AddData(0, counter, currPos.y);
                positionYChart.AddData(1, counter, physicsManager.rb.velocity.magnitude * 2);
                positionXChart.AddData(0, counter, currPos.x);
                positionZChart.AddData(0, counter, currPos.z);
                counter++;
            }
        }
    }
}