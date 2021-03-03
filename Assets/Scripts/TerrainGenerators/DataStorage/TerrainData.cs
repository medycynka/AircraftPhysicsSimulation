using UnityEngine;


[CreateAssetMenu(fileName = "TerrainDataConfiguration", menuName = "Procedural Terrain/Terrain Data")]
public class TerrainData : DataUpdater
{
    public bool useFalloff;
    public bool useFlattShading;
    public float uniformScale = 2.5f;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public float minHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);

    public float maxHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
}
