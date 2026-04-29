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
        [SerializeField] private int defaultBGMVolume = 5;
        [SerializeField] private int defaultSFXVolume = 5;

        // -- 0~10 integer volume --
        private int _masterVolume;
        private int _bgmVolume;
        private int _sfxVolume;

        // -- Convert 0~10 to 0~1 --
        private float MasterF => _masterVolume / 10f;
        private float BGMF => _bgmVolume / 10f;
        private float SFXF => _sfxVolume / 10f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

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

        // -------------------------------------------------------
        // -- BGM API --------------------------------------------
        // -------------------------------------------------------

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
            bgmSource.volume = MasterF * BGMF;
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

        // -------------------------------------------------------
        // -- SFX API --------------------------------------------
        // -------------------------------------------------------

        public void PlaySFX(string key)
        {
            AudioClip clip = sfxData?.Get(key);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: SFX key '{key}' not found.");
                return;
            }
            sfxSource.PlayOneShot(clip, MasterF * SFXF);
        }

        public void PlaySFXAtPosition(string key, Vector3 position)
        {
            AudioClip clip = sfxData?.Get(key);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, MasterF * SFXF);
        }

        // -------------------------------------------------------
        // -- Volume API -----------------------------------------
        // -------------------------------------------------------

        public void SetMasterVolume(int volume)
        {
            _masterVolume = Mathf.Clamp(volume, 0, 10);
            ApplyVolumes();
            SaveVolumes();
        }

        public void SetBGMVolume(int volume)
        {
            _bgmVolume = Mathf.Clamp(volume, 0, 10);
            bgmSource.volume = MasterF * BGMF;
            SaveVolumes();
        }

        public void SetSFXVolume(int volume)
        {
            _sfxVolume = Mathf.Clamp(volume, 0, 10);
            SaveVolumes();
        }

        public int GetMasterVolume() => _masterVolume;
        public int GetBGMVolume() => _bgmVolume;
        public int GetSFXVolume() => _sfxVolume;

        // -------------------------------------------------------
        // -- Internal -------------------------------------------
        // -------------------------------------------------------

        private void ApplyVolumes()
        {
            bgmSource.volume = MasterF * BGMF;
        }

        private void SaveVolumes()
        {
            PlayerPrefs.SetInt("MasterVolume", _masterVolume);
            PlayerPrefs.SetInt("BGMVolume", _bgmVolume);
            PlayerPrefs.SetInt("SFXVolume", _sfxVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumes()
        {
            _masterVolume = PlayerPrefs.GetInt("MasterVolume", defaultMasterVolume);
            _bgmVolume = PlayerPrefs.GetInt("BGMVolume", defaultBGMVolume);
            _sfxVolume = PlayerPrefs.GetInt("SFXVolume", defaultSFXVolume);
            ApplyVolumes();
        }
    }
}