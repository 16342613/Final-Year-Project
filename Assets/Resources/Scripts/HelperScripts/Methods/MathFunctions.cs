using System;
using UnityEngine;

public class MathFunctions
{
    public MathFunctions()
    {
        // Empty Constructor
    }

    /// <summary>
    /// Gets the internal angle 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b">The point in the middle i.e. the corner</param>
    /// <param name="c"></param>
    /// <returns></returns>
    /// REFERENCE POINT 4
    public static float GetVectorInternalAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        double num = (b.x - a.x) * (c.x - a.x) + (b.y - a.y) * (c.y - a.y) + (b.z - a.z) * (c.z - a.z);

        double den = Math.Sqrt(Math.Pow((b.x - a.x), 2f) + Math.Pow((b.y - a.y), 2f) + Math.Pow((b.z - a.z), 2f)) *
                     Math.Sqrt(Math.Pow((c.x - a.x), 2f) + Math.Pow((c.y - a.y), 2f) + Math.Pow((c.z - a.z), 2f));

        double angle = Math.Acos(num / den) * (180f / Math.PI);

        return (float)angle;
    }

    public static Vector3 ApplyTransformationPoint(Vector3 toTransform, Matrix4x4 trsMatrix)
    {
        Vector3 vector3;
        vector3.x = (trsMatrix[0, 0] * toTransform.x + trsMatrix[0, 1] * toTransform.y + trsMatrix[0, 2] * toTransform.z) + trsMatrix[0, 3];
        vector3.y = (trsMatrix[1, 0] * toTransform.x + trsMatrix[1, 1] * toTransform.y + trsMatrix[1, 2] * toTransform.z) + trsMatrix[1, 3];
        vector3.z = (trsMatrix[2, 0] * toTransform.x + trsMatrix[2, 1] * toTransform.y + trsMatrix[2, 2] * toTransform.z) + trsMatrix[2, 3];
        float num = 1f / ((trsMatrix[3, 0] * toTransform.x + trsMatrix[3, 1] * toTransform.y + trsMatrix[3, 2] * toTransform.z) + trsMatrix[3, 3]);
        vector3.x *= num;
        vector3.y *= num;
        vector3.z *= num;

        return vector3;
    }

    public static Vector3 ApplyTransformationDirection(Vector3 toTransform, Quaternion rotationMatrix)
    {
        Vector3 t = 2 * Vector3.Cross(new Vector3(rotationMatrix.x, rotationMatrix.y, rotationMatrix.z), toTransform);

        Vector3 rotated = toTransform + rotationMatrix.w * t + Vector3.Cross(new Vector3(rotationMatrix.x, rotationMatrix.y, rotationMatrix.z), t);

        return rotated;
    }

    public static float ConvertDegreesToRadians(float degrees)
    {
        return ((float)Math.PI / (float)180) * degrees;
    }

    public static Vector3 ConvertDegreesToRadians(Vector3 degrees)
    {
        return new Vector3(ConvertDegreesToRadians(degrees[0]), ConvertDegreesToRadians(degrees[1]), ConvertDegreesToRadians(degrees[2]));
    }

    public static float ConvertRadiansToDegrees(float radians)
    {
        return ((float)180 / (float)Math.PI) * radians;
    }

    public static Vector3 ConvertRadiansToDegrees(Vector3 radians)
    {
        return new Vector3(ConvertRadiansToDegrees(radians[0]), ConvertRadiansToDegrees(radians[1]), ConvertRadiansToDegrees(radians[2]));
    }

    public static Matrix4x4 GetTranslationMatrix(Vector3 position)
    {
        return new Matrix4x4(new Vector4(1, 0, 0, 0),
                             new Vector4(0, 1, 0, 0),
                             new Vector4(0, 0, 1, 0),
                             new Vector4(position.x, position.y, position.z, 1));
    }

    public static Matrix4x4 GetRotationMatrix(Vector3 anglesRad)
    {
        Matrix4x4 rotationX = new Matrix4x4(new Vector4(1, 0, 0, 0),
                                            new Vector4(0, Mathf.Cos(anglesRad.x), Mathf.Sin(anglesRad.x), 0),
                                            new Vector4(0, -Mathf.Sin(anglesRad.x), Mathf.Cos(anglesRad.x), 0),
                                            new Vector4(0, 0, 0, 1));

        Matrix4x4 rotationY = new Matrix4x4(new Vector4(Mathf.Cos(anglesRad.y), 0, -Mathf.Sin(anglesRad.y), 0),
                                            new Vector4(0, 1, 0, 0),
                                            new Vector4(Mathf.Sin(anglesRad.y), 0, Mathf.Cos(anglesRad.y), 0),
                                            new Vector4(0, 0, 0, 1));

        Matrix4x4 rotationZ = new Matrix4x4(new Vector4(Mathf.Cos(anglesRad.z), Mathf.Sin(anglesRad.z), 0, 0),
                                            new Vector4(-Mathf.Sin(anglesRad.z), Mathf.Cos(anglesRad.z), 0, 0),
                                            new Vector4(0, 0, 1, 0),
                                            new Vector4(0, 0, 0, 1));

        return rotationX * rotationY * rotationZ;
    }

    public static Matrix4x4 GetScaleMatrix(Vector3 scale)
    {
        return new Matrix4x4(new Vector4(scale.x, 0, 0, 0),
                             new Vector4(0, scale.y, 0, 0),
                             new Vector4(0, 0, scale.z, 0),
                             new Vector4(0, 0, 0, 1));
    }

    public static Matrix4x4 Get_TRS_Matrix(Vector3 position, Vector3 rotationAngles, Vector3 scale)
    {
        return GetTranslationMatrix(position) * GetRotationMatrix(rotationAngles) * GetScaleMatrix(scale);
    }

    public static Vector3[] Decompose_TRS_Matrix(Matrix4x4 trsm)
    {
        Vector3 position = trsm.GetColumn(3);

        // Extract new local rotation
        Quaternion rotation = Quaternion.LookRotation(
            trsm.GetColumn(2),
            trsm.GetColumn(1)
        );

        // Extract new local scale
        Vector3 scale = new Vector3(
            trsm.GetColumn(0).magnitude,
            trsm.GetColumn(1).magnitude,
            trsm.GetColumn(2).magnitude
        );

        return new Vector3[] { position, rotation.eulerAngles, scale };
    }

    // TAKEN FROM http://bediyap.com/programming/convert-quaternion-to-euler-rotations/
    public static Vector3 QuaternionToEulerAngles(Quaternion q)
    {
        return ConvertRadiansToDegrees(ThreeAxisRot(2 * (q.x * q.z + q.w * q.y),
                     q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z,
                    -2 * (q.y * q.z - q.w * q.x),
                     2 * (q.x * q.y + q.w * q.z),
                     q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z));
    }

    private static Vector3 ThreeAxisRot(double r11, double r12, double r21, double r31, double r32)
    {
        Vector3 res = new Vector3((float)Math.Asin(r21), (float)Math.Atan2(r11, r12), (float)Math.Atan2(r31, r32));

        return res;
    }

    public static float CopySign(float x, float y)
    {
        float sign = 1;

        if (y < 0) sign = -1;

        return x * sign;
    }

    public static Quaternion LookRotation(Vector3 dir, Vector3 up)
    {
        if (dir == Vector3.zero)
        {
            Debug.Log("Zero direction in MyLookRotation");
            return Quaternion.identity;
        }

        if (up != dir)
        {
            up.Normalize();
            var v = dir + up * -Vector3.Dot(up, dir);
            var q = FromToRotation(Vector3.forward, v);
            return FromToRotation(v, dir) * q;
        }
        else
        {
            return FromToRotation(Vector3.forward, dir);
        }
    }

    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        Vector3 a = Vector3.Cross(from, to);

        Quaternion quaternion = new Quaternion();
        quaternion.x = a.x;
        quaternion.y = a.y;
        quaternion.z = a.z;
        quaternion.w = (float)Math.Sqrt((Vector3.Magnitude(from) * Vector3.Magnitude(from)) * (Vector3.Magnitude(to) * Vector3.Magnitude(to))) + Vector3.Dot(from, to);

        return NormalizeQuaternion(quaternion);
    }

    public static Quaternion NormalizeQuaternion(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);

        if (mag < Mathf.Epsilon)
            return Quaternion.identity;

        return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
    }
}


