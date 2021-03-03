using UnityEngine;

public struct PowerTorqueVector3
{
    public Vector3 p;
    public Vector3 q;

    public PowerTorqueVector3(Vector3 force, Vector3 torque)
    {
        p = force;
        q = torque;
    }

    public static PowerTorqueVector3 operator+(PowerTorqueVector3 a, PowerTorqueVector3 b)
    {
        return new PowerTorqueVector3(a.p + b.p, a.q + b.q);
    }

    public static PowerTorqueVector3 operator *(float f, PowerTorqueVector3 a)
    {
        return new PowerTorqueVector3(f * a.p, f * a.q);
    }

    public static PowerTorqueVector3 operator *(PowerTorqueVector3 a, float f)
    {
        return f * a;
    }
}
