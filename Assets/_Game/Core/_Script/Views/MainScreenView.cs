namespace ColorBlockJam.Views
{
    public class MainScreenView : BaseView
    {
        public void Init()
        {
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}