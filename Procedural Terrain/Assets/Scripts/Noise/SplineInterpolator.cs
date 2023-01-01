using UnityEngine;
/// <summary>
/// Interpolates the y value of the spline for a given x value.
/// </summary>
public class SplineInterpolator {
    private float[] xValues;    // x values of the spline
    private float[] yValues;    // y values of the spline corresponding to the x values

    public SplineInterpolator(float[] xValues, float[] yValues) {
        this.xValues = xValues;
        this.yValues = yValues;
    }

    public float Interpolate(float x) {
        int index = 0;
        try {
            // Find the index of the first x value greater than the given x value
            while (xValues[index] < x) {
                index++;
            }

            if (index == 0) {
                return yValues[0];
            }
            if (index >= xValues.Length) {
                return yValues[yValues.Length - 1];
            }

            // Interpolate between the two points
            float x1 = xValues[index - 1];
            float x2 = xValues[index];
            float y1 = yValues[index - 1];
            float y2 = yValues[index];
            float y = y1 + (y2 - y1) * (x - x1) / (x2 - x1);

            return y;
        } catch (System.IndexOutOfRangeException) {
            return yValues[yValues.Length - 1];
        }
    }
}
