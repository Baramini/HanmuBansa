using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace BrmnModules.Common
{
    // Map keys to UnityActions using Unity Input System
    public class KeyActionTrigger : MonoBehaviour
    {
        [System.Serializable]
        public class KeyBinding
        {
            public string label;
            public Key key;
            public UnityEngine.Events.UnityEvent onPressed;
        }

        [SerializeField] private List<KeyBinding> bindings;

        private void Update()
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
            {
                if (Keyboard.current[binding.key].wasPressedThisFrame) binding.onPressed?.Invoke();
            }
        }
    }
}