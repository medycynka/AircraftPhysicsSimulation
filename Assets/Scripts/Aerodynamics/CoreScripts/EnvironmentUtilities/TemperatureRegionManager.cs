using System;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public class TemperatureRegionManager : MonoBehaviour
    {
        public AirRegion airRegion;
        public Transform regionTransform;

        private BoxCollider _collider;
        private string _airPlaneTag = "Player";
        private bool _isInside;
        private bool _insideReset = true;
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
            
            if (airRegion != null && _collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                _collider.isTrigger = true;
                _collider.center = _center;
            }
        }

        private void OnTriggerEnter(Collider other)
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
                }
                
                _physicsManager.airDensity = airRegion.CalculateAirDensity();
                _physicsManager.currentTemperature = airRegion.temperature;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                // _physicsManager = null;
                _isInside = false;
                _insideReset = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (airRegion)
            {
                if (_collider)
                {
                    Gizmos.color = airRegion.regionSidesColor;
                    Gizmos.DrawCube(regionTransform.position, _collider.size);
                    Gizmos.color = airRegion.regionBorderColor;
                    Gizmos.DrawWireCube(regionTransform.position, _collider.size);
                }
                else
                {
                    _collider = GetComponent<BoxCollider>();
                }
            }
        }
    }
}