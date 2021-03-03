using UnityEngine;


[CreateAssetMenu(fileName = "TextureDataConfiguration", menuName = "Procedural Terrain/Texture Data")]
public class TextureData : DataUpdater
{
    private float savedMinHeight;
    private float savedMaxHeight;
    
    public void ApplyToMaterial(Material material)
    {
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        material.SetFloat("_MinHeight", minHeight);
        material.SetFloat("_MaxHeight", maxHeight);
    }
}
