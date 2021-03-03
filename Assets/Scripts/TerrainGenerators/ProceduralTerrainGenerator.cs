using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;


public class ProceduralTerrainGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiceMap,
        Mesh,
        Falloff
    }

    public NoiceData noiceData;
    public TerrainData terrainData;
    public TextureData textureData;

    public Material terrainMaterial;
    
    public MapDisplayer mapDisplayer;
    [Range(0, 6)] public int editorLOD;
    public DrawMode drawMode;

    private float[,] falloffMap;
    private Queue<MapThreadInfo<MapData>> _mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> _meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

    public int mapChunkSize
    {
        get
        {
            if (terrainData.useFlattShading)
            {
                return 95;
            }

            return 239;
        }
    }

    private void Update()
    {
        if (_mapDataThreadQueue.Count > 0)
        {
            for (int i = 0; i < _mapDataThreadQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = _mapDataThreadQueue.Dequeue();
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
        if (terrainData != null)
        {
            terrainData.ONValuesUpdated -= OnValuesUpdated;
            terrainData.ONValuesUpdated += OnValuesUpdated;
        }
        
        if (noiceData != null)
        {
            noiceData.ONValuesUpdated -= OnValuesUpdated;
            noiceData.ONValuesUpdated += OnValuesUpdated;
        }
        
        if (textureData != null)
        {
            textureData.ONValuesUpdated -= OnValuesUpdated;
            textureData.ONValuesUpdated += OnValuesUpdated;
        }
    }
    
    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        
        switch (drawMode)
        {
            case DrawMode.NoiceMap:
                mapDisplayer.DrawTexture(NoiceGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.Mesh:
                mapDisplayer.DrawMesh(
                    NoiceGenerator.GenerateMeshTerrain(mapData.heightMap, terrainData.meshHeightMultiplier,
                        terrainData.meshHeightCurve, editorLOD, terrainData.useFlattShading));
                break;
            case DrawMode.Falloff:
                mapDisplayer.DrawTexture(NoiceGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (_mapDataThreadQueue)
        {
            _mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }
    
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData =
            NoiceGenerator.GenerateMeshTerrain(mapData.heightMap, terrainData.meshHeightMultiplier,
                terrainData.meshHeightCurve, lod, terrainData.useFlattShading);
        lock (_meshDataThreadQueue)
        {
            _meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }
   
    private MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiceMap = NoiceGenerator.GenerateNoiceMap(mapChunkSize + 2, mapChunkSize + 2, 
            noiceData.seed, noiceData.perlinNoiseScale, noiceData.octaves, noiceData.persistence, noiceData.lacunarity,
            centre + noiceData.offset, noiceData.normalizeMode);

        if (terrainData.useFalloff)
        {
            falloffMap ??= FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    if (terrainData.useFalloff)
                    {
                        noiceMap[x, y] = Mathf.Clamp01(noiceMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }
        
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

        return new MapData(noiceMap);
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

[Serializable]
public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}
