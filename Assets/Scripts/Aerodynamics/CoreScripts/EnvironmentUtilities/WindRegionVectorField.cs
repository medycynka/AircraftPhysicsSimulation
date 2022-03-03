﻿using System;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [CreateAssetMenu(fileName = "WindRegion", menuName = "Aerodynamics/Environment/Wind Region", order = 2)]
    public class WindRegionVectorField : ScriptableObject
    {
        [Header("Air Region", order = 0)] 
        [Header("Region air properties", order = 1)]
        public float cellSize = 1f;
        public int cellsX = 5;
        public int cellsY = 5;
        public int cellsZ = 5;
        public float force = 1f;

        [Header("Initial Vector Field Position", order = 1)]
        public Vector3 position;
        public Vector3 coordinates000;
        public Vector3 topCoordinates000;
        public Vector3[,,] vectors;
        public bool initialized = false;
        
        [Header("Noise Parameters", order = 1)]
        [SerializeField] float seed = 1;
        [SerializeField] float frequency = 0.1f;
        [SerializeField] float amplitude = 4f;
        [SerializeField] float persistence = 1f;
        [SerializeField] int octave = 1;

        [Header("Draw properties", order = 1)] 
        public bool drawSegments;
        public bool drawArrows = true;
        public Color regionArrowColor = new Color(1f, 0.42f, 0f, 1f);
        public Color regionSegmentColor = new Color(1f, 1f, 0f, 1f);
        public Color topSegmentColor1 = new Color(1f, 0f, 0f, 1f);
        public Color topSegmentColor2 = new Color(1f, 1f, 1f, 1f);

        private void OnValidate()
        {
            InitializeCoordinates000();
            InitializeVectors();
            
            initialized = true;
        }

        public void ForceInit(Vector3 initialPosition)
        {
            position = initialPosition;
            
            InitializeCoordinates000();
            InitializeVectors();
                
            initialized = true;
        }
        
        public void Init()
        {
            if(!initialized)
            {
                InitializeCoordinates000();
                InitializeVectors();
                
                initialized = true;
            }
        }
        
        private void InitializeVectors() 
        {
            vectors = new Vector3[cellsX, cellsY, cellsZ];

            for (int x = 0; x < cellsX; x++)
            {
                for (int y = 0; y < cellsY; y++)
                {
                    for (int z = 0; z < cellsZ; z++)
                    {
                        float noise = PerlinNoise3D.GetValue(x, y, z, seed, frequency, amplitude, persistence, octave);
                        float noisePI = noise * Mathf.PI;
                        Vector3 noiseDirection = new Vector3(Mathf.Cos(noisePI), Mathf.Sin(noisePI), Mathf.Cos(noisePI));
                        // Debug.Log($"[{x}, {y}, {z}] : {noiseDirection.normalized}");
                        vectors[x, y, z] = noiseDirection.normalized;
                    }
                }
            }
        }

        public void InitializeCoordinates000()
        {
            float x0 = (cellsX / 2f) * cellSize;
            float y0 = (cellsY / 2f) * cellSize;
            float z0 = (cellsZ / 2f) * cellSize;

            // On the top corner
            topCoordinates000 = position - new Vector3(x0, y0, z0);

            // On center of the first cube
            coordinates000 = topCoordinates000 + new Vector3(cellSize / 2f, cellSize / 2f, cellSize / 2f);
        }

        private Vector3Int VectorIndexInWorldCoordinates(Vector3 worldCoordinates)
        {
            Vector3 distanceVector = worldCoordinates - topCoordinates000;
            Vector3Int vectorIndex = Vector3Int.FloorToInt(distanceVector / cellSize);

            if(vectorIndex.x < 0 || vectorIndex.x >= cellsX ||
               vectorIndex.y < 0 || vectorIndex.y >= cellsY ||
               vectorIndex.z < 0 || vectorIndex.z >= cellsZ
              )
            {
                return new Vector3Int(-1, -1, -1);
            } 
            else
            {
                return vectorIndex;
            }
        }

        private Vector3 VectorValueInWorldCoordinates(Vector3 worldCoordinates)
        {
            Vector3Int vectorIndex = VectorIndexInWorldCoordinates(worldCoordinates);
            
            if(vectorIndex.x == -1)
            {
                return Vector3.zero;
            } 
            else
            {
                return vectors[vectorIndex.x, vectorIndex.y, vectorIndex.z];
            }
        }

        public Vector3 GetWindVector(Vector3 worldCoordinates)
        {
            return VectorValueInWorldCoordinates(worldCoordinates) * force;
        }

        public void DrawInGizmos()
        {
            // Coordinates 000
            Handles.color = topSegmentColor2;
            Handles.DrawLine(position, coordinates000);
            Handles.color = topSegmentColor1;
            Handles.DrawLine(coordinates000, topCoordinates000);

            // Draw Cubes
            if (drawSegments)
            {
                Handles.color = regionSegmentColor;
                for (int x = 0; x < cellsX; x++)
                {
                    for (int y = 0; y < cellsY; y++)
                    {
                        for (int z = 0; z < cellsZ; z++)
                        {
                            Vector3 cubeCoordinates =
                                new Vector3(
                                    coordinates000.x + (x * cellSize),
                                    coordinates000.y + (y * cellSize),
                                    coordinates000.z + (z * cellSize)
                                );

                            Vector3 cubeDimensions = new Vector3(cellSize, cellSize, cellSize);

                            Handles.DrawWireCube(cubeCoordinates, cubeDimensions);
                        }
                    }
                }
            }

            // Draw Vectors
            if(initialized && drawArrows)
            {
                Handles.color = regionArrowColor;

                for (int x = 0; x < cellsX; x++)
                {
                    for (int y = 0; y < cellsY; y++)
                    {
                        for (int z = 0; z < cellsZ; z++)
                        {
                            Vector3 vectorCoordinates =
                                new Vector3(
                                    coordinates000.x + (x * cellSize),
                                    coordinates000.y + (y * cellSize),
                                    coordinates000.z + (z * cellSize)
                                );

                            Handles.ArrowHandleCap(0, vectorCoordinates, 
                                Quaternion.LookRotation(vectors[x, y, z]), cellSize / 2f, EventType.Repaint
                                );
                        }
                    }
                }
            }
        }
    }
}