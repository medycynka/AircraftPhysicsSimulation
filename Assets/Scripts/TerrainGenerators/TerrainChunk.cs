using System;
using UnityEngine;

public class TerrainChunk
{
	private const float ColliderGenerationDistanceThreshold = 5f;
	
	public event Action<TerrainChunk, bool> ONVisibilityChanged; 
	public Vector2 coord;
	
	private GameObject _meshObject;
	private Vector2 _sampleCenter;
	private Bounds _bounds;

	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;
	private MeshCollider _meshCollider;

	private LODInfo[] _detailLevels;
	private LODMesh[] _lodMeshes;
	private int _colliderLODId;
	private float _maxViewDistance;

	private HeightMap _heightMap;
	private bool _heightMapReceived;
	private int _previousLODIndex = -1;
	private bool _hasSetCollider;

	private HeightMapSettings _heightMapSettings;
	private MeshSettings _meshSettings;

	private Transform _viewer;

	public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, 
		LODInfo[] detailLevels, int colliderLODId, Transform parent, Transform viewer, Material material)
	{
		this.coord = coord;
		_detailLevels = detailLevels;
		_colliderLODId = colliderLODId;
		_heightMapSettings = heightMapSettings;
		_meshSettings = meshSettings;
		_viewer = viewer;

		_sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
		Vector2 position = coord * meshSettings.meshWorldSize;
		_bounds = new Bounds(position,Vector2.one * meshSettings.meshWorldSize);

		_meshObject = new GameObject("Terrain Chunk");
		_meshRenderer = _meshObject.AddComponent<MeshRenderer>();
		_meshFilter = _meshObject.AddComponent<MeshFilter>();
		_meshRenderer.material = material;
		_meshCollider = _meshObject.AddComponent<MeshCollider>();

		_meshObject.transform.position = new Vector3(position.x, 0, position.y);
		_meshObject.transform.parent = parent;
		SetVisible(false);

		_lodMeshes = new LODMesh[detailLevels.Length];
		
		for (int i = 0; i < detailLevels.Length; i++) 
		{
			_lodMeshes[i] = new LODMesh(detailLevels[i].lod);
			_lodMeshes[i].UpdateCallback += UpdateTerrainChunk;

			if (i == _colliderLODId)
			{
				_lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
			}
		}

		_maxViewDistance = _detailLevels[_detailLevels.Length - 1].visibleDstThreshold;
	}

	private Vector2 viewerPosition => new Vector2(_viewer.position.x, _viewer.position.z);

	public void Load()
	{
		ThreadedDataRequester.RequestData(
			() => HeightMapGenerator.GenerateHeightMap(_meshSettings.numVertsPerLine, _meshSettings.numVertsPerLine,
				_heightMapSettings, _sampleCenter), OnHeightMapReceived);
	}

	private void OnHeightMapReceived(object heightMap) 
	{
		_heightMap = (HeightMap)heightMap;
		_heightMapReceived = true;

		UpdateTerrainChunk ();
	}

	public void UpdateTerrainChunk() 
	{
		if (_heightMapReceived) 
		{
			float viewerDstFromNearestEdge = Mathf.Sqrt (_bounds.SqrDistance (viewerPosition));
			bool wasVisible = IsVisible();
			bool visible = viewerDstFromNearestEdge <= _maxViewDistance;

			if (visible) 
			{
				int lodIndex = 0;

				for (int i = 0; i < _detailLevels.Length - 1; i++) 
				{
					if (viewerDstFromNearestEdge > _detailLevels [i].visibleDstThreshold)
					{
						lodIndex = i + 1;
					} 
					else 
					{
						break;
					}
				}

				if (lodIndex != _previousLODIndex) 
				{
					LODMesh lodMesh = _lodMeshes[lodIndex];
					
					if (lodMesh.hasMesh) 
					{
						_previousLODIndex = lodIndex;
						_meshFilter.mesh = lodMesh.mesh;
					} 
					else if (!lodMesh.hasRequestedMesh) 
					{
						lodMesh.RequestMesh(_heightMap, _meshSettings);
					}
				}
			}

			if (wasVisible != visible)
			{
				SetVisible(visible);

				ONVisibilityChanged?.Invoke(this, visible);
			}
		}
	}

	public void UpdateCollisionMesh()
	{
		if (!_hasSetCollider)
		{
			float sqrtFromViewerToEdge = _bounds.SqrDistance(viewerPosition);

			if (sqrtFromViewerToEdge < _detailLevels[_colliderLODId].sqrtVisibleDistanceTreshold)
			{
				if (!_lodMeshes[_colliderLODId].hasRequestedMesh)
				{
					_lodMeshes[_colliderLODId].RequestMesh(_heightMap, _meshSettings);
				}
			}

			if (sqrtFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
			{
				if (_lodMeshes[_colliderLODId].hasMesh)
				{
					_meshCollider.sharedMesh = _lodMeshes[_colliderLODId].mesh;
					_hasSetCollider = true;
				}
			}
		}
	}

	public void SetVisible(bool visible) 
	{
		_meshObject.SetActive (visible);
	}

	public bool IsVisible() 
	{
		return _meshObject.activeSelf;
	}

}

class LODMesh {

	public Mesh mesh;
	public bool hasRequestedMesh;
	public bool hasMesh;
	private int _lod;
	public event Action UpdateCallback;

	public LODMesh(int lod) 
	{
		_lod = lod;
	}

	private void OnMeshDataReceived(object meshData) 
	{
		mesh = ((MeshData)meshData).GenerateMesh();
		hasMesh = true;

		UpdateCallback();
	}

	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) 
	{
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => Generator.GenerateMeshTerrain(heightMap.values, meshSettings, _lod),
			OnMeshDataReceived);
	}
}
