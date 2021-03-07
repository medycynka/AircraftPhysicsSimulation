using System.Collections.Generic;
using UnityEngine;


public class PhysicsManager : MonoBehaviour
{
    [Range(1, 25)] public float thrust = 5;
    [Range(0, 5)] public float airDensity = 1.2f;
    public List<AerodynamicSurfaceManager> aerodynamicSurfaces;

    private Rigidbody _rb;
    private float _thrust;
    private float _thrustPercent;
    private PowerTorqueVector3 _currentForceAndTorque;
    private const float PredictionTimeStepFraction = 0.5f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _thrust = thrust * 1000f;
    }

    private void FixedUpdate()
    {
        HandleCalculations();
    }

    private void HandleCalculations()
    {
        PowerTorqueVector3 forceAndTorqueThisFrame =
            CalculateAerodynamicForces(_rb.velocity, _rb.angularVelocity, Vector3.zero, airDensity, _rb.worldCenterOfMass);
        Vector3 velocityPrediction = PredictVelocity(forceAndTorqueThisFrame.p +
                                                     transform.forward * (_thrust * _thrustPercent) +
                                                     Physics.gravity * _rb.mass);
        Vector3 angularVelocityPrediction = PredictAngularVelocity(forceAndTorqueThisFrame.q);
        PowerTorqueVector3 forceAndTorquePrediction = CalculateAerodynamicForces(velocityPrediction,
            angularVelocityPrediction, Vector3.zero, airDensity, _rb.worldCenterOfMass);

        _currentForceAndTorque = (forceAndTorqueThisFrame + forceAndTorquePrediction) * 0.5f;
        _rb.AddForce(_currentForceAndTorque.p);
        _rb.AddTorque(_currentForceAndTorque.q);
        _rb.AddForce(transform.forward * (_thrust * _thrustPercent));
    }
    
    public void SetThrustPercent(float percent)
    {
        _thrustPercent = percent;
    }

    private PowerTorqueVector3 CalculateAerodynamicForces(Vector3 velocity, Vector3 angularVelocity, Vector3 wind, 
        float airDensity, Vector3 centerOfMass)
    {
        PowerTorqueVector3 forceAndTorque = new PowerTorqueVector3();

        for (var i = 0; i < aerodynamicSurfaces.Count; i++)
        {
            Vector3 relativePosition = aerodynamicSurfaces[i].transform.position - centerOfMass;
            forceAndTorque += aerodynamicSurfaces[i]
                .CalculateForces(-velocity + wind - Vector3.Cross(angularVelocity, relativePosition), 
                    airDensity, relativePosition);
        }

        return forceAndTorque;
    }

    private Vector3 PredictVelocity(Vector3 force)
    {
        return _rb.velocity + Time.fixedDeltaTime * PredictionTimeStepFraction * force / _rb.mass;
    }

    private Vector3 PredictAngularVelocity(Vector3 torque)
    {
        Quaternion inertiaTensorWorldRotation = _rb.rotation * _rb.inertiaTensorRotation;
        Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * torque;
        Vector3 angularVelocityChangeInDiagonalSpace = new Vector3(torqueInDiagonalSpace.x / _rb.inertiaTensor.x,
            torqueInDiagonalSpace.y / _rb.inertiaTensor.y, torqueInDiagonalSpace.z / _rb.inertiaTensor.z);

        return _rb.angularVelocity + Time.fixedDeltaTime * PredictionTimeStepFraction
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
            forceAndTorque =
                CalculateAerodynamicForces(-displayAirVelocity, Vector3.zero, Vector3.zero, displayAirDensity, com);
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


