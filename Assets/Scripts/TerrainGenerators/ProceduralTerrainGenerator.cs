using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;


public class ProceduralTerrainGenerator : MonoBehaviour
{
    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;

    public Material terrainMaterial;
    
    public MapDisplayer mapDisplayer;
    [Range(0, MeshSettings.NumSupportedLODs - 1)] public int editorLOD;
    
    public DrawMode drawMode;

    private float[,] falloffMap;
    private Queue<MapThreadInfo<HeightMap>> _heightMapThreadQueue = new Queue<MapThreadInfo<HeightMap>>();
    private Queue<MapThreadInfo<MeshData>> _meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Update()
    {
        if (_heightMapThreadQueue.Count > 0)
        {
            for (int i = 0; i < _heightMapThreadQueue.Count; i++)
            {
                MapThreadInfo<HeightMap> threadInfo = _heightMapThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        
        if (_meshDataThreadQueue.Count > 0)
        {
            for (int i = 0; i < _meshDataThreadQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = _meshDataThreadQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.ONValuesUpdated -= OnValuesUpdated;
            meshSettings.ONValuesUpdated += OnValuesUpdated;
        }
        
        if (heightMapSettings != null)
        {
            heightMapSettings.ONValuesUpdated -= OnValuesUpdated;
            heightMapSettings.ONValuesUpdated += OnValuesUpdated;
        }
    }
    
    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    public void DrawMapInEditor()
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
        
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                mapDisplayer.DrawTexture(Generator.TextureFromHeightMap(heightMap.values));
                break;
            case DrawMode.Mesh:
                mapDisplayer.DrawMesh(Generator.GenerateMeshTerrain(heightMap.values, meshSettings, editorLOD));
                break;
            case DrawMode.Falloff:
                mapDisplayer.DrawTexture(
                    Generator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RequestHeightMap(Vector2 centre, Action<HeightMap> callback)
    {
        ThreadStart threadStart = delegate
        {
            HeightMapThread(centre, callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void HeightMapThread(Vector2 centre, Action<HeightMap> callback)
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine, heightMapSettings, centre);
        lock (_heightMapThreadQueue)
        {
            _heightMapThreadQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }
    
    public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(heightMap, lod, callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
    {
        MeshData meshData = Generator.GenerateMeshTerrain(heightMap.values, meshSettings, lod);
        lock (_meshDataThreadQueue)
        {
            _meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
