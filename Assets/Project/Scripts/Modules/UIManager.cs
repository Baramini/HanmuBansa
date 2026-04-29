using UnityEngine;
using System.Collections.Generic;

namespace BrmnModules.UI
{
    // Provides API Only - Logic handled by each UI
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Persistent UIs")]
        [SerializeField] private List<PersistentUI> persistentUIs;

        [Header("Popup UI Prefabs")]
        [SerializeField] private List<PopupUI> popupPrefabs; // Popup UI Prefebs

        [Header("Popup Root")]
        [SerializeField] private Transform popupRoot; // Popup UI's Parent

        [Header("Backdrop")]
        [SerializeField] private GameObject backdrop;

        // -- Popup UI Caching --
        private Dictionary<System.Type, PopupUI> _popupCache = new();

        // -- Popup stack --
        private Stack<PopupUI> _popupStack = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeAll();
        }

        private void InitializeAll()
        {
            foreach (var ui in persistentUIs)
                ui?.Initialize();

            if (backdrop != null)
                backdrop.SetActive(false);
        }

        // -------------------------------------------------------
        // -- Popup API ------------------------------------------
        // -------------------------------------------------------

        public void ShowPopup<T>(System.Action<T> onBeforeShow = null) where T : PopupUI
        {
            T popup = GetPopup<T>();
            if (popup == null) return;

            // -- Action before show --
            onBeforeShow?.Invoke(popup);

            if (_popupStack.Count == 0 && backdrop != null)
                backdrop.SetActive(true);

            if (!_popupStack.Contains(popup))
                _popupStack.Push(popup);

            popup.Show();
        }

        public void HidePopup<T>() where T : PopupUI
        {
            T popup = GetPopup<T>();
            if (popup == null) return;

            // -- Remove from stack --
            RemoveFromStack(popup);
            popup.Hide();

            // -- Hide backdrop if no more popups --
            if (_popupStack.Count == 0 && backdrop != null)
                backdrop.SetActive(false);
        }
        public void HidePopup(PopupUI popup)
        {
            RemoveFromStack(popup);
            popup.Hide();

            if (_popupStack.Count == 0 && backdrop != null)
                backdrop.SetActive(false);
        }

        // -- Hide top popup (back button behavior) --
        public void HideTopPopup()
        {
            if (_popupStack.Count == 0) return;

            PopupUI top = _popupStack.Pop();
            top.Hide();

            if (_popupStack.Count == 0 && backdrop != null)
                backdrop.SetActive(false);
        }

        // -------------------------------------------------------
        // -- Persistent API -------------------------------------
        // -------------------------------------------------------

        public T GetPersistent<T>() where T : PersistentUI
        {
            foreach (var ui in persistentUIs)
            {
                if (ui is T target)
                    return target;
            }
            Debug.LogWarning($"UIManager: PersistentUI of type {typeof(T)} not found.");
            return null;
        }

        // -------------------------------------------------------
        // -- Internal -------------------------------------------
        // -------------------------------------------------------

        public T GetPopup<T>() where T : PopupUI
        {
            System.Type type = typeof(T);

            if (_popupCache.TryGetValue(type, out PopupUI cached))
                return cached as T;

            // Search Popup
            foreach (var prefab in popupPrefabs)
            {
                if (prefab is T)
                {
                    T instance = Instantiate(prefab, popupRoot) as T;
                    instance.Initialize();
                    _popupCache[type] = instance;
                    return instance;
                }
            }

            Debug.LogWarning($"UIManager: PopupUI prefab {typeof(T)} not found.");
            return null;
        }

        public bool IsPopupOpen<T>() where T : PopupUI
        {
            System.Type type = typeof(T);
            if (!_popupCache.TryGetValue(type, out PopupUI popup)) return false;
            return popup.gameObject.activeInHierarchy;
        }

        private void RemoveFromStack(PopupUI popup)
        {
            // -- Rebuild stack without the target popup --
            Stack<PopupUI> temp = new();

            while (_popupStack.Count > 0)
            {
                PopupUI top = _popupStack.Pop();
                if (top != popup) temp.Push(top);
            }

            while (temp.Count > 0) _popupStack.Push(temp.Pop());
        }
    }
}