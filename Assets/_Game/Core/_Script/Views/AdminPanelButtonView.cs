using ColorBlockJam.Interfaces;
using VContainer;

namespace ColorBlockJam.Views
{
    // AdminPanelButtonView: a small button over every screen (its own canvas) that opens the admin panel.
    // Made by ScreenController when ScreenViewSettings.showAdminPanel is on; its click is wired in the prefab.
    public class AdminPanelButtonView : BaseView
    {
        private IPopupService _popupService;

        [Inject]
        public void Construct(IPopupService popupService)
        {
            _popupService = popupService;
        }

        // Not over another popup.
        public void OpenAdminPanel()
        {
            if (_popupService.HasOpenPopup) return;
            _popupService.AddPopup(new AdminPanelPopupParameters());
        }
    }
}
