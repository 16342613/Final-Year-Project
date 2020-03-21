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
    public static float GetVectorInternalAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        double num = (b.x - a.x) * (c.x - a.x) + (b.y - a.y) * (c.y - a.y) + (b.z - a.z) * (c.z - a.z);

        double den = Math.Sqrt(Math.Pow((b.x - a.x), 2f) + Math.Pow((b.y - a.y), 2f) + Math.Pow((b.z - a.z), 2f)) *
                     Math.Sqrt(Math.Pow((c.x - a.x), 2f) + Math.Pow((c.y - a.y), 2f) + Math.Pow((c.z - a.z), 2f));

        double angle = Math.Acos(num / den) * (180f / Math.PI);

        return (float) angle;
    }


    public static Vector3 ApplyTransformationPoint(Vector3 toTransform, Matrix4x4 trsMatrix)
    {
        Vector3 vector3;
        vector3.x = (trsMatrix[0,0] * toTransform.x + trsMatrix[0, 1] * toTransform.y + trsMatrix[0, 2] * toTransform.z) + trsMatrix[0, 3];
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
        return ((float)Math.PI / (float) 180) * degrees;
    }

    public static Vector3 ConvertDegreesToRadians(Vector3 degrees)
    {
        return new Vector3(ConvertDegreesToRadians(degrees[0]), ConvertDegreesToRadians(degrees[1]), ConvertDegreesToRadians(degrees[2]));
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

    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Cross(forward, right);
        var m00 = right.x;
        var m01 = right.y;
        var m02 = right.z;
        var m10 = up.x;
        var m11 = up.y;
        var m12 = up.z;
        var m20 = forward.x;
        var m21 = forward.y;
        var m22 = forward.z;


        float num8 = (m00 + m11) + m22;
        var quaternion = new Quaternion();
        if (num8 > 0f)
        {
            var num = (float)System.Math.Sqrt(num8 + 1f);
            quaternion.w = num * 0.5f;
            num = 0.5f / num;
            quaternion.x = (m12 - m21) * num;
            quaternion.y = (m20 - m02) * num;
            quaternion.z = (m01 - m10) * num;
            return quaternion;
        }
        if ((m00 >= m11) && (m00 >= m22))
        {
            var num7 = (float)System.Math.Sqrt(((1f + m00) - m11) - m22);
            var num4 = 0.5f / num7;
            quaternion.x = 0.5f * num7;
            quaternion.y = (m01 + m10) * num4;
            quaternion.z = (m02 + m20) * num4;
            quaternion.w = (m12 - m21) * num4;
            return quaternion;
        }
        if (m11 > m22)
        {
            var num6 = (float)System.Math.Sqrt(((1f + m11) - m00) - m22);
            var num3 = 0.5f / num6;
            quaternion.x = (m10 + m01) * num3;
            quaternion.y = 0.5f * num6;
            quaternion.z = (m21 + m12) * num3;
            quaternion.w = (m20 - m02) * num3;
            return quaternion;
        }
        var num5 = (float)System.Math.Sqrt(((1f + m22) - m00) - m11);
        var num2 = 0.5f / num5;
        quaternion.x = (m20 + m02) * num2;
        quaternion.y = (m21 + m12) * num2;
        quaternion.z = 0.5f * num5;
        quaternion.w = (m01 - m10) * num2;

        return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }

}


