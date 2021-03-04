using System;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;
    [Range(0, MeshSettings.NumSupportedLODs - 1)] public int editorLOD;
    
    public DrawMode drawMode;

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
                DrawTexture(Generator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.Mesh:
                DrawMesh(Generator.GenerateMeshTerrain(heightMap.values, meshSettings, editorLOD));
                break;
            case DrawMode.Falloff:
                DrawTexture(
                    Generator.TextureFromHeightMap(
                        new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    private void DrawMesh(MeshData meshData)
    {
        meshFilter.mesh = meshData.GenerateMesh();
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }
}
