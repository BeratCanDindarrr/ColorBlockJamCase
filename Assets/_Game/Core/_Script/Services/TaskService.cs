using System;
using System.Collections.Generic;
using System.Threading;
using ColorBlockJam.Enums;
using ColorBlockJam.Interfaces;
using ColorBlockJam.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.Core.Service
{
    public class TaskService : ITaskService
    {
        private GameTaskData[] _taskDataArray = new GameTaskData[2];
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #region Injection
        private IObjectResolver _objectResolver; 
        public TaskService(IObjectResolver objectResolver)
        {
            _objectResolver = objectResolver;
        }
        #endregion
        public void Init()
        {
            _taskDataArray[0] = new GameTaskData { GameTasks = new Queue<IGameTask>() };
            _taskDataArray[1] = new GameTaskData { GameTasks = new Queue<IGameTask>() };
        }
        public void AddTask(IGameTask task,bool isImmediate = false,TaskType taskType = TaskType.FirstLayer)
        {
            var data = _taskDataArray[(int)taskType];
            if(data == null) return;
            if (isImmediate)
            {
                RunTaskSafe(task, _cancellationTokenSource.Token).Forget();
                return;
            }
            data.GameTasks.Enqueue(task);
            if (data.GameTasks.Count == 1) RunTasks(data).Forget();
        }

        private async UniTaskVoid RunTasks(GameTaskData data)
        {
            while (data.GameTasks.Count > 0)
            {
                await RunTaskSafe(data.GameTasks.Peek(), _cancellationTokenSource.Token);
                data.GameTasks.Dequeue();
            }
        }
        
        private async UniTask RunTaskSafe(IGameTask task, CancellationToken token)
        {
            try
            {
                _objectResolver.Inject(task);
                await task.RunTask(token);
            }
            catch (OperationCanceledException e)
            {
                DebugExtension.Log($"{nameof(TaskService)} cancelled: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                task.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    

    public class GameTaskData
    {
        public Queue<IGameTask> GameTasks = new Queue<IGameTask>();
    }
}