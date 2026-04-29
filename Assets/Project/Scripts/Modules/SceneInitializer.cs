using UnityEngine;
using System.Collections;
using BrmnModules.Audio;

namespace BrmnModules.Common
{
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Custom Actions")]
        [SerializeField] private string initialBgmName = "";
        [SerializeField] private UnityEngine.Events.UnityEvent onSceneInitialized;

        [Header("Delayed Actions")]
        [SerializeField] private float delaySeconds = 0.1f;
        [SerializeField] private UnityEngine.Events.UnityEvent onDelayedInitialized;

        private void Start()
        {
            onSceneInitialized?.Invoke();
            if (initialBgmName != "") AudioManager.Instance.PlayBGM(initialBgmName, true);
            StartCoroutine(DelayedInitializeCoroutine());
        }

        private IEnumerator DelayedInitializeCoroutine()
        {
            yield return new WaitForSeconds(delaySeconds);
            onDelayedInitialized?.Invoke();
        }
    }
}