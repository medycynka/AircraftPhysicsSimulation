

public class MinMax
{
    public float minVal { get; private set; }
    public float maxVal { get; private set; }

    public MinMax()
    {
        minVal = float.MaxValue;
        maxVal = float.MinValue;
    }

    public void AddValue(float v)
    {
        if (v > maxVal)
        {
            maxVal = v;
        }

        if (v < minVal)
        {
            minVal = v;
        }
    }
}
