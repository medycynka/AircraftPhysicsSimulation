using System;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public enum WindFlowType
    {
        Default,
        Spiral,
        Custom,
        PerlinNoise
    }

    public enum FunctionType
    {
        None,
        XYZ,
        XZY,
        ZXY,
        mYmZX,
        ABC,
        PQR,
    }
    
    [CreateAssetMenu(fileName = "WindRegion", menuName = "Aerodynamics/Environment/Wind Region", order = 2)]
    public class WindRegionVectorField : ScriptableObject
    {
        [Header("Wind Region", order = 0)] 
        [Header("Wind Region properties", order = 1)]
        public float cellSize = 1f;
        public int cellsX = 5;
        public int cellsY = 5;
        public int cellsZ = 5;
        [Range(1.0f, 400.0f)] public float force = 1f;
        public WindFlowType flowType = WindFlowType.PerlinNoise;

        [Header("Initial Vector Field Position", order = 1)]
        public Vector3 position;
        public Vector3Int offset;
        public Vector3 direction;
        public Vector3 coordinates000;
        public Vector3 topCoordinates000;
        public Vector3[][][] vectors;
        public bool initialized = false;
        
        [Header("Custom Parameters", order = 1)]
        [SerializeField] private FunctionType functionType;
        [SerializeField] private float a = 1f;
        [SerializeField] private float b = Mathf.Sqrt(2f / 3f);
        [SerializeField] private float c = Mathf.Sqrt(1f / 3f);
        [SerializeField] private CustomFunctionBase p;
        [SerializeField] private CustomFunctionBase q;
        [SerializeField] private CustomFunctionBase r;
        
        [Header("Noise Parameters", order = 1)]
        [SerializeField] float seed = 1;
        [SerializeField, Range(-1f, 1f)] float frequency = 0.1f;
        [SerializeField] float amplitude = 4f;
        [SerializeField] float persistence = 1f;
        [SerializeField, Range(1, 64)] int octave = 1;

        [Header("Draw properties", order = 1)] 
        public bool drawSegments;
        public bool drawArrows = true;
        public Color regionArrowColor = new Color(1f, 0.42f, 0f, 1f);
        public Color regionSegmentColor = new Color(1f, 1f, 0f, 1f);
        public Color topSegmentColor1 = new Color(1f, 0f, 0f, 1f);
        public Color topSegmentColor2 = new Color(1f, 1f, 1f, 1f);

        private void OnValidate()
        {
            direction.Normalize();

            InitializeCoordinates000();
            InitializeVectors();
            
            initialized = true;
        }

        public void ValidateChildren()
        {
            OnValidate();
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
            switch (flowType)
            {
                case WindFlowType.Default:
                    InitializeVectorsDefault();
                    break;
                case WindFlowType.Spiral:
                    InitializeVectorsSpiral();
                    break;
                case WindFlowType.Custom:
                    InitializeVectorsCustom();
                    break;
                case WindFlowType.PerlinNoise:
                    InitializeVectorsPerlin();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Func<int, int, int, Vector3> GetFunction(FunctionType expression)
        {
            return expression switch
            {
                FunctionType.None => (x, y, z) => Vector3.up,
                FunctionType.XYZ => (x, y, z) => new Vector3(x, y, z),
                FunctionType.XZY => (x, y, z) => new Vector3(x, z, y),
                FunctionType.ZXY => (x, y, z) => new Vector3(z, x, y),
                FunctionType.mYmZX => (x, y, z) => new Vector3(-y, -z, x),
                FunctionType.ABC => (x, y, z) => new Vector3(a * Mathf.Sin(z) + c * Mathf.Cos(y), b * Mathf.Sin(x) + a * Mathf.Cos(z), c * Mathf.Sin(y) + b * Mathf.Cos(x)),
                FunctionType.PQR => (x, y, z) => Vector3.zero,
                _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, null)
            };
        }

        private void InitializeVectorsDefault() 
        {
            vectors = new Vector3[cellsX][][];

            if (direction == Vector3.zero)
            {
                direction = Vector3.forward;
            }

            for (int x = 0; x < cellsX; x++)
            {
                vectors[x] = new Vector3[cellsY][];
                for (int y = 0; y < cellsY; y++)
                {
                    vectors[x][y] = new Vector3[cellsZ];
                    for (int z = 0; z < cellsZ; z++)
                    {
                        vectors[x][y][z] = direction;
                    }
                }
            }
        }
        
        private void InitializeVectorsSpiral() 
        {
            vectors = new Vector3[cellsX][][];

            for (int x = 0; x < cellsX; x++)
            {
                vectors[x] = new Vector3[cellsY][];
                for (int y = 0; y < cellsY; y++)
                {
                    vectors[x][y] = new Vector3[cellsZ];
                    for (int z = 0; z < cellsZ; z++)
                    {
                        int xOff = x + offset.x;
                        int yOff = y + offset.y;
                        int zOff = z + offset.z;
                        
                        Vector3 noiseDirection = new Vector3(zOff - xOff, 0, -zOff - xOff);
                        vectors[x][y][z] = noiseDirection.normalized;
                    }
                }
            }
        }

        private void InitializeVectorsCustom() 
        {
            vectors = new Vector3[cellsX][][];
            
            if (functionType == FunctionType.PQR)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    vectors[x] = new Vector3[cellsY][];
                    for (int y = 0; y < cellsY; y++)
                    {
                        vectors[x][y] = new Vector3[cellsZ];
                        for (int z = 0; z < cellsZ; z++)
                        {
                            int xOff = x + offset.x;
                            int yOff = y + offset.y;
                            int zOff = z + offset.z;
                            
                            Vector3 noiseDirection = new Vector3(
                                p.GetValue(xOff, yOff, zOff), 
                                q.GetValue(xOff, yOff, zOff), 
                                r.GetValue(xOff, yOff, zOff)
                                );
                            vectors[x][y][z] = noiseDirection.normalized;
                        }
                    }
                }
            }
            else
            {
                Func<int, int, int, Vector3> func = GetFunction(functionType);

                for (int x = 0; x < cellsX; x++)
                {
                    vectors[x] = new Vector3[cellsY][];
                    for (int y = 0; y < cellsY; y++)
                    {
                        vectors[x][y] = new Vector3[cellsZ];
                        for (int z = 0; z < cellsZ; z++)
                        {
                            int xOff = x + offset.x;
                            int yOff = y + offset.y;
                            int zOff = z + offset.z;
                            
                            Vector3 noiseDirection = func(xOff, yOff, zOff);
                            vectors[x][y][z] = noiseDirection.normalized;
                        }
                    }
                }
            }
        }
        
        private void InitializeVectorsPerlin() 
        {
            vectors = new Vector3[cellsX][][];

            for (int x = 0; x < cellsX; x++)
            {
                vectors[x] = new Vector3[cellsY][];
                for (int y = 0; y < cellsY; y++)
                {
                    vectors[x][y] = new Vector3[cellsZ];
                    for (int z = 0; z < cellsZ; z++)
                    {
                        int xOff = x + offset.x;
                        int yOff = y + offset.y;
                        int zOff = z + offset.z;
                        float noise = PerlinNoise3D.GetValue(xOff, yOff, zOff, seed, frequency, amplitude, persistence, octave);
                        float noisePI = noise * Mathf.PI;
                        Vector3 noiseDirection = new Vector3(Mathf.Cos(noisePI), Mathf.Sin(noisePI), Mathf.Cos(noisePI));
                        
                        vectors[x][y][z] = noiseDirection.normalized;
                    }
                }
            }
        }

        private void InitializeCoordinates000()
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
            
            return vectorIndex;
        }

        private Vector3 VectorValueInWorldCoordinates(Vector3 worldCoordinates)
        {
            Vector3Int vectorIndex = VectorIndexInWorldCoordinates(worldCoordinates);
            
            if(vectorIndex.x == -1)
            {
                return Vector3.zero;
            } 
            
            return vectors[vectorIndex.x][vectorIndex.y][vectorIndex.z];
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
                                Quaternion.LookRotation(vectors[x][y][z]), cellSize / 2f, EventType.Repaint
                                );
                        }
                    }
                }
            }
        }
    }
}