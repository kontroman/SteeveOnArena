using UnityEngine;
using UnityEngine.Audio;
using Devotion.SoundManager;

namespace Devotion.Managers
{
    public class SoundManager : BaseManager
    {
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private SoundLibrary _sound;

        private AudioSource _musicSourse;
        private AudioSource _effectSource;

        private void Awake()
        {
            _musicSourse = GetComponent<AudioSource>();
            _effectSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            PlayMusic(SoundTags.BackGround);
        }

        public void PlayEffect(SoundTags tag)
        {
            _effectSource.PlayOneShot(_sound.GetRandomEffect(tag));
        }

        public void PlayMusic(SoundTags tag)
        {
            if (_musicSourse.clip == _sound.GetMusic(tag))
                return;

            _musicSourse.clip = _sound.GetMusic(tag);
            _musicSourse.Play();
        }

        public void StopMusic()
        {
            _musicSourse.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            _audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        }

        public void SetEffectVolume(float volume)
        {
            _audioMixer.SetFloat("EffectVolume", Mathf.Log10(volume) * 20);
        }
    }
}
