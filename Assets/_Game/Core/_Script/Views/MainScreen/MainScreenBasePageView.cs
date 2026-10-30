namespace ColorBlockJam.Views
{
    // Each main screen page derives from this and keeps its own logic.
    public abstract class MainScreenBasePageView : BaseView
    {
        public virtual void OnPageSelected() { }

        public virtual void OnPageDeselected() { }
    }
}
