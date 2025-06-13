using System;

internal static class FloatExtensions
{

    /// <summary>
    /// Checks if this float is between two other floats
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min">The min value.</param>
    /// <param name="max">The max value.</param>
    /// <returns>True if float is in range. Otherwise, false.</returns>
    public static bool InRange(this float value, float min, float max)
    {
        return min <= value && max >= value;
    }

    public static float Lerp(this float a, float b, float t)
    {
        // Clamp t between 0 and 1
        t = Math.Max(0.0f, Math.Min(1.0f, t));

        return a + (b - a) * t;
    }

}
