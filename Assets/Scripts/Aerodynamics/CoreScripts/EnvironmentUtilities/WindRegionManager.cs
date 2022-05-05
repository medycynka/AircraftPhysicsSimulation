using UnityEngine;


namespace Aerodynamics.CoreScripts.EnvironmentUtilities
{
    public class WindRegionManager : MonoBehaviour
    {
        public WindRegionVectorField windRegion;
        public Transform regionTransform;

        private BoxCollider _collider;
        private string _airPlaneTag = "Player";
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

            if (windRegion)
            {
                if (windRegion != null)
                {
                    windRegion.ForceInit(_center);
                }
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

                Vector3 currentPlanePos = other.transform.position;
                
                _physicsManager.windVector = windRegion.GetWindVector(currentPlanePos);
                for (int i = 0; i < _physicsManager.aerodynamicSurfacePositions.Count; i++)
                {
                    _physicsManager.aerodynamicSurfaceWindVectors[i] = windRegion.GetWindVector(currentPlanePos + _physicsManager.aerodynamicSurfacePositions[i]);
                }
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(_airPlaneTag) && _physicsManager != null)
            {
                Vector3 currentPlanePos = other.transform.position;
                
                _physicsManager.windVector = windRegion.GetWindVector(currentPlanePos);
                for (int i = 0; i < _physicsManager.aerodynamicSurfacePositions.Count; i++)
                {
                    _physicsManager.aerodynamicSurfaceWindVectors[i] = windRegion.GetWindVector(currentPlanePos + _physicsManager.aerodynamicSurfacePositions[i]);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_airPlaneTag))
            {
                _physicsManager.windVector = Vector3.zero;
                for (int i = 0; i < _physicsManager.aerodynamicSurfacePositions.Count; i++)
                {
                    _physicsManager.aerodynamicSurfaceWindVectors[i] = Vector3.zero;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (windRegion != null)
            {
                windRegion.DrawInGizmos();
            }
        }
    }
}