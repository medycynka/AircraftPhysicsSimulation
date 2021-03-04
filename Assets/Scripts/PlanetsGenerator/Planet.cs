using UnityEngine;


public class Planet : MonoBehaviour
{
    [Range(2,256)] public int resolution = 10;
    public bool autoUpdate = true;
    
    public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back }
    public FaceRenderMask faceRenderMask;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;

    [HideInInspector]
    public bool shapeSettingsFoldout;
    [HideInInspector]
    public bool colourSettingsFoldout;

    private ShapeGenerator _shapeGenerator = new ShapeGenerator();
    private ColorGenerator _colorGenerator = new ColorGenerator();

    private MeshFilter[] _meshFilters;
    private TerrainFace[] _terrainFaces;

    void Initialize()
    {
        _shapeGenerator.UpdateSettings(shapeSettings);
        _colorGenerator.UpdateSettings(colorSettings);

        if (_meshFilters == null || _meshFilters.Length == 0)
        {
            _meshFilters = new MeshFilter[6];
        }
        
        _terrainFaces = new TerrainFace[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            if (_meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                _meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                _meshFilters[i].sharedMesh = new Mesh();
            }
            
            _meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMaterial;
            _terrainFaces[i] = new TerrainFace(_shapeGenerator, _meshFilters[i].sharedMesh, resolution, directions[i]);
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            _meshFilters[i].gameObject.SetActive(renderFace);
        }
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColours();
    }

    public void OnShapeSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateMesh();
        }
    }

    public void OnColourSettingsUpdated()
    {
        if (autoUpdate)
        {
            Initialize();
            GenerateColours();
        }
    }

    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (_meshFilters[i].gameObject.activeSelf)
            {
                _terrainFaces[i].ConstructMesh();
            }
        }

        _colorGenerator.UpdateElevation(_shapeGenerator.elevationMinMax);
    }

    void GenerateColours()
    {
        _colorGenerator.UpdateColours();
        for (int i = 0; i < 6; i++)
        {
            if (_meshFilters[i].gameObject.activeSelf)
            {
                _terrainFaces[i].UpdateUVs(_colorGenerator);
            }
        }
    }
}
