using UnityEngine;

namespace BrmnModules.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clip Data")]
        [SerializeField] private BGMData bgmData;
        [SerializeField] private SFXData sfxData;

        [Header("Volume Settings")]
        [SerializeField] private int defaultMasterVolume = 5;
        [SerializeField] private int defaultBgmVolume = 5;
        [SerializeField] private int defaultSfxVolume = 5;

        // 0~10 int volume --
        private int masterVolume;
        private int bgmVolume;
        private int sfxVolume;

        // Convert 0~10 to 0~1 --
        private float MasterVolume => masterVolume / 10f;
        private float BgmVolume => bgmVolume / 10f;
        private float SfxVolume => sfxVolume / 10f;

        private void Awake()
        {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmData == null) bgmData = Resources.Load<BGMData>("BGMData");
            if (sfxData == null) sfxData = Resources.Load<SFXData>("SFXData");
 
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }
 
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
 
            LoadVolumes();
        }

        // -- BGM API --
        public void PlayBGM(string key, bool loop = true)
        {
            AudioClip clip = bgmData?.Get(key);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: BGM key '{key}' not found.");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = MasterVolume * BgmVolume;
            bgmSource.Play();
        }

        public void StopBGM() => bgmSource.Stop();
        public void PauseBGM() => bgmSource.Pause();
        public void ResumeBGM() => bgmSource.UnPause();

        public void ChangeBGM(string key, bool loop = true)
        {
            StopBGM();
            PlayBGM(key, loop);
        }

        // -- SFX API --
        public void PlaySFX(string key)
        {
            AudioClip clip = sfxData?.Get(key);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: SFX key '{key}' not found.");
                return;
            }
            sfxSource.PlayOneShot(clip, MasterVolume * SfxVolume);
        }

        public void PlaySFXAtPosition(string key, Vector3 position)
        {
            AudioClip clip = sfxData?.Get(key);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, MasterVolume * SfxVolume);
        }

        // -- Volume API --
        public void SetMasterVolume(int volume)
        {
            masterVolume = Mathf.Clamp(volume, 0, 10);
            ApplyVolumes();
            SaveVolumes();
        }

        public void SetBGMVolume(int volume)
        {
            bgmVolume = Mathf.Clamp(volume, 0, 10);
            bgmSource.volume = MasterVolume * BgmVolume;
            SaveVolumes();
        }

        public void SetSFXVolume(int volume)
        {
            sfxVolume = Mathf.Clamp(volume, 0, 10);
            SaveVolumes();
        }

        public int GetMasterVolume() => masterVolume;
        public int GetBGMVolume() => bgmVolume;
        public int GetSFXVolume() => sfxVolume;

        // -- Internal --
        private void ApplyVolumes()
        {
            bgmSource.volume = MasterVolume * BgmVolume;
        }

        private void SaveVolumes()
        {
            PlayerPrefs.SetInt("MasterVolume", masterVolume);
            PlayerPrefs.SetInt("BGMVolume", bgmVolume);
            PlayerPrefs.SetInt("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumes()
        {
            masterVolume = PlayerPrefs.GetInt("MasterVolume", defaultMasterVolume);
            bgmVolume = PlayerPrefs.GetInt("BGMVolume", defaultBgmVolume);
            sfxVolume = PlayerPrefs.GetInt("SFXVolume", defaultSfxVolume);
            ApplyVolumes();
        }
    }
}