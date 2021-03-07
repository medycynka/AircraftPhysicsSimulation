using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhysicsManager))]
public class AirplaneController : MonoBehaviour
{
    public List<AerodynamicSurfaceManager> controlSurfaces;
    public List<WheelCollider> wheels;
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
    private PhysicsManager _physicsManager;
    private Rigidbody _rb;

    private void Start()
    {
        _physicsManager = GetComponent<PhysicsManager>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleAwsdInput();
        HandleQeInput();
        HandleTrustAndBreaks();
        HandleFlapsInput();

        UpdateVisibleTextValue();
    }

    private void FixedUpdate()
    {
        SetControlSurfecesAngles(pitchPower, rollPower, yawPower, flapPower);
        _physicsManager.SetThrustPercent(_thrustPercent);

        for (int i = 0; i < wheels.Count; i++)
        {
            wheels[i].brakeTorque = _brakesTorque;
            // small torque to wake up wheel collider
            wheels[i].motorTorque = 0.01f;
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
            _thrustPercent = _thrustPercent > 0 ? 0 : 1f;
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
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

    private void UpdateVisibleTextValue()
    {
        displayText.text = "Velocity: " + ((int)_rb.velocity.magnitude).ToString("D3") + " m/s\n" + 
                           "Height: " + ((int)transform.position.y).ToString("D4") + " m\n" + 
                           "Trust: " + (int)(_thrustPercent * 100) + "%\n" + 
                           (_brakesTorque > 0 ? "Breaks: ON" : "Breaks: OFF");
    }

    private void SetControlSurfecesAngles(float pitch, float roll, float yaw, float flap)
    {
        for (int i = 0; i < controlSurfaces.Count; i++)
        {
            switch (controlSurfaces[i].inputType)
            {
                case ControlInputType.Pitch:
                    controlSurfaces[i].SetFlapAngle(pitch * pitchControlSensitivity * controlSurfaces[i].inputMultiplier);
                    break;
                case ControlInputType.Roll:
                    controlSurfaces[i].SetFlapAngle(roll * rollControlSensitivity * controlSurfaces[i].inputMultiplier);
                    break;
                case ControlInputType.Yaw:
                    controlSurfaces[i].SetFlapAngle(yaw * yawControlSensitivity * controlSurfaces[i].inputMultiplier);
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
