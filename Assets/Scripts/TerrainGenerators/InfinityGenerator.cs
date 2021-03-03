using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class InfinityGenerator : MonoBehaviour
{
	private const float ViewerMoveThresholdForChunkUpdate = 25f;
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
	private const float ColliderGenerationDistanceThreshold = 5f;
	
	public int colliderLODId;
	public LODInfo[] detailLevels;
	public static float maxViewDst;
	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	
	private static ProceduralTerrainGenerator _mapGenerator;
	
	private Vector2 _viewerPositionOld;
	private float _meshWorldSize;
	private int _chunksVisibleInViewDst;
	private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private static List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

	private void Start() 
	{
		_mapGenerator = FindObjectOfType<ProceduralTerrainGenerator> ();

		maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		_meshWorldSize = _mapGenerator.meshSettings.meshWorldSize;
		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / _meshWorldSize);

		UpdateVisibleChunks ();
	}

	private void Update()
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		if (viewerPosition != _viewerPositionOld)
		{
			for (int i = 0; i < _visibleTerrainChunks.Count; i++)
			{
				_visibleTerrainChunks[i].UpdateCollisionMesh();
			}
		}
		
		if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) 
		{
			_viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}
		
	private void UpdateVisibleChunks()
	{
		HashSet<Vector2> alreadyUpdatedChunkCords = new HashSet<Vector2>();
		
		for (int i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
		{
			alreadyUpdatedChunkCords.Add(_visibleTerrainChunks[i].coord);
			_visibleTerrainChunks[i].UpdateTerrainChunk();
		}
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / _meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / _meshWorldSize);

		for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++) 
		{
			for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++) 
			{
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (!alreadyUpdatedChunkCords.Contains(viewedChunkCoord))
				{
					if (_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
					{
						_terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					}
					else
					{
						_terrainChunkDictionary.Add(viewedChunkCoord,
							new TerrainChunk(viewedChunkCoord, _meshWorldSize, detailLevels, colliderLODId, transform,
								mapMaterial));
					}
				}
			}
		}
	}

	public class TerrainChunk
	{
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

		private HeightMap _heightMap;
		private bool _mapDataReceived;
		private int _previousLODIndex = -1;
		private bool _hasSetCollider;

		public TerrainChunk(Vector2 coord, float meshWorlSize, LODInfo[] detailLevels, int colliderLODId, Transform parent, Material material)
		{
			this.coord = coord;
			_detailLevels = detailLevels;
			_colliderLODId = colliderLODId;

			_sampleCenter = coord * meshWorlSize / _mapGenerator.meshSettings.meshScale;
			Vector2 position = coord * meshWorlSize;
			_bounds = new Bounds(position,Vector2.one * meshWorlSize);

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
				_lodMeshes[i].updateCallback += UpdateTerrainChunk;

				if (i == _colliderLODId)
				{
					_lodMeshes[i].updateCallback += UpdateCollisionMesh;
				}
			}

			_mapGenerator.RequestHeightMap(_sampleCenter, OnMapDataReceived);
		}

		private void OnMapDataReceived(HeightMap heightMap) 
		{
			_heightMap = heightMap;
			_mapDataReceived = true;

			UpdateTerrainChunk ();
		}

		public void UpdateTerrainChunk() 
		{
			if (_mapDataReceived) 
			{
				float viewerDstFromNearestEdge = Mathf.Sqrt (_bounds.SqrDistance (viewerPosition));
				bool wasVisible = IsVisible();
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

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
							lodMesh.RequestMesh(_heightMap);
						}
					}
				}

				if (wasVisible != visible)
				{
					if (visible)
					{
						_visibleTerrainChunks.Add(this);
					}
					else
					{
						_visibleTerrainChunks.Remove(this);
					}
					
					SetVisible(visible);
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
						_lodMeshes[_colliderLODId].RequestMesh(_heightMap);
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
		public event Action updateCallback;

		public LODMesh(int lod) 
		{
			_lod = lod;
		}

		private void OnMeshDataReceived(MeshData meshData) 
		{
			mesh = meshData.GenerateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(HeightMap heightMap) 
		{
			hasRequestedMesh = true;
			_mapGenerator.RequestMeshData(heightMap, _lod, OnMeshDataReceived);
		}
	}

	[Serializable]
	public struct LODInfo 
	{
		[Range(0, MeshSettings.NumSupportedLODs - 1)]
		public int lod;
		public float visibleDstThreshold;

		public float sqrtVisibleDistanceTreshold => visibleDstThreshold * visibleDstThreshold;
	}
}
