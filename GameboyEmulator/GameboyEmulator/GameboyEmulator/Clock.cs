namespace GameboyEmulator
{
    public class Clock
    {
        private int cycleCount;
        private int lastCycleCountIncrement;

        public void IncrementCycleCount( int offset )
        {
            lastCycleCountIncrement = offset;
            cycleCount += offset;
        }

        public int LastCycleCountIncrement { get { return lastCycleCountIncrement; } }
        
        public int CycleCount { get { return cycleCount; } }

        public bool IsFrameCompleted
        {
            get { return CycleCount >= 70224; }
        }

        public void Reset()
        {
            cycleCount = 0;
        }
    }
}