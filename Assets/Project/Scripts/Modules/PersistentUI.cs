using UnityEngine;

namespace BrmnModules.UI
{
    // Always Visible UI
    // No Show/Hide - only internal data updates
    public abstract class PersistentUI : BaseUI
    {
        public override void Initialize()
        {
            gameObject.SetActive(true);
        }
    }
}