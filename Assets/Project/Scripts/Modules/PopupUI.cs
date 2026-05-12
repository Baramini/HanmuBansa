using UnityEngine;
using System.Collections;

namespace BrmnModules.UI
{
    // Popup UI with fade and backdrop
    public abstract class PopupUI : BaseUI
    {
        [Header("Popup Settings")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine fadeCoroutine;

        public override void Initialize()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f));
        }

        public virtual void Hide(bool fade = true)
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!fade)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                return;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, onComplete: () =>
            {
                gameObject.SetActive(false);
            }));
        }

        private IEnumerator FadeCoroutine(float from, float to, System.Action onComplete = null)
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        public virtual void OnCloseButton()
        {
            UIManager.Instance?.HidePopup(this);
        }
    }
}