using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts
{
    public class AerodynamicSurfaceManager : MonoBehaviour
    {
        public AerodynamicSurface config;
        public bool isControlSurface;
        public ControlInputType inputType;
        public float inputMultiplier = 1;

        private float _flapAngle; // need it here because it wasn't possible to edit this in SO at runtime
        private Transform surfaceTransform;

        private void OnValidate()
        {
            if (!surfaceTransform)
            {
                surfaceTransform = GetComponent<Transform>();
            }
        }

        private void Awake()
        {
            if (!surfaceTransform)
            {
                surfaceTransform = GetComponent<Transform>();
            }
        }

        public void SetFlapAngle(float angle)
        {
            _flapAngle = Mathf.Clamp(angle, -AerodynamicUtils.deg2Rad50, AerodynamicUtils.deg2Rad50);
        }

        public PowerTorqueVector3 CalculateForces(Vector3 worldAirVelocity, float airDensity, Vector3 relativePosition)
        {
            bool isSurfaceNotActive = !gameObject.activeInHierarchy || !config;
            var forces = config.CalculateForces(isSurfaceNotActive, surfaceTransform, worldAirVelocity, airDensity,
                relativePosition, _flapAngle);

            if (isSurfaceNotActive)
            {
                return forces.currentPowerAndTorque;
            }

#if UNITY_EDITOR
            isAtStall = forces.isAtStall;
            currentLift = forces.currentLift;
            currentDrag = forces.currentDrag;
            currentTorque = forces.currentTorque;
#endif

            // return forceAndTorque;
            return forces.currentPowerAndTorque;
        }

#if UNITY_EDITOR
        public AerodynamicSurface GetConfig() => config;
        public float GetFlapAngle() => _flapAngle * inputMultiplier;
        public Vector3 currentLift { get; private set; }
        public Vector3 currentDrag { get; private set; }
        public Vector3 currentTorque { get; private set; }
        public bool isAtStall { get; private set; }
#endif
    }
}
