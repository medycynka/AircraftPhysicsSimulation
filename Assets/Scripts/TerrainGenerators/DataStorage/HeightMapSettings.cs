using UnityEngine;


[CreateAssetMenu(fileName = "HeightMapConfiguration", menuName = "Procedural Terrain/Height Map Settings")]
public class HeightMapSettings : DataUpdater
{
    public MeshNoiseSettings meshNoiseSettings;
    public float heightMultiplier;
    public bool useFalloff;
    public AnimationCurve heightCurve;

    public float minHeight => heightMultiplier * heightCurve.Evaluate(0);

    public float maxHeight => heightMultiplier * heightCurve.Evaluate(1);
}
