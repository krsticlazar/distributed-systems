namespace DSLab1Server;

public class AccState
{
    private readonly object _lockObject = new();
    private int _acc = 1;

    public int GetAcc()
    {
        lock (_lockObject)
        {
            return _acc;
        }
    }

    public void SetAcc(int value)
    {
        lock (_lockObject)
        {
            _acc = value;
        }
    }
}
