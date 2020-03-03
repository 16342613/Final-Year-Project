using System;
using UnityEngine;

public class MathFunctions
{
    public MathFunctions()
    {

    }

    /// <summary>
    /// 
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

}
