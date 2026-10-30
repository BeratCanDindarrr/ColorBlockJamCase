using ColorBlockJam.Core.Service;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.Enums;
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
        private readonly ScreenController _screenController;
        
        public ApplicationController(ApplicationSettings applicationSettings
        , TaskService taskService
        , PopupService popupService
        , ScreenController screenController)
        {
            _applicationSettings = applicationSettings;
            _taskService = taskService;
            _popupService = popupService;
            _screenController = screenController;
        }
        public void Initialize()
        {
            DebugExtension.Log("ApplicationController initialized " + _applicationSettings.ApplicationName);
            _taskService.Init();
            _popupService.Init();
            _screenController.Init();
            
            
            
            //loading and play 
            _screenController.SetScreen(ScreenType.LoadingScreen);
            
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