using UnityEngine;

namespace BrmnModules.UI
{
    // Base class for all UI components.
    // All UI must inherit from either PersistentUI or PopupUI.
    public abstract class BaseUI : MonoBehaviour
    {
        public virtual void Initialize() { }
    }
}