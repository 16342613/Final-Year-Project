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
}
