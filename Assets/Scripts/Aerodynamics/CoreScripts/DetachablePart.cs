using System;
using UnityEngine;


namespace Aerodynamics.CoreScripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class DetachablePart : MonoBehaviour
    {
        [Range(1f, 1000f)] public float mass = 100f;
        public GameObject detachableHolder;
        public bool isRocket;
        [Range(1f, 10f)] public float rocketSpeedMultiplier = 2f;
        public GameObject collisionEffect;
        public GameObject startEffect;

        private bool _canCollide;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = mass;
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (_canCollide)
            {
                _canCollide = false;

                if (collisionEffect != null)
                {
                    collisionEffect = Instantiate(collisionEffect, transform.position, Quaternion.identity);
                }

                gameObject.SetActive(false);
                Destroy(gameObject, 2f);
            }
        }

        public void Detach(Vector3 velocityDuringDetach)
        {
            if (detachableHolder != null)
            {
                Destroy(detachableHolder);
            }

            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.velocity = isRocket ? (velocityDuringDetach * rocketSpeedMultiplier) : velocityDuringDetach;
            _canCollide = true;
            transform.parent = null;

            if (isRocket && startEffect != null)
            {
                startEffect = Instantiate(startEffect, transform.position, Quaternion.identity, transform);
            }
        }
    }
}