using System;
using System.Collections.Generic;
using UnityEngine;


public class TerrainGenerator : MonoBehaviour
{
	private const float ViewerMoveThresholdForChunkUpdate = 25f;
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

	public int colliderLODId;
	public LODInfo[] detailLevels;
	//public static float maxViewDst;
	public Transform viewer;
	public Material mapMaterial;
	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	
	private Vector2 _viewerPosition;
	private Vector2 _viewerPositionOld;
	private float _meshWorldSize;
	private int _chunksVisibleInViewDst;
	private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

	private void Start() 
	{
		float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
		_meshWorldSize = meshSettings.meshWorldSize;
		_chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / _meshWorldSize);

		UpdateVisibleChunks ();
	}

	private void Update()
	{
		_viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		if (_viewerPosition != _viewerPositionOld)
		{
			for (int i = 0; i < _visibleTerrainChunks.Count; i++)
			{
				_visibleTerrainChunks[i].UpdateCollisionMesh();
			}
		}
		
		if ((_viewerPositionOld - _viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) 
		{
			_viewerPositionOld = _viewerPosition;
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
			
		int currentChunkCoordX = Mathf.RoundToInt (_viewerPosition.x / _meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (_viewerPosition.y / _meshWorldSize);

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
						TerrainChunk terrainChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings,
							detailLevels, colliderLODId, transform, viewer, mapMaterial);
						_terrainChunkDictionary.Add(viewedChunkCoord, terrainChunk);
						terrainChunk.ONVisibilityChanged += OnTerrainChunkVisibilityChanged;
						terrainChunk.Load();
					}
				}
			}
		}
	}

	private void OnTerrainChunkVisibilityChanged(TerrainChunk terrainChunk, bool isVisible)
	{
		if (isVisible)
		{
			_visibleTerrainChunks.Add(terrainChunk);
		}
		else
		{
			_visibleTerrainChunks.Remove(terrainChunk);
		}
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