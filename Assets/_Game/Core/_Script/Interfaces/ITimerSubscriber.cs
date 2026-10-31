namespace ColorBlockJam.Interfaces
{
    public interface ITimerSubscriber
    {
        public void Subscribe(ITimerOwner owner);
        public void UnSubscribe(ITimerOwner owner);
    }
}