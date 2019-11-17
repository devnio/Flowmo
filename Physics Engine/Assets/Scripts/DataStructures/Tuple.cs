public class Tuple<T1, T2, T3>
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
}

[System.Serializable]
public class DistTuple : Tuple<int, int, float>
{
    public DistTuple(int particle1, int particle2, float distance)
    {
        Item1 = particle1;
        Item2 = particle2;
        Item3 = distance;
    }
}

public class Tuple<T1, T2>
{
    public T1 Item1;
    public T2 Item2;

    public Tuple(T1 i1, T2 i2)
    {
        this.Item1 = i1;
        this.Item2 = i2;
    }
}
