using UnityEngine;


public static class Generator
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= settings.persistence;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;

                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistence;
                    frequency *= settings.lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }

                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }

    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point, 
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    public static MeshData GenerateMeshTerrain(float[,] heightMap, MeshSettings meshSettings, int lod)
    {
        int meshSimplificationIncrement = lod < 1 ? 1 : lod * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        float topLeftX = (meshSizeUnsimplified - 1) / (-2f);
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;
        MeshData meshData = new MeshData(verticesPerLine, meshSettings.useFlatShading);

        int[][] vertexIndicesMap = new int[borderedSize][];
        for (int index = 0; index < borderedSize; index++)
        {
            vertexIndicesMap[index] = new int[borderedSize];
        }

        int meshVertexId = 0;
        int borderVertexId = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                if (y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1)
                {
                    vertexIndicesMap[x][y] = borderVertexId;
                    borderVertexId--;
                }
                else
                {
                    vertexIndicesMap[x][y] = meshVertexId;
                    meshVertexId++;
                }
            }
        }
        
        
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x][y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float) meshSize,
                    (y - meshSimplificationIncrement) / (float) meshSize);
                float height = heightMap[x, y];
                Vector3 vertexPosition =
                    new Vector3((topLeftX + percent.x * meshSizeUnsimplified) * meshSettings.meshScale, height,
                        (topLeftZ - percent.y * meshSizeUnsimplified) * meshSettings.meshScale);
                
                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x][y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement][y];
                    int c = vertexIndicesMap[x][y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement][y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
            }
        }
        
        meshData.FinalizeCalculations();

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector2[] _uvs;
    private Vector3[] _borderVertices;
    private int[] _borderTriangles;
    private int _triangleId;
    private int _borderTriangleId;
    private Vector3[] _bakedNormals;
    private bool _useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        _vertices = new Vector3[verticesPerLine * verticesPerLine];
        _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        _uvs = new Vector2[verticesPerLine * verticesPerLine];
        _borderVertices = new Vector3[verticesPerLine * 4 + 4];
        _borderTriangles = new int[24 * verticesPerLine];
        _useFlatShading = useFlatShading;
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            _borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            _vertices[vertexIndex] = vertexPosition;
            _uvs[vertexIndex] = uv;
        }
    }
    
    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            _borderTriangles[_borderTriangleId] = a;
            _borderTriangles[_borderTriangleId + 1] = b;
            _borderTriangles[_borderTriangleId + 2] = c;
            _borderTriangleId += 3;  
        }
        else
        {
            _triangles[_triangleId] = a;
            _triangles[_triangleId + 1] = b;
            _triangles[_triangleId + 2] = c;
            _triangleId += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] normals = new Vector3[_vertices.Length];
        int triangleCount = _triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleId = i * 3;
            int vertexIndexA = _triangles[normalTriangleId];
            int vertexIndexB = _triangles[normalTriangleId + 1];
            int vertexIndexC = _triangles[normalTriangleId + 2];

            Vector3 abcNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            normals[vertexIndexA] += abcNormal;
            normals[vertexIndexB] += abcNormal;
            normals[vertexIndexC] += abcNormal;
        }
        
        int borderTriangleCount = _borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleId = i * 3;
            int vertexIndexA = _borderTriangles[normalTriangleId];
            int vertexIndexB = _borderTriangles[normalTriangleId + 1];
            int vertexIndexC = _borderTriangles[normalTriangleId + 2];

            Vector3 abcNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0)
            {
                normals[vertexIndexA] += abcNormal;
            }

            if (vertexIndexB >= 0)
            {
                normals[vertexIndexB] += abcNormal;
            }

            if (vertexIndexC >= 0)
            {
                normals[vertexIndexC] += abcNormal;
            }
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }

        return normals;
    }

    private Vector3 SurfaceNormalFromIndices(int aId, int bId, int cId)
    {
        Vector3 pointA = aId < 0 ? _borderVertices[-aId - 1] : _vertices[aId];
        Vector3 pointB = bId < 0 ? _borderVertices[-bId - 1] : _vertices[bId];
        Vector3 pointC = cId < 0 ? _borderVertices[-cId - 1] : _vertices[cId];
        Vector3 ab = pointB - pointA;
        Vector3 ac = pointC - pointA;

        return Vector3.Cross(ab, ac).normalized;
    }

    public void FinalizeCalculations()
    {
        if (_useFlatShading)
        {
            FlattShading();
        }
        else
        {
            BakeNormals();
        }
    }
    private void BakeNormals()
    {
        _bakedNormals = CalculateNormals();
    }

    private void FlattShading()
    {
        Vector3[] flatShadedVecrtices = new Vector3[_triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[_triangles.Length];

        for (int i = 0; i < _triangles.Length; i++)
        {
            flatShadedVecrtices[i] = _vertices[_triangles[i]];
            flatShadedUvs[i] = _uvs[_triangles[i]];
            _triangles[i] = i;
        }

        _vertices = flatShadedVecrtices;
        _uvs = flatShadedUvs;
    }
    
    public Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = _vertices, 
            triangles = _triangles, 
            uv = _uvs
        };

        if (_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = _bakedNormals;
        }

        return mesh;
    }
}


[System.Serializable]
public class NoiseSettings
{
    public int seed;
    [Range(0.01f, 100)] public float scale = 40f;
    [Range(1, 10)] public int octaves = 8;
    [Range(0, 1)] public float persistence = 0.5f;
    [Range(1, 25)]public float lacunarity = 2f;
    public Vector2 offset;
    public NormalizeMode normalizeMode;
}