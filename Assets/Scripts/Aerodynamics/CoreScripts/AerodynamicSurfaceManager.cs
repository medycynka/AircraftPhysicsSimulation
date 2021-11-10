using System;
using UnityEngine;


public enum ControlInputType { Pitch, Yaw, Roll, Flap }


public class AerodynamicSurfaceManager : MonoBehaviour
{
    public AerodynamicSurface config;
    public bool isControlSurface;
    public ControlInputType inputType;
    public float inputMultiplier = 1;

    private float _flapAngle;
    
    private const float deg2Rad = Mathf.Deg2Rad;
    private const float deg2Rad50 = deg2Rad * 50;
    private const float pi = Mathf.PI;

    public void SetFlapAngle(float angle)
    {
        _flapAngle = Mathf.Clamp(angle, -deg2Rad50, deg2Rad50);
    }

    public PowerTorqueVector3 CalculateForces(Vector3 worldAirVelocity, float airDensity, Vector3 relativePosition)
    {
        PowerTorqueVector3 forceAndTorque = new PowerTorqueVector3();
        
        if (!gameObject.activeInHierarchy || config == null)
        {
            return forceAndTorque;
        }

        // aspect ratio effect on lift coefficient.
        float correctedLiftSlope = CorrectedLiftSlope(config);
        // flap deflection influence on zero lift AoA and angles at which stall happens.
        float deltaLift = DeltaLift(correctedLiftSlope, Theta(config.flapFraction), _flapAngle);
        float zeroLiftAoaBase = config.zeroLiftAoA * deg2Rad;
        float zeroLiftAoA = zeroLiftAoaBase - deltaLift / correctedLiftSlope;
        float stallAngleHigh = zeroLiftAoA + MaxHigh(correctedLiftSlope, config, zeroLiftAoaBase, deltaLift) / correctedLiftSlope;
        float stallAngleLow = zeroLiftAoA + MaxLow(correctedLiftSlope, config, zeroLiftAoaBase, deltaLift) / correctedLiftSlope;

        // air velocity relative to the surface's coordinate system.
        Vector3 airVelocity = AirVelocity(transform.InverseTransformDirection(worldAirVelocity));
        Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
        Vector3 liftDirection = LiftDirection(dragDirection, transform.forward);

        float area = config.chord * config.span;
        float dynamicPressure = DynamicPressure(airDensity, airVelocity);
        float angleOfAttack = AoA(airVelocity);

        Vector3 aerodynamicCoefficients = CalculateCoefficients(angleOfAttack, correctedLiftSlope, zeroLiftAoA,
            stallAngleHigh, stallAngleLow);
        Vector3 lift = liftDirection * (aerodynamicCoefficients.x * dynamicPressure * area);
        Vector3 drag = dragDirection * (aerodynamicCoefficients.y * dynamicPressure * area);
        Vector3 torque = -transform.forward * (aerodynamicCoefficients.z * dynamicPressure * area * config.chord);

        forceAndTorque.p += lift + drag;
        forceAndTorque.q += Vector3.Cross(relativePosition, forceAndTorque.p);
        forceAndTorque.q += torque;

#if UNITY_EDITOR
        // For gizmos drawing.
        isAtStall = !(angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow);
        currentLift = lift;
        currentDrag = drag;
        currentTorque = torque;
#endif

        return forceAndTorque;
    }

    private Vector3 CalculateCoefficients(float angleOfAttack, float correctedLiftSlope, float zeroLiftAoA, 
        float stallAngleHigh, float stallAngleLow)
    {
        Vector3 aerodynamicCoefficients;
        float paddedStallAngleHigh = PaddedStallAngleHigh(stallAngleHigh, _flapAngle);
        float paddedStallAngleLow = PaddedStallAngleLow(stallAngleLow, _flapAngle);

        if (angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow)
        {
            aerodynamicCoefficients = CalculateCoefficientsAtLowAoA(angleOfAttack, correctedLiftSlope, zeroLiftAoA);
        }
        else
        {
            if (angleOfAttack > paddedStallAngleHigh || angleOfAttack < paddedStallAngleLow)
            {
                aerodynamicCoefficients = CalculateCoefficientsAtStall(angleOfAttack, correctedLiftSlope, zeroLiftAoA, stallAngleHigh, stallAngleLow);
            }
            else
            {
                bool angleOfAttackVsStallHigh = angleOfAttack > stallAngleHigh;
                Vector3 aerodynamicCoefficientsLow = angleOfAttackVsStallHigh
                    ? CalculateCoefficientsAtLowAoA(stallAngleHigh, correctedLiftSlope, zeroLiftAoA)
                    : CalculateCoefficientsAtLowAoA(stallAngleLow, correctedLiftSlope, zeroLiftAoA);
                Vector3 aerodynamicCoefficientsStall = angleOfAttackVsStallHigh
                    ? CalculateCoefficientsAtStall(paddedStallAngleHigh, correctedLiftSlope, zeroLiftAoA,
                        stallAngleHigh, stallAngleLow)
                    : CalculateCoefficientsAtStall(paddedStallAngleLow, correctedLiftSlope, zeroLiftAoA, stallAngleHigh,
                        stallAngleLow);
                float lerpParam = angleOfAttackVsStallHigh
                    ? (angleOfAttack - stallAngleHigh) / (paddedStallAngleHigh - stallAngleHigh)
                    : (angleOfAttack - stallAngleLow) / (paddedStallAngleLow - stallAngleLow);
                
                aerodynamicCoefficients = Vector3.Lerp(aerodynamicCoefficientsLow, aerodynamicCoefficientsStall, lerpParam);
            }
        }
        
        return aerodynamicCoefficients;
    }

    private Vector3 CalculateCoefficientsAtLowAoA(float angleOfAttack, float correctedLiftSlope, float zeroLiftAoA)
    {
        float liftCoefficient = LiftCoeff(correctedLiftSlope, angleOfAttack, zeroLiftAoA);
        float effectiveAngle = angleOfAttack - zeroLiftAoA - (liftCoefficient / (pi * config.aspectRatio));
        float tangentialCoefficient = TangentialCoeff(config.skinFriction, effectiveAngle);
        float normalCoefficient = NormalCoeff(liftCoefficient, effectiveAngle, tangentialCoefficient);

        return new Vector3(liftCoefficient, 
            normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle), 
            -normalCoefficient * TorqueCoefficientProportion(effectiveAngle));
    }

    private Vector3 CalculateCoefficientsAtStall(float angleOfAttack, float correctedLiftSlope, float zeroLiftAoA, 
        float stallAngleHigh, float stallAngleLow)
    {
        float liftCoefficientLowAoA = angleOfAttack > stallAngleHigh
            ? correctedLiftSlope * (stallAngleHigh - zeroLiftAoA)
            : correctedLiftSlope * (stallAngleLow - zeroLiftAoA);
        float inducedAngle = liftCoefficientLowAoA / (pi * config.aspectRatio);
        float lerpParam = angleOfAttack > stallAngleHigh
            ? (pi / 2 - Mathf.Clamp(angleOfAttack, -pi / 2, pi / 2)) / (pi / 2 - stallAngleHigh)
            : (-pi / 2 - Mathf.Clamp(angleOfAttack, -pi / 2, pi / 2)) / (-pi / 2 - stallAngleLow);

        inducedAngle = Mathf.Lerp(0, inducedAngle, lerpParam);
        
        float effectiveAngle = angleOfAttack - zeroLiftAoA - inducedAngle;
        float normalCoefficient = FrictionAt90Degrees(_flapAngle) * Mathf.Sin(effectiveAngle) *
                                  (1 / (0.56f + 0.44f * Mathf.Abs(Mathf.Sin(effectiveAngle))) -
                                   0.41f * (1 - Mathf.Exp(-17 / config.aspectRatio)));
        float tangentialCoefficient = 0.5f * config.skinFriction * Mathf.Cos(effectiveAngle);

        return new Vector3(normalCoefficient * Mathf.Cos(effectiveAngle) - tangentialCoefficient * Mathf.Sin(effectiveAngle)
            , normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle), 
            -normalCoefficient * TorqueCoefficientProportion(effectiveAngle));
    }

    private static float CorrectedLiftSlope(AerodynamicSurface config)
    {
        return config.liftSlope * config.aspectRatio /
               (config.aspectRatio + 2 * (config.aspectRatio + 4) / (config.aspectRatio + 2));
    }

    private static float Theta(float flapFraction)
    {
        return Mathf.Acos(2 * flapFraction - 1);
    }

    private static float DeltaLift(float liftSlope, float theta, float flapAngle)
    {
        return liftSlope * (1 - (theta - Mathf.Sin(theta)) / pi) * FlapEffectivenessCorrection(flapAngle) * flapAngle;
    }

    private static float MaxHigh(float liftSlope, AerodynamicSurface config, float zeroLift, float deltaLift)
    {
        return liftSlope * (config.stallAngleHigh * deg2Rad - zeroLift) + deltaLift * LiftCoefficientMaxFraction(config.flapFraction);
    }
    
    private static float MaxLow(float liftSlope, AerodynamicSurface config, float zeroLift, float deltaLift)
    {
        return liftSlope * (config.stallAngleLow * deg2Rad - zeroLift) + deltaLift * LiftCoefficientMaxFraction(config.flapFraction);
    }

    private static Vector3 AirVelocity(Vector3 worldAirVelocity)
    {
        return new Vector3(worldAirVelocity.x, worldAirVelocity.y);
    }

    private static Vector3 LiftDirection(Vector3 drag, Vector3 forward)
    {
        return Vector3.Cross(drag, forward);
    }

    private static float DynamicPressure(float airDensity, Vector3 airVelocity)
    {
        return 0.5f * airDensity * airVelocity.sqrMagnitude;
    }

    private static float AoA(Vector3 airVelocity)
    {
        return Mathf.Atan2(airVelocity.y, -airVelocity.x);
    }

    private static float PaddedStallAngleHigh(float angleHigh, float flapAngle)
    {
        return angleHigh + deg2Rad * Mathf.Lerp(15, 5, (deg2Rad * flapAngle + 50) / 100);
    }

    private static float PaddedStallAngleLow(float angleHigh, float flapAngle)
    {
        return angleHigh - deg2Rad * Mathf.Lerp(15, 5, (-deg2Rad * flapAngle + 50) / 100);
    }

    private static float LiftCoeff(float liftSlope, float aoa, float zeroAoa)
    {
        return liftSlope * (aoa - zeroAoa);
    }
    
    private static float TangentialCoeff(float skinFriction, float angle)
    {
        return skinFriction * Mathf.Cos(angle);
    }
    
    private static float NormalCoeff(float lift, float angle, float tangential)
    {
        return (lift + Mathf.Sin(angle) * tangential) / Mathf.Cos(angle);
    }
    
    private static float TorqueCoefficientProportion(float effectiveAngle)
    {
        return 0.25f - 0.175f * (1 - 2 * Mathf.Abs(effectiveAngle) / pi);
    }

    private static float FrictionAt90Degrees(float flapAngle)
    {
        return 1.98f - 0.0426f * flapAngle * flapAngle + 0.21f * flapAngle;
    }

    private static float FlapEffectivenessCorrection(float flapAngle)
    {
        return Mathf.Lerp(0.8f, 0.4f, (flapAngle * deg2Rad - 10) / 50);
    }

    private static float LiftCoefficientMaxFraction(float flapFraction)
    {
        return Mathf.Clamp01(1 - 0.5f * (flapFraction - 0.1f) / 0.3f);
    }

#if UNITY_EDITOR
    public AerodynamicSurface GetConfig() => config;
    public float GetFlapAngle() => _flapAngle;
    public Vector3 currentLift { get; private set; }
    public Vector3 currentDrag { get; private set; }
    public Vector3 currentTorque { get; private set; }
    public bool isAtStall { get; private set; }
#endif
}
