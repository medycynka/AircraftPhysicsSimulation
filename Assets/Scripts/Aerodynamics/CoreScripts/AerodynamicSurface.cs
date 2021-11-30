using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts
{
    [CreateAssetMenu(fileName = "Aerodynamic Surface Config",
        menuName = "Aerodynamics/Aerodynamic Surface Configuration")]
    public class AerodynamicSurface : ScriptableObject
    {
        public float liftSlope = 6.28f;
        public float skinFriction = 0.02f;
        public float zeroLiftAoA;
        [Range(0, 30)] public float stallAngleHigh = 15;
        [Range(-30, 0)] public float stallAngleLow = -15;
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

        public AerodynamicSurfaceForces CalculateForces(bool forceReturn, Transform surfaceTransform,
            Vector3 worldAirVelocity,
            float airDensity, Vector3 relativePosition, float currentFlapAngle)
        {
            PowerTorqueVector3 forceAndTorque = new PowerTorqueVector3();

            if (forceReturn)
            {
                return new AerodynamicSurfaceForces()
                {
                    currentPowerAndTorque = forceAndTorque,
                    isAtStall = false,
                    currentLift = Vector3.zero,
                    currentDrag = Vector3.zero,
                    currentTorque = Vector3.zero
                };
            }

            // aspect ratio effect on lift coefficient.
            float correctedLiftSlope = AerodynamicUtils.CorrectedLiftSlope(this);
            // flap deflection influence on zero lift AoA and angles at which stall happens.
            float deltaLift = AerodynamicUtils.DeltaLift(correctedLiftSlope, AerodynamicUtils.Theta(flapFraction),
                currentFlapAngle);
            float zeroLiftAoaBase = zeroLiftAoA * AerodynamicUtils.deg2Rad;
            float correctedZeroLiftAoA = zeroLiftAoaBase - deltaLift / correctedLiftSlope;
            float correctedStallAngleHigh = correctedZeroLiftAoA +
                                            AerodynamicUtils.MaxHigh(correctedLiftSlope, this, zeroLiftAoaBase,
                                                deltaLift) / correctedLiftSlope;
            float correctedStallAngleLow = correctedZeroLiftAoA +
                                           AerodynamicUtils.MaxLow(correctedLiftSlope, this, zeroLiftAoaBase,
                                               deltaLift) / correctedLiftSlope;

            // air velocity relative to the surface's coordinate system.
            Vector3 airVelocity =
                AerodynamicUtils.AirVelocity(surfaceTransform.InverseTransformDirection(worldAirVelocity));
            Vector3 dragDirection = surfaceTransform.TransformDirection(airVelocity.normalized);
            Vector3 forward = surfaceTransform.forward;
            Vector3 liftDirection = AerodynamicUtils.LiftDirection(dragDirection, forward);

            float area = chord * span;
            float dynamicPressure = AerodynamicUtils.DynamicPressure(airDensity, airVelocity);
            float angleOfAttack = AerodynamicUtils.AoA(airVelocity);

            Vector3 aerodynamicCoefficients = CalculateCoefficients(angleOfAttack, correctedLiftSlope,
                correctedZeroLiftAoA,
                correctedStallAngleHigh, correctedStallAngleLow, currentFlapAngle);
            Vector3 lift = liftDirection * (aerodynamicCoefficients.x * dynamicPressure * area);
            Vector3 drag = dragDirection * (aerodynamicCoefficients.y * dynamicPressure * area);
            Vector3 torque = -forward * (aerodynamicCoefficients.z * dynamicPressure * area * chord);

            forceAndTorque.p += lift + drag;
            forceAndTorque.q += Vector3.Cross(relativePosition, forceAndTorque.p);
            forceAndTorque.q += torque;

            return new AerodynamicSurfaceForces()
            {
                currentPowerAndTorque = forceAndTorque,
                isAtStall = !(angleOfAttack < correctedStallAngleHigh && angleOfAttack > correctedStallAngleLow),
                currentLift = lift,
                currentDrag = drag,
                currentTorque = torque
            };
        }

        private Vector3 CalculateCoefficients(float angleOfAttack, float correctedLiftSlope, float correctedZeroLiftAoA,
            float correctedStallAngleHigh, float correctedStallAngleLow, float currentFlapAngle)
        {
            Vector3 aerodynamicCoefficients;
            float paddedStallAngleHigh =
                AerodynamicUtils.PaddedStallAngleHigh(correctedStallAngleHigh, currentFlapAngle);
            float paddedStallAngleLow = AerodynamicUtils.PaddedStallAngleLow(correctedStallAngleLow, currentFlapAngle);

            if (angleOfAttack < correctedStallAngleHigh && angleOfAttack > correctedStallAngleLow)
            {
                aerodynamicCoefficients =
                    CalculateCoefficientsAtLowAoA(angleOfAttack, correctedLiftSlope, correctedZeroLiftAoA);
            }
            else
            {
                if (angleOfAttack > paddedStallAngleHigh || angleOfAttack < paddedStallAngleLow)
                {
                    aerodynamicCoefficients = CalculateCoefficientsAtStall(angleOfAttack, correctedLiftSlope,
                        correctedZeroLiftAoA, correctedStallAngleHigh, correctedStallAngleLow, currentFlapAngle);
                }
                else
                {
                    bool angleOfAttackVsStallHigh = angleOfAttack > correctedStallAngleHigh;
                    Vector3 aerodynamicCoefficientsLow = angleOfAttackVsStallHigh
                        ? CalculateCoefficientsAtLowAoA(correctedStallAngleHigh, correctedLiftSlope,
                            correctedZeroLiftAoA)
                        : CalculateCoefficientsAtLowAoA(correctedStallAngleLow, correctedLiftSlope,
                            correctedZeroLiftAoA);
                    Vector3 aerodynamicCoefficientsStall = angleOfAttackVsStallHigh
                        ? CalculateCoefficientsAtStall(paddedStallAngleHigh, correctedLiftSlope, correctedZeroLiftAoA,
                            correctedStallAngleHigh, correctedStallAngleLow, currentFlapAngle)
                        : CalculateCoefficientsAtStall(paddedStallAngleLow, correctedLiftSlope, correctedZeroLiftAoA,
                            correctedStallAngleHigh,
                            correctedStallAngleLow, currentFlapAngle);
                    float lerpParam = angleOfAttackVsStallHigh
                        ? (angleOfAttack - correctedStallAngleHigh) / (paddedStallAngleHigh - correctedStallAngleHigh)
                        : (angleOfAttack - correctedStallAngleLow) / (paddedStallAngleLow - correctedStallAngleLow);

                    aerodynamicCoefficients =
                        Vector3.Lerp(aerodynamicCoefficientsLow, aerodynamicCoefficientsStall, lerpParam);
                }
            }

            return aerodynamicCoefficients;
        }

        private Vector3 CalculateCoefficientsAtLowAoA(float angleOfAttack, float correctedLiftSlope,
            float correctedZeroLiftAoA)
        {
            float liftCoefficient = AerodynamicUtils.LiftC(correctedLiftSlope, angleOfAttack, correctedZeroLiftAoA);
            float effectiveAngle = angleOfAttack - correctedZeroLiftAoA -
                                   (liftCoefficient / (AerodynamicUtils.pi * aspectRatio));
            float tangentialCoefficient = AerodynamicUtils.TangentialC(skinFriction, effectiveAngle);
            float normalCoefficient = AerodynamicUtils.NormalC(liftCoefficient, effectiveAngle, tangentialCoefficient);

            return AerodynamicUtils.CoefficientAtLowAoA(liftCoefficient, normalCoefficient, effectiveAngle,
                tangentialCoefficient);
        }

        private Vector3 CalculateCoefficientsAtStall(float angleOfAttack, float correctedLiftSlope,
            float correctedZeroLiftAoA,
            float correctedStallAngleHigh, float correctedStallAngleLow, float currentFlapAngle)
        {
            float liftCoefficientLowAoA = angleOfAttack > correctedStallAngleHigh
                ? correctedLiftSlope * (correctedStallAngleHigh - correctedZeroLiftAoA)
                : correctedLiftSlope * (correctedStallAngleLow - correctedZeroLiftAoA);
            float inducedAngle = liftCoefficientLowAoA / (AerodynamicUtils.pi * aspectRatio);
            float lerpParam = angleOfAttack > correctedStallAngleHigh
                ? AerodynamicUtils.MaxAngleOfAttackLerpValue(angleOfAttack, correctedStallAngleHigh)
                : AerodynamicUtils.MinAngleOfAttackLerpValue(angleOfAttack, correctedStallAngleLow);

            inducedAngle = Mathf.Lerp(0, inducedAngle, lerpParam);

            float effectiveAngle = angleOfAttack - correctedZeroLiftAoA - inducedAngle;
            float normalCoefficient = AerodynamicUtils.NormalFrictionC(currentFlapAngle, effectiveAngle, aspectRatio);
            float tangentialCoefficient = 0.5f * AerodynamicUtils.TangentialC(skinFriction, effectiveAngle);

            return AerodynamicUtils.CoefficientAtStall(normalCoefficient, effectiveAngle, tangentialCoefficient);
        }
    }

    [Serializable]
    public struct AerodynamicSurfaceForces
    {
        public PowerTorqueVector3 currentPowerAndTorque;
        public bool isAtStall;
        public Vector3 currentLift;
        public Vector3 currentDrag;
        public Vector3 currentTorque;
    }
}