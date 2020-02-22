using System;
using UnityEngine;

public class ConversionUtils
{
    public static string GetVersionDate(bool includeSeconds=false)
    {
        DateTime now = DateTime.Now;
        string dateValue = now.ToString("yyyyMMddHHmm");
        if (includeSeconds) dateValue = now.ToString("yyyyMMddHHmm.ss.fff");
        return dateValue;
    }


    public static double GetInchFeet(float height)
    {
        // i.e.: 1.86 / 0.3048 = 6.10 ie: 6' 10"
        return height / 0.3048f;
    }


    public static string GetFeetInchesFormatted(float height)
    {
        //CENTIMETERS TO FEET
        //height is meter input
        double inchFeet = GetInchFeet(height);

        //LEFT PART BEFORE DECIMAL POINT. WHOLE FEET
        int wholeFeet = (int)inchFeet;

        double inches = GetHeightInches(height);

        if( inches == 12f )
        {
            wholeFeet++;
            inches = 0.0f;
        }

        string value = wholeFeet.ToString() + "'" + " " + inches.ToString() + "\"";
        return value;
    }


    public static string GetTotalInchesFormatted(float height)
    {
        double distance = GetInchFeet(height) * 12.0f;
        string value = ((int)distance).ToString() + "\"";
        return value;
    }


    public static double GetHeightFeet(float height)
    {
        //CENTIMETERS TO FEET
        //height is meter input
        double inchFeet = GetInchFeet(height);

        //LEFT PART BEFORE DECIMAL POINT. WHOLE FEET
        int wholeFeet = (int)inchFeet;
        return wholeFeet;
    }


    public static double GetHeightInches(float height)
    {
        double inchFeet = GetInchFeet(height);
        int wholeFeet = (int)inchFeet;

        //DECIMAL OF A FOOT TO INCHES TEST HEIGHT 181cm to see if 11''
        // (6.10-6) / 0.0833 = 1.2004801921
        // rounded = 1
        // 6' 1"
        double inches = Math.Round((inchFeet - wholeFeet) / 0.0833);
        return inches;
    }


    public static float GetMetersFromInches(float inches)
    {
        return inches / 39.37f;
    }


    public static float GetAngle2D(Vector2 a, Vector2 b, bool convertToDegrees)
    {
        return GetAngle2D(a.x, a.y, b.x, b.y, convertToDegrees);
    }

    public static float GetAngle2D(Vector3 a, Vector3 b, bool convertToDegrees)
    {
        return GetAngle2D(a.x, a.y, b.x, b.y, convertToDegrees);
    }

    public static float GetAngle2D(float pointAX, float pointAY, float pointBX, float pointBY, bool convertToDegrees)
    {
        //Debug.Log("GetAngle2D: pointAX: " + pointAX + ", pointAY: " + pointAY + ", pointBX: " + pointBX + ", pointBY: " + pointBY);
        //return angle
        float nYdiff = pointAY - pointBY;
        float nXdiff = pointAX - pointBX;
        float rad = Mathf.Atan2(nYdiff, nXdiff);

        float deg = rad * 180 / Mathf.PI;

        //this will return a true 360 value
        if (convertToDegrees) deg = ConvertDeg(deg);
        return deg;
    }

    public static float ConvertDeg(float d)
    {
        float deg = d < 0 ? 180 + (180 - Mathf.Abs(d)) : d;
        return deg;
    }

    public static int GetDistanceViaXZ(Vector3 pos_1, Vector3 pos_2)
    {
        return GetDistance(new Vector2(pos_1.x, pos_1.z), new Vector2(pos_2.x, pos_2.z));
    }

    public static int GetDistance(Vector2 pos_1, Vector2 pos_2)
    {
        float d1 = pos_2.x - pos_1.x;
        d1 = d1 < 0 ? d1 * -1 : d1;
        float d2 = pos_2.y - pos_1.y;
        d2 = d2 < 0 ? d2 * -1 : d2;
        return Mathf.FloorToInt(d1 + d2);
    }

    public static float ClampAngle(float a, float min, float max)
    {
        if (max < min) max += 360.0f;
        if (a > max) a -= 360.0f;
        if (a < min) a += 360.0f;

        if (a > max)
        {
            if (a - (max + min) * 0.5f < 180.0f)
                return max;
            else
                return min;
        }
        else
            return a;
    }


    /// <summary>
    /// Bounds is a Vector3 representing x/y/z axis rotation extents in eulers from zero.
    /// i.e.: x = 20, then the clamp would be from -20 to 20
    /// </summary>
    /// <param name="q"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static Quaternion ClampRotation(Quaternion q, Vector3 bounds)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, -bounds.x, bounds.x);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
        angleY = Mathf.Clamp(angleY, -bounds.y, bounds.y);
        q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

        float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
        angleZ = Mathf.Clamp(angleZ, -bounds.z, bounds.z);
        q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);

        return q;
    }


    public static T GetEnumForString<T>(string value) where T : struct
    {
        if ((typeof(T).IsEnum))
        {
            foreach (T eValue in Enum.GetValues(typeof(T)))
            {
                if (eValue.ToString().Equals(value)) return eValue;
            }
        }

        return default;
    }
}

