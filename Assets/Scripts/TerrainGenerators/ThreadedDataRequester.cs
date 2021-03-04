using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    private static ThreadedDataRequester _instance;
    private Queue<ThreadInfo> _dataQueue = new Queue<ThreadInfo>();
    //private Queue<ThreadInfo<MeshData>> _meshDataThreadQueue = new Queue<ThreadInfo<MeshData>>();

    private void Awake()
    {
        _instance = FindObjectOfType<ThreadedDataRequester>();
    }

    private void Update()
    {
        if (_dataQueue.Count > 0)
        {
            for (int i = 0; i < _dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = _dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        
        // if (_meshDataThreadQueue.Count > 0)
        // {
        //     for (int i = 0; i < _meshDataThreadQueue.Count; i++)
        //     {
        //         ThreadInfo<MeshData> threadInfo = _meshDataThreadQueue.Dequeue();
        //         threadInfo.callback(threadInfo.parameter);
        //     }
        // }
    }
    
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            _instance.DataThread(generateData, callback);
        };
        
        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        lock (_dataQueue)
        {
            _dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }
    
    // public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback)
    // {
    //     ThreadStart threadStart = delegate
    //     {
    //         MeshDataThread(heightMap, lod, callback);
    //     };
    //     
    //     new Thread(threadStart).Start();
    // }
    //
    // private void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback)
    // {
    //     MeshData meshData = Generator.GenerateMeshTerrain(heightMap.values, meshSettings, lod);
    //     lock (_meshDataThreadQueue)
    //     {
    //         _meshDataThreadQueue.Enqueue(new ThreadInfo<MeshData>(callback, meshData));
    //     }
    // }
    
    private struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
