using System;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public class AirRegionManager : MonoBehaviour
    {
        public AtmosphericRegion atmosphericRegion;
        public Transform regionTransform;

        private BoxCollider _collider;
        private string _airPlaneTag = "Player";
        private bool _useAirRegion;
        private bool _useWindRegion;
        private PhysicsManager _physicsManager;
        private Vector3 _center;

        private void OnValidate()
        {
            if (regionTransform == null)
            {
                regionTransform = transform;
            }

            _center = regionTransform.position;
        }

        private void Awake()
        {
            _center = regionTransform.position;
            
            if (atmosphericRegion != null && (atmosphericRegion.airRegion != null || atmosphericRegion.windRegionVectorField != null))
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                _collider.isTrigger = true;
                _collider.center = _center;
                _collider.size = atmosphericRegion.size;
            }

            _useAirRegion = (atmosphericRegion != null && atmosphericRegion.airRegion);
            _useWindRegion = (atmosphericRegion != null && atmosphericRegion.windRegionVectorField);

            if (_useWindRegion)
            {
                atmosphericRegion.windRegionVectorField.ForceInit(regionTransform.position);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_useAirRegion || _useWindRegion)
            {
                if (other.CompareTag(_airPlaneTag))
                {
                    if (_physicsManager == null)
                    {
                        _physicsManager = other.TryGetComponent(out PhysicsManager pm)
                            ? pm
                            : other.GetComponentInParent<PhysicsManager>();

                        if (_physicsManager == null)
                        {
                            _physicsManager = other.GetComponentInChildren<PhysicsManager>();
                        }

                        if (_useAirRegion)
                        {
                            _physicsManager.airDensity = atmosphericRegion.GetAirDensity();
                        }

                        if (_useWindRegion)
                        {
                            _physicsManager.windVector = atmosphericRegion.GetWind(other.transform.position);
                        }
                    }
                }
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(_airPlaneTag) && _physicsManager != null)
            {
                _physicsManager.windVector = atmosphericRegion.GetWind(other.transform.position);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                _physicsManager.windVector = Vector3.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (atmosphericRegion)
            {
                atmosphericRegion.DrawRegionBoundaries(_center);
                atmosphericRegion.DrawRegionWindDirection();
            }
        }
    }
}