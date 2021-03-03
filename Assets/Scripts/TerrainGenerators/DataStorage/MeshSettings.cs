using UnityEngine;


[CreateAssetMenu(fileName = "MeshConfiguration", menuName = "Procedural Terrain/Mesh Settings")]
public class MeshSettings : DataUpdater
{
    public const int NumSupportedLODs = 5;
    public const int NumSupportedChunkSizes = 9;
    public const int NumSupportedFlatShadedChunkSizes = 3;
    public static readonly int[] SupportedChunkSizes = { 48, 72, 96 ,120, 144, 168, 192, 216, 240 };

    public bool useFlatShading;
    public float meshScale = 2.5f;
    
    public ChunkSize chunkSize = ChunkSize._240x240;
    public FlatShadedChunkSize flatShadedChunkSize = FlatShadedChunkSize._96x96;
    
    public int numVertsPerLine => SupportedChunkSizes[useFlatShading ? (int)flatShadedChunkSize : (int)chunkSize] + 1;
    public float meshWorldSize => (numVertsPerLine - 3) * meshScale;
}
