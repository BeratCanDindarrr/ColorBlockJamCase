using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.GameTask;
using ColorBlockJam.Views.TestPopup;
using ColorBlockJam.Utils;
using VContainer.Unity;

namespace ColorBlockJam.Core.Controller
{
    public class ApplicationController : IInitializable
    {
        private readonly ApplicationSettings _applicationSettings;
        private readonly TaskService _taskService;
        private readonly PopupService _popupService;
        
        public ApplicationController(ApplicationSettings applicationSettings
        , TaskService taskService
        , PopupService popupService)
        {
            _applicationSettings = applicationSettings;
            _taskService = taskService;
            _popupService = popupService;
        }
        public void Initialize()
        {
            DebugExtension.Log("ApplicationController initialized " + _applicationSettings.ApplicationName);
            _taskService.Init();
            _popupService.Init();
            
            var waitForSeconds = new WaitForSecondsGameTask();
            waitForSeconds.Init(2f);
            _taskService.AddTask(waitForSeconds);
            CallBackGameTask callBackGameTask = new CallBackGameTask();
            callBackGameTask.Init(() =>
            {
                DebugExtension.Log("CallBackGameTask " + _applicationSettings.ApplicationName);
                
            });
            _taskService.AddTask(callBackGameTask);
            _popupService.AddPopup(new TestPopupParameter());
        }
    }
}