﻿using System;
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

        // Accounting for aspect ratio effect on lift coefficient.
        float correctedLiftSlope = config.liftSlope * config.aspectRatio / (config.aspectRatio + 2 * (config.aspectRatio + 4) / (config.aspectRatio + 2));

        // Calculating flap deflection influence on zero lift angle of attack
        // and angles at which stall happens.
        float theta = Mathf.Acos(2 * config.flapFraction - 1);
        float deltaLift = correctedLiftSlope * (1 - (theta - Mathf.Sin(theta)) / pi) * FlapEffectivenessCorrection(_flapAngle) * _flapAngle;
        float zeroLiftAoaBase = config.zeroLiftAoA * deg2Rad;
        float zeroLiftAoA = zeroLiftAoaBase - deltaLift / correctedLiftSlope;
        float clMaxHigh = correctedLiftSlope * (config.stallAngleHigh * deg2Rad - zeroLiftAoaBase) + deltaLift * LiftCoefficientMaxFraction(config.flapFraction);
        float clMaxLow = correctedLiftSlope * (config.stallAngleLow * deg2Rad - zeroLiftAoaBase) + deltaLift * LiftCoefficientMaxFraction(config.flapFraction);
        float stallAngleHigh = zeroLiftAoA + clMaxHigh / correctedLiftSlope;
        float stallAngleLow = zeroLiftAoA + clMaxLow / correctedLiftSlope;

        // Calculating air velocity relative to the surface's coordinate system.
        // Z component of the velocity is discarded.
        Vector3 airVelocity = transform.InverseTransformDirection(worldAirVelocity);
        airVelocity = new Vector3(airVelocity.x, airVelocity.y);

        Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
        Vector3 liftDirection = Vector3.Cross(dragDirection, transform.forward);

        float area = config.chord * config.span;
        float dynamicPressure = 0.5f * airDensity * airVelocity.sqrMagnitude;
        float angleOfAttack = Mathf.Atan2(airVelocity.y, -airVelocity.x);

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
        IsAtStall = !(angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow);
        CurrentLift = lift;
        CurrentDrag = drag;
        CurrentTorque = torque;
#endif

        return forceAndTorque;
    }

    private Vector3 CalculateCoefficients(float angleOfAttack, float correctedLiftSlope, float zeroLiftAoA, 
        float stallAngleHigh, float stallAngleLow)
    {
        Vector3 aerodynamicCoefficients;
        // Low angles of attack mode and stall mode curves are stitched together by a line segment. 
        float paddedStallAngleHigh = stallAngleHigh + deg2Rad * Mathf.Lerp(15, 5, (deg2Rad * _flapAngle + 50) / 100);
        float paddedStallAngleLow = stallAngleLow - deg2Rad * Mathf.Lerp(15, 5, (-deg2Rad * _flapAngle + 50) / 100);

        if (angleOfAttack < stallAngleHigh && angleOfAttack > stallAngleLow)
        {
            // Low angle of attack mode.
            aerodynamicCoefficients = CalculateCoefficientsAtLowAoA(angleOfAttack, correctedLiftSlope, zeroLiftAoA);
        }
        else
        {
            if (angleOfAttack > paddedStallAngleHigh || angleOfAttack < paddedStallAngleLow)
            {
                // Stall mode.
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
        float liftCoefficient = correctedLiftSlope * (angleOfAttack - zeroLiftAoA);
        float inducedAngle = liftCoefficient / (pi * config.aspectRatio);
        float effectiveAngle = angleOfAttack - zeroLiftAoA - inducedAngle;
        float tangentialCoefficient = config.skinFriction * Mathf.Cos(effectiveAngle);
        float normalCoefficient = (liftCoefficient + Mathf.Sin(effectiveAngle) * tangentialCoefficient) / Mathf.Cos(effectiveAngle);

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
        float normalCoefficient = frictionAt90Degrees * Mathf.Sin(effectiveAngle) *
                                  (1 / (0.56f + 0.44f * Mathf.Abs(Mathf.Sin(effectiveAngle))) -
                                   0.41f * (1 - Mathf.Exp(-17 / config.aspectRatio)));
        float tangentialCoefficient = 0.5f * config.skinFriction * Mathf.Cos(effectiveAngle);

        return new Vector3(normalCoefficient * Mathf.Cos(effectiveAngle) - tangentialCoefficient * Mathf.Sin(effectiveAngle)
            , normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle), 
            -normalCoefficient * TorqueCoefficientProportion(effectiveAngle));
    }

    private static float TorqueCoefficientProportion(float effectiveAngle) => 0.25f - 0.175f * (1 - 2 * Mathf.Abs(effectiveAngle) / pi);

    private float frictionAt90Degrees => 1.98f - 0.0426f * _flapAngle * _flapAngle + 0.21f * _flapAngle;

    private static float FlapEffectivenessCorrection(float flapAngle) => Mathf.Lerp(0.8f, 0.4f, (flapAngle * deg2Rad - 10) / 50);

    private static float LiftCoefficientMaxFraction(float flapFraction) => Mathf.Clamp01(1 - 0.5f * (flapFraction - 0.1f) / 0.3f);

#if UNITY_EDITOR
    // For gizmos drawing.
    public AerodynamicSurface Config => config;
    public float GetFlapAngle() => _flapAngle;
    public Vector3 CurrentLift { get; private set; }
    public Vector3 CurrentDrag { get; private set; }
    public Vector3 CurrentTorque { get; private set; }
    public bool IsAtStall { get; private set; }
#endif
}
