using System;
using UnityEditor;
using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    [RequireComponent(typeof(BoxCollider))]
    public class AirRegionManager : MonoBehaviour
    {
        public AirRegion airRegion;
        public Transform regionTransform;
        public BoxCollider collider;
        
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

            if (airRegion != null && collider == null)
            {
                collider = GetComponent<BoxCollider>();
                collider.size = airRegion.size;
            }
        }

        private void Awake()
        {
            if (airRegion != null)
            {
                collider = GetComponent<BoxCollider>();
                collider.isTrigger = true;
            }
            _center = regionTransform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                _isInside = true;

                if (_insideReset)
                {
                    if (_physicsManager == null)
                    {
                        _physicsManager = other.TryGetComponent(out PhysicsManager pm) ? pm : other.GetComponentInParent<PhysicsManager>();

                        if (_physicsManager == null)
                        {
                            _physicsManager = other.GetComponentInChildren<PhysicsManager>();
                        }

                        _physicsManager.airDensity = airRegion.CalculateAirDensity();
                    }
                }
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                if (_isInside && _insideReset)
                {
                    _insideReset = false;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                _physicsManager = null;
                _isInside = false;
                _insideReset = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (airRegion)
            {
                airRegion.DrawRegionBoundaries(_center);
            }
        }
    }
}