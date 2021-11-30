using UnityEngine;


namespace Aerodynamics.CoreScripts
{
    public enum ControlInputType
    {
        Pitch,
        Yaw,
        Roll,
        Flap
    }


    public static class AerodynamicUtils
    {
        public const float deg2Rad = Mathf.Deg2Rad;
        public const float deg2Rad50 = deg2Rad * 50;
        public const float pi = Mathf.PI;

        public static float CorrectedLiftSlope(AerodynamicSurface config)
        {
            return config.liftSlope * config.aspectRatio /
                   (config.aspectRatio + 2 * (config.aspectRatio + 4) / (config.aspectRatio + 2));
        }

        public static float Theta(float flapFraction)
        {
            return Mathf.Acos(2 * flapFraction - 1);
        }

        public static float DeltaLift(float liftSlope, float theta, float flapAngle)
        {
            return liftSlope * (1 - (theta - Mathf.Sin(theta)) / pi) *
                   Mathf.Lerp(0.8f, 0.4f, (flapAngle * deg2Rad - 10) / 50) *
                   flapAngle; // FlapEffectivenessCorrection(flapAngle)
        }

        public static float MaxHigh(float liftSlope, AerodynamicSurface config, float zeroLift, float deltaLift)
        {
            return liftSlope * (config.stallAngleHigh * deg2Rad - zeroLift) +
                   deltaLift * Mathf.Clamp01(1 - 0.5f * (config.flapFraction - 0.1f) /
                       0.3f); // LiftCoefficientMaxFraction(config.flapFraction)
        }

        public static float MaxLow(float liftSlope, AerodynamicSurface config, float zeroLift, float deltaLift)
        {
            return liftSlope * (config.stallAngleLow * deg2Rad - zeroLift) +
                   deltaLift * Mathf.Clamp01(1 - 0.5f * (config.flapFraction - 0.1f) /
                       0.3f); // LiftCoefficientMaxFraction(config.flapFraction)
        }

        public static Vector3 AirVelocity(Vector3 worldAirVelocity)
        {
            return new Vector3(worldAirVelocity.x, worldAirVelocity.y);
        }

        public static Vector3 LiftDirection(Vector3 drag, Vector3 forward)
        {
            return Vector3.Cross(drag, forward);
        }

        public static float DynamicPressure(float airDensity, Vector3 airVelocity)
        {
            return 0.5f * airDensity * airVelocity.sqrMagnitude;
        }

        public static float AoA(Vector3 airVelocity)
        {
            return Mathf.Atan2(airVelocity.y, -airVelocity.x);
        }

        public static float PaddedStallAngleHigh(float angleHigh, float flapAngle)
        {
            return angleHigh + deg2Rad * Mathf.Lerp(15, 5, (deg2Rad * flapAngle + 50) / 100);
        }

        public static float PaddedStallAngleLow(float angleHigh, float flapAngle)
        {
            return angleHigh - deg2Rad * Mathf.Lerp(15, 5, (-deg2Rad * flapAngle + 50) / 100);
        }

        public static float LiftC(float liftSlope, float aoa, float zeroAoa)
        {
            return liftSlope * (aoa - zeroAoa);
        }

        public static float TangentialC(float skinFriction, float angle)
        {
            return skinFriction * Mathf.Cos(angle);
        }

        public static float NormalC(float lift, float angle, float tangential)
        {
            return (lift + Mathf.Sin(angle) * tangential) / Mathf.Cos(angle);
        }

        public static float TorqueCoefficientProportion(float effectiveAngle)
        {
            return 0.25f - 0.175f * (1 - 2 * Mathf.Abs(effectiveAngle) / pi);
        }

        public static float FrictionAt90Degrees(float flapAngle)
        {
            return 1.98f - 0.0426f * flapAngle * flapAngle + 0.21f * flapAngle;
        }

        public static float NormalFrictionC(float flapAngle, float effectiveAngle, float aspectRatio)
        {
            return FrictionAt90Degrees(flapAngle) * Mathf.Sin(effectiveAngle) *
                   (1 / (0.56f + 0.44f * Mathf.Abs(Mathf.Sin(effectiveAngle))) -
                    0.41f * (1 - Mathf.Exp(-17 / aspectRatio)));
        }

        public static float MaxAngleOfAttackLerpValue(float aoa, float stallAngle)
        {
            return (pi / 2 - Mathf.Clamp(aoa, -pi / 2, pi / 2)) / (pi / 2 - stallAngle);
        }

        public static float MinAngleOfAttackLerpValue(float aoa, float stallAngle)
        {
            return (-pi / 2 - Mathf.Clamp(aoa, -pi / 2, pi / 2)) / (-pi / 2 - stallAngle);
        }

        public static Vector3 CoefficientAtLowAoA(float liftCoefficient, float normalCoefficient, float effectiveAngle,
            float tangentialCoefficient)
        {
            return new Vector3(
                liftCoefficient,
                normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle),
                -normalCoefficient * TorqueCoefficientProportion(effectiveAngle)
            );
        }

        public static Vector3 CoefficientAtStall(float normalCoefficient, float effectiveAngle,
            float tangentialCoefficient)
        {
            return new Vector3(
                normalCoefficient * Mathf.Cos(effectiveAngle) - tangentialCoefficient * Mathf.Sin(effectiveAngle),
                normalCoefficient * Mathf.Sin(effectiveAngle) + tangentialCoefficient * Mathf.Cos(effectiveAngle),
                -normalCoefficient * TorqueCoefficientProportion(effectiveAngle)
            );
        }

        // public static float FlapEffectivenessCorrection(float flapAngle)
        // {
        //     return Mathf.Lerp(0.8f, 0.4f, (flapAngle * deg2Rad - 10) / 50);
        // }
        //
        // public static float LiftCoefficientMaxFraction(float flapFraction)
        // {
        //     return Mathf.Clamp01(1 - 0.5f * (flapFraction - 0.1f) / 0.3f);
        // }
    }
}