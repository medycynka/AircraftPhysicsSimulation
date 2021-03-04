using UnityEngine;


public static class Generator
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, MeshNoiseSettings settings, Vector2 sampleCenter)
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

    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white,
                    Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]));
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    public static MeshData GenerateMeshTerrain(float[,] heightMap, MeshSettings meshSettings, int lod)
    {
        int skipIncrement = lod < 1 ? 1 : lod * 2;
        int numVertsPerLine = meshSettings.numVertsPerLine;
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;
    
        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        int[][] vertexIndicesMap = new int[numVertsPerLine][];
        for (int index = 0; index < numVertsPerLine; index++)
        {
            vertexIndicesMap[index] = new int[numVertsPerLine];
        }

        int meshVertexId = 0;
        int outOfMeshId = -1;

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x][y] = outOfMeshId;
                    outOfMeshId--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x][y] = meshVertexId;
                    meshVertexId++;
                }
            }
        }
        
        
        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                
                if (!isSkippedVertex) {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
 
                    int vertexIndex = vertexIndicesMap[x][y];
                    Vector2 percent = new Vector2 (x - 1, y - 1) / (numVertsPerLine - 3);
                    Vector2 vertexPosition2D = topLeft + new Vector2 (percent.x, -percent.y) * meshSettings.meshWorldSize;
                    float height = heightMap [x, y];
 
                    if (isEdgeConnectionVertex) {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        Coord coordA = new Coord((isVertical) ? x : x - dstToMainVertexA,
                            (isVertical) ? y - dstToMainVertexA : y);
                        Coord coordB = new Coord((isVertical) ? x : x + dstToMainVertexB,
                            (isVertical) ? y + dstToMainVertexB : y);
 
                        float heightMainVertexA = heightMap [coordA.x,coordA.y];
                        float heightMainVertexB = heightMap [coordB.x,coordB.y];
 
                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;

                        EdgeConnectionVertexData edgeConnectionVertexData = new EdgeConnectionVertexData(vertexIndex,
                            vertexIndicesMap[coordA.x][coordA.y], vertexIndicesMap[coordB.x][coordB.y],
                            dstPercentFromAToB);
                        meshData.DeclareEdgeConnectionVertex (edgeConnectionVertexData);
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent,
                        vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 &&
                                          (!isEdgeConnectionVertex || (x != 2 && y != 2));
 
                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3)
                            ? skipIncrement
                            : 1;
 
                        int a = vertexIndicesMap[x][y];
                        int b = vertexIndicesMap[x + currentIncrement][y];
                        int c = vertexIndicesMap[x][y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement][y + currentIncrement];
                        meshData.AddTriangle (a, d, c);
                        meshData.AddTriangle (d, a, b);
                    }
                }
            }
        }
        
        meshData.FinalizeCalculations();

        return meshData;
    }
}

public readonly struct Coord {
    public readonly int x;
    public readonly int y;
 
    public Coord (int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class EdgeConnectionVertexData {
    public readonly int vertexIndex;
    public readonly int mainVertexAIndex;
    public readonly int mainVertexBIndex;
    public readonly float dstPercentFromAToB;
 
    public EdgeConnectionVertexData (int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float dstPercentFromAToB)
    {
        this.vertexIndex = vertexIndex;
        this.mainVertexAIndex = mainVertexAIndex;
        this.mainVertexBIndex = mainVertexBIndex;
        this.dstPercentFromAToB = dstPercentFromAToB;
    }
}

public class MeshData
{
    private Vector3[] _vertices;
    private readonly int[] _triangles;
    private Vector2[] _uvs;
    
    private readonly Vector3[] _outOfMeshVertices;
    private readonly int[] _outOfMeshTriangles;
    
    private int _triangleId;
    private int _outOfMeshTriangleId;
    
    private Vector3[] _bakedNormals;
    
    private readonly bool _useFlatShading;
    
    private readonly EdgeConnectionVertexData[] _edgeConnectionVertices;
    private int _edgeConnectionVertexIndex;

    public MeshData(int numVertsPerLine, int skipCount, bool useFlatShading)
    {
        _useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipCount - 1) * (numVertsPerLine - 5) / skipCount * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipCount + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        _vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        _uvs = new Vector2[_vertices.Length];
        _edgeConnectionVertices = new EdgeConnectionVertexData[numEdgeConnectionVertices];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
       
        _triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        _outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        _outOfMeshTriangles = new int[(numVertsPerLine - 2) * 24];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            _outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
            _outOfMeshTriangles[_outOfMeshTriangleId] = a;
            _outOfMeshTriangles[_outOfMeshTriangleId + 1] = b;
            _outOfMeshTriangles[_outOfMeshTriangleId + 2] = c;
            _outOfMeshTriangleId += 3;  
        }
        else
        {
            _triangles[_triangleId] = a;
            _triangles[_triangleId + 1] = b;
            _triangles[_triangleId + 2] = c;
            _triangleId += 3;
        }
    }
    
    public void DeclareEdgeConnectionVertex(EdgeConnectionVertexData edgeConnectionVertexData) {
        _edgeConnectionVertices[_edgeConnectionVertexIndex] = edgeConnectionVertexData;
        _edgeConnectionVertexIndex++;
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
        
        int borderTriangleCount = _outOfMeshTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleId = i * 3;
            int vertexIndexA = _outOfMeshTriangles[normalTriangleId];
            int vertexIndexB = _outOfMeshTriangles[normalTriangleId + 1];
            int vertexIndexC = _outOfMeshTriangles[normalTriangleId + 2];

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
    
    private void ProcessEdgeConnectionVertices()
    {
        for (var i = 0; i < _edgeConnectionVertices.Length; i++)
        {
            _bakedNormals[_edgeConnectionVertices[i].vertexIndex] =
                _bakedNormals[_edgeConnectionVertices[i].mainVertexAIndex] *
                (1 - _edgeConnectionVertices[i].dstPercentFromAToB) +
                _bakedNormals[_edgeConnectionVertices[i].mainVertexBIndex] *
                _edgeConnectionVertices[i].dstPercentFromAToB;
        }
    }


    private Vector3 SurfaceNormalFromIndices(int aId, int bId, int cId)
    {
        Vector3 pointA = aId < 0 ? _outOfMeshVertices[-aId - 1] : _vertices[aId];
        Vector3 pointB = bId < 0 ? _outOfMeshVertices[-bId - 1] : _vertices[bId];
        Vector3 pointC = cId < 0 ? _outOfMeshVertices[-cId - 1] : _vertices[cId];
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
            ProcessEdgeConnectionVertices();
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
public class MeshNoiseSettings
{
    public int seed;
    [Range(0.01f, 100)] public float scale = 40f;
    [Range(1, 10)] public int octaves = 8;
    [Range(0, 1)] public float persistence = 0.5f;
    [Range(1, 25)]public float lacunarity = 2f;
    public Vector2 offset;
    public NormalizeMode normalizeMode;
}