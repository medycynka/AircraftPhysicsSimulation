using System.Collections.Generic;
using Aerodynamics.CoreScripts.EnvironmentUtilities;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts
{
    public class PhysicsManager : MonoBehaviour
    {
        [Range(1, 250)] public float thrust = 5;
        [Range(0, 5)] public float airDensity = 1.2f;
        public float currentTemperature = 20f;
        public Vector3 windVector = Vector3.zero;
        public List<AerodynamicSurfaceManager> aerodynamicSurfaces;

        public Rigidbody rb => _rb;
        
        private Rigidbody _rb;
        private Transform _airplaneTransform;
        private float _thrust;
        private float _thrustPercent;
        private PowerTorqueVector3 _currentForceAndTorque;
        private const float predictionTimeStepFraction = 0.5f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _thrust = thrust * 1000f;
            _airplaneTransform = transform;

            /*for (int i = 0; i < 120; i++)
            {
                AirRegion newSo = ScriptableObject.CreateInstance<AirRegion>();
                newSo.name = $"AirRegion_{i * 100}-{(i + 1) * 100}";
                newSo.altitude = i * 100;
                newSo.temperature = 20.0f - i * 0.6f;
                AssetDatabase.CreateAsset(newSo, $"Assets/Misc/Config/AerodynamicsConfigu/EnvironmentsConfig/DefaultsAirRegions/{newSo.name}.asset");
            }*/
        }

        private void HandleCalculations(float delta)
        {
            PowerTorqueVector3 forceAndTorqueThisFrame = CalculateAerodynamicForces(_rb.velocity, _rb.angularVelocity,
                windVector, airDensity, _rb.worldCenterOfMass);
            forceAndTorqueThisFrame.p +=
                _airplaneTransform.forward * (_thrust * _thrustPercent * 0.5f) + Physics.gravity * _rb.mass;

            Vector3 velocityPrediction = PredictVelocity(forceAndTorqueThisFrame.p, delta);
            Vector3 angularVelocityPrediction = PredictAngularVelocity(forceAndTorqueThisFrame.q, delta);
            PowerTorqueVector3 forceAndTorquePrediction = CalculateAerodynamicForces(velocityPrediction,
                angularVelocityPrediction, windVector, airDensity, _rb.worldCenterOfMass);

            _currentForceAndTorque = (forceAndTorqueThisFrame + forceAndTorquePrediction) * 0.5f;
            _rb.AddForce(_currentForceAndTorque.p);
            _rb.AddTorque(_currentForceAndTorque.q);
            _rb.AddForce(_airplaneTransform.forward * (_thrust * _thrustPercent * 0.5f));
        }

        public void HandleFixedUpdate(float delta)
        {
            HandleCalculations(delta);
        }
        
        public void SetThrustPercent(float percent)
        {
            _thrustPercent = percent;
        }

        private PowerTorqueVector3 CalculateAerodynamicForces(Vector3 velocity, Vector3 angularVelocity, Vector3 wind,
            float currentAirDensity, Vector3 centerOfMass)
        {
            PowerTorqueVector3 forceAndTorque = new PowerTorqueVector3();

            for (var i = 0; i < aerodynamicSurfaces.Count; i++)
            {
                Vector3 relativePosition = aerodynamicSurfaces[i].transform.position - centerOfMass;
                forceAndTorque += aerodynamicSurfaces[i].CalculateForces(
                    -velocity + wind - Vector3.Cross(angularVelocity, relativePosition),
                    currentAirDensity, relativePosition);
            }

            return forceAndTorque;
        }

        private Vector3 PredictVelocity(Vector3 force, float delta)
        {
            return _rb.velocity + delta * predictionTimeStepFraction * force / _rb.mass;
        }

        private Vector3 PredictAngularVelocity(Vector3 torque, float delta)
        {
            Quaternion inertiaTensorWorldRotation = _rb.rotation * _rb.inertiaTensorRotation;
            Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * torque;
            Vector3 inertiaTensor = _rb.inertiaTensor;
            Vector3 angularVelocityChangeInDiagonalSpace = new Vector3(torqueInDiagonalSpace.x / inertiaTensor.x,
                torqueInDiagonalSpace.y / inertiaTensor.y, torqueInDiagonalSpace.z / inertiaTensor.z);

            return _rb.angularVelocity + delta * predictionTimeStepFraction
                                               * (inertiaTensorWorldRotation * angularVelocityChangeInDiagonalSpace);
        }

#if UNITY_EDITOR
        // For gizmos drawing.
        public void CalculateCenterOfLift(out Vector3 center, out Vector3 force, Vector3 displayAirVelocity,
            float displayAirDensity)
        {
            Vector3 com;
            PowerTorqueVector3 forceAndTorque;

            if (aerodynamicSurfaces == null)
            {
                center = Vector3.zero;
                force = Vector3.zero;

                return;
            }

            if (_rb == null)
            {
                com = GetComponent<Rigidbody>().worldCenterOfMass;
                forceAndTorque = CalculateAerodynamicForces(-displayAirVelocity, Vector3.zero,
                    windVector, displayAirDensity, com);
            }
            else
            {
                com = _rb.worldCenterOfMass;
                forceAndTorque = _currentForceAndTorque;
            }

            force = forceAndTorque.p;
            center = com + Vector3.Cross(forceAndTorque.p, forceAndTorque.q) / forceAndTorque.p.sqrMagnitude;
        }
#endif
    }
}


