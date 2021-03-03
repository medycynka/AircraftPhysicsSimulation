using UnityEngine;


[CreateAssetMenu(fileName = "New Aerodynamic Surface Config", menuName = "Aerodynamics/Aerodynamic Surface Configuration")]
public class AerodynamicSurface : ScriptableObject
{
    public float liftSlope = 6.28f;
    public float skinFriction = 0.02f;
    public float zeroLiftAoA;
    public float stallAngleHigh = 15;
    public float stallAngleLow = -15;
    public float chord = 1;
    public float flapFraction;
    public float span = 1;
    public bool autoAspectRatio = true;
    public float aspectRatio = 2;

    private void OnValidate()
    {
        if (flapFraction > 0.4f)
        {
            flapFraction = 0.4f;
        }

        if (flapFraction < 0)
        {
            flapFraction = 0;
        }

        if (stallAngleHigh < 0)
        {
            stallAngleHigh = 0;
        }

        if (stallAngleLow > 0)
        {
            stallAngleLow = 0;
        }

        if (chord < 1e-3f)
        {
            chord = 1e-3f;
        }

        if (autoAspectRatio)
        {
            aspectRatio = span / chord;
        }
    }
}
