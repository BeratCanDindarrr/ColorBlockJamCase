using ColorBlockJam.Views;

namespace ColorBlockJam.Interfaces
{
    public interface IPopupService
    {
        public void AddPopup(BasePopupParameters param);
        // A popup is on screen (the level clock stops meanwhile).
        public bool HasOpenPopup { get; }
    }
}