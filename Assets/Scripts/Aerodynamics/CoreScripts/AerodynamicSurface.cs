using UnityEngine;


[CreateAssetMenu(fileName = "Aerodynamic Surface Config", menuName = "Aerodynamics/Aerodynamic Surface Configuration")]
public class AerodynamicSurface : ScriptableObject
{
    public float liftSlope = 6.28f;
    public float skinFriction = 0.02f;
    public float zeroLiftAoA;
    [Range(0, 30)] public float stallAngleHigh = 15;
    [Range(-30, 0)]public float stallAngleLow = -15;
    [Range(0.001f, 5)] public float chord = 1;
    [Range(0, 0.6f)] public float flapFraction;
    public float span = 1;
    public bool autoAspectRatio = true;
    public float aspectRatio = 2;

    private void OnValidate()
    {
        if (autoAspectRatio)
        {
            aspectRatio = span / chord;
        }
    }
}
