using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;


namespace Aerodynamics.CoreScripts
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PhysicsManager))]
    public class AirplaneController : MonoBehaviour
    {
        public List<AerodynamicSurfaceManager> controlSurfaces;
        public List<WheelCollider> wheels;
        public List<DetachablePart> detachableParts;
        public bool randomDetach;
        public List<Transform> rotators;
        [Range(500f, 25000f)] public float airplaneMass = 1000f;
        public float rollControlSensitivity = 0.2f;
        public float pitchControlSensitivity = 0.2f;
        public float yawControlSensitivity = 0.2f;
        [Range(-1, 1)] public float pitchPower;
        [Range(-1, 1)] public float yawPower;
        [Range(-1, 1)] public float rollPower;
        [Range(0, 1)] public float flapPower;
        [Range(0, 0.3f)] public float flapIncrementPower = 0.15f;
        public int flapMaxIncrementAmount = 3;
        public TextMeshProUGUI displayText;

        private float _thrustPercent;
        private float _brakesTorque;
        private int _incCount;
        private int _decCount;
        private float _currentMass;
        private PhysicsManager _physicsManager;
        private Rigidbody _rb;
        private Animator _anim;
        private int _areWheelsOpenId;
        private int _openWheelsId;
        private int _closeWheelsId;
        private bool _thrustChanged;
        private bool _breakChanged;

        public float mass => _currentMass;

        private void Start()
        {
            _physicsManager = GetComponent<PhysicsManager>();
            _rb = GetComponent<Rigidbody>();
            _anim = GetComponentInChildren<Animator>();
            _areWheelsOpenId = Animator.StringToHash("areWheelsOpen");
            _openWheelsId = Animator.StringToHash("OpenGear");
            _closeWheelsId = Animator.StringToHash("CloseGear");

            for (int i = 0; i < detachableParts.Count; i++)
            {
                detachableParts[i].transform.parent = transform;
            }

            _currentMass = airplaneMass;

            for (int i = 0; i < detachableParts.Count; i++)
            {
                _currentMass += detachableParts[i].mass;
            }

            airplaneMass = _currentMass;
            _rb.mass = _currentMass;
        }

        private void Update()
        {
            HandleAwsdInput();
            HandleQeInput();
            HandleTrustAndBreaks();
            HandleFlapsInput();
            HandleDetachInput();
            HandleWheelsInput();

            HandleRotations(Time.deltaTime);

            UpdateVisibleTextValue();
        }

        private void FixedUpdate()
        {
            SetControlSurfecesAngles(pitchPower, rollPower, yawPower, flapPower);

            if (_thrustChanged)
            {
                _physicsManager.SetThrustPercent(_thrustPercent);
                _thrustChanged = false;
            }

            if (Time.frameCount % 10 == 0 || _breakChanged)
            {
                for (int i = 0; i < wheels.Count; i++)
                {
                    wheels[i].brakeTorque = _brakesTorque;
                    wheels[i].motorTorque = 0.01f;
                }

                _breakChanged = false;
            }
        }

        private void HandleAwsdInput()
        {
            pitchPower = Input.GetAxis("Vertical");
            rollPower = Input.GetAxis("Horizontal");
        }

        private void HandleQeInput()
        {
            yawPower = Input.GetAxis("Yaw");
        }

        private void HandleTrustAndBreaks()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _thrustChanged = true;
                _thrustPercent = _thrustPercent > 0 ? 0 : 1f;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                _breakChanged = true;
                _brakesTorque = _brakesTorque > 0 ? 0 : 100f;
            }
        }

        private void HandleFlapsInput()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (_incCount < flapMaxIncrementAmount)
                {
                    flapPower += flapIncrementPower;

                    if (_decCount > 0)
                    {
                        _decCount--;
                    }
                    else
                    {
                        _incCount++;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                if (_decCount < flapMaxIncrementAmount)
                {
                    flapPower -= flapIncrementPower;

                    if (_incCount > 0)
                    {
                        _incCount--;
                    }
                    else
                    {
                        _decCount++;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                flapPower = 0.0f;
                _incCount = 0;
                _decCount = 0;
            }
        }

        private void HandleDetachInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) && detachableParts.Count > 0)
            {
                if (randomDetach)
                {
                    int i = Random.Range(0, detachableParts.Count);
                    _currentMass -= detachableParts[i].mass;
                    detachableParts[i].Detach(_rb.velocity);
                    detachableParts.RemoveAt(i);
                }
                else
                {
                    _currentMass -= detachableParts[0].mass;
                    detachableParts[0].Detach(_rb.velocity);
                    detachableParts.RemoveAt(0);
                }

                _rb.mass = _currentMass;
                airplaneMass = _currentMass;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.LeftControl) && detachableParts.Count > 0)
                {
                    for (int i = 0; i < detachableParts.Count; i++)
                    {
                        _currentMass -= detachableParts[i].mass;
                        detachableParts[i].Detach(_rb.velocity);
                    }

                    _rb.mass = _currentMass;
                    detachableParts.Clear();
                }

                airplaneMass = _currentMass;
            }
        }

        private void HandleWheelsInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (_anim.GetBool(_areWheelsOpenId))
                {
                    _anim.CrossFade(_closeWheelsId, 0.2f);
                    _anim.SetBool(_areWheelsOpenId, false);

                    for (int i = 0; i < wheels.Count; i++)
                    {
                        wheels[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    _anim.CrossFade(_openWheelsId, 0.2f);
                    _anim.SetBool(_areWheelsOpenId, true);

                    for (int i = 0; i < wheels.Count; i++)
                    {
                        wheels[i].gameObject.SetActive(true);
                    }
                }
            }
        }

        private void HandleRotations(float delta)
        {
            if (rotators.Count > 0 && _thrustPercent > 0)
            {
                float currentVelocity = _rb.velocity.magnitude;
                float currentRotation = (currentVelocity > 60 ? 60 : currentVelocity) * 36 * delta;

                for (int i = 0; i < rotators.Count; i++)
                {
                    rotators[i].Rotate(0, 0, currentRotation);
                }
            }
        }

        private void UpdateVisibleTextValue()
        {
            displayText.text = "Velocity: " + ((int) _rb.velocity.magnitude).ToString("D3") + " m/s\n" +
                               "Height: " + ((int) transform.position.y).ToString("D4") + " m\n" +
                               "Trust: " + (int) (_thrustPercent * 100) + "%\n" +
                               (_brakesTorque > 0 ? "Breaks: ON" : "Breaks: OFF");
        }

        private void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
        {
            for (int i = 0; i < controlSurfaces.Count; i++)
            {
                switch (controlSurfaces[i].inputType)
                {
                    case ControlInputType.Pitch:
                        controlSurfaces[i]
                            .SetFlapAngle(pitch * pitchControlSensitivity * controlSurfaces[i].inputMultiplier);
                        break;
                    case ControlInputType.Roll:
                        controlSurfaces[i]
                            .SetFlapAngle(roll * rollControlSensitivity * controlSurfaces[i].inputMultiplier);
                        break;
                    case ControlInputType.Yaw:
                        controlSurfaces[i]
                            .SetFlapAngle(yaw * yawControlSensitivity * controlSurfaces[i].inputMultiplier);
                        break;
                    case ControlInputType.Flap:
                        controlSurfaces[i].SetFlapAngle(flap * controlSurfaces[i].inputMultiplier);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                SetControlSurfecesAngles(pitchPower, rollPower, yawPower, flapPower);
            }
        }
    }
}
