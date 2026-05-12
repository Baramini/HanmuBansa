using UnityEngine;
using System.Collections.Generic;

namespace BrmnModules.UI
{
    // API Only
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Persistent UIs")]
        [SerializeField] private List<PersistentUI> persistentUIs;

        [Header("Popup UI Prefabs")]
        [SerializeField] private List<PopupUI> popupPrefabs;

        [Header("Popup Root")]
        [SerializeField] private Transform popupRoot;

        [Header("Backdrop")]
        [SerializeField] private GameObject backdrop;

        private Dictionary<System.Type, PopupUI> popupCache = new();
        private Stack<PopupUI> popupStack = new();

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
            foreach (var ui in persistentUIs) ui?.Initialize();
            if (backdrop != null) backdrop.SetActive(false);
        }

        // -- Popup API --
        public void ShowPopup<T>(System.Action<T> onBeforeShow = null) where T : PopupUI
        {
            T popup = GetPopup<T>();
            if (popup == null) return;

            // Action before show
            onBeforeShow?.Invoke(popup);

            if (popupStack.Count == 0 && backdrop != null) backdrop.SetActive(true);

            if (!popupStack.Contains(popup)) popupStack.Push(popup);

            popup.Show();
        }

        public void HidePopup<T>() where T : PopupUI
        {
            T popup = GetPopup<T>();
            if (popup == null) return;

            // Remove from stack
            RemoveFromStack(popup);
            popup.Hide();

            // Hide backdrop if no more popups
            if (popupStack.Count == 0 && backdrop != null) backdrop.SetActive(false);
        }
        public void HidePopup(PopupUI popup)
        {
            RemoveFromStack(popup);
            popup.Hide();

            if (popupStack.Count == 0 && backdrop != null) backdrop.SetActive(false);
        }

        public void HideTopPopup()
        {
            if (popupStack.Count == 0) return;

            PopupUI top = popupStack.Pop();
            top.Hide();

            if (popupStack.Count == 0 && backdrop != null) backdrop.SetActive(false);
        }

        public T GetPopup<T>() where T : PopupUI
        {
            System.Type type = typeof(T);

            if (popupCache.TryGetValue(type, out PopupUI cached)) return cached as T;

            // Search Popup
            foreach (var prefab in popupPrefabs)
            {
                if (prefab is T)
                {
                    T instance = Instantiate(prefab, popupRoot) as T;
                    instance.Initialize();
                    popupCache[type] = instance;
                    return instance;
                }
            }

            return null;
        }

        public bool IsPopupOpen<T>() where T : PopupUI
        {
            System.Type type = typeof(T);
            if (!popupCache.TryGetValue(type, out PopupUI popup)) return false;
            return popup.gameObject.activeInHierarchy;
        }

        public bool IsAnyPopupOpen => popupStack.Count > 0;

        private void RemoveFromStack(PopupUI popup)
        {
            // Remake stack
            Stack<PopupUI> temp = new();

            while (popupStack.Count > 0)
            {
                PopupUI top = popupStack.Pop();
                if (top != popup) temp.Push(top);
            }

            while (temp.Count > 0) popupStack.Push(temp.Pop());
        }

        public void ResetPopupState()
        {
            foreach (var popup in popupCache.Values)
            {
                if (popup != null && popup.gameObject.activeSelf) popup.Hide(false);
            }
 
            popupStack.Clear();
 
            if (backdrop != null) backdrop.SetActive(false);
        }

        // -- Persistent API --
        public T GetPersistent<T>() where T : PersistentUI
        {
            foreach (var ui in persistentUIs)
            {
                if (ui is T target) return target;
            }

            return null;
        }

        // -- Internal --
        public void ShowSettingsPopup()
        {
            if (!IsPopupOpen<SettingsPopup>()) ShowPopup<SettingsPopup>();
            else HidePopup<SettingsPopup>();
        }
    }
}