namespace ColorBlockJam.Interfaces
{
    public interface ITimerOwner
    {
        public void UpdateTime(float intervalTime);
        public void ResumeTime(float passedRealTime);
    }
}