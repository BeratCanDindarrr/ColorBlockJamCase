using ColorBlockJam.Enums;

namespace ColorBlockJam.Interfaces
{
    public interface ITaskService
    {
        public void AddTask(IGameTask task, bool isImmediate = false, TaskType taskType = TaskType.FirstLayer);
    }
}