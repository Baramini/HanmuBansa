namespace BrmnModules.UI
{
    // Always visible UI
    public abstract class PersistentUI : BaseUI
    {
        public override void Initialize()
        {
            gameObject.SetActive(true);
        }
    }
}