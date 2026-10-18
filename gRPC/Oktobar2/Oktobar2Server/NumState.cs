namespace Oktobar2Server
{
    public class NumState
    {
        private readonly object _lockObject = new();
        private int[] _nums = { 0,0};


        public int[] GetNum()
        {
            lock (_lockObject)
            {
                return _nums;
            }
        }

        public void SetNum(int num)
        {
            lock (_lockObject)
            {
                _nums[0] = _nums[1];
                _nums[1] = num;
            }
        }
    }
}
