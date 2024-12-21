using UnityEngine;
using UnityEngine.Audio;
using Devotion.MusicResourses;

namespace Devotion.Managers
{
    public class AudioManager : BaseManager
    {
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private MusicResourses.MusicResourses _music;

        private AudioSource _musicSourse;
        private AudioSource _effectSource;       

        private void Awake()
        {
            _musicSourse = GetComponent<AudioSource>();
            _effectSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            PlayMusic("BackGround");
        }

        public void PlayEffect(string name)
        {
            _effectSource.PlayOneShot(_music.GetEffect(name));
        }

        public void PlayMusic(string name)
        {
            if (_musicSourse.clip == _music.GetMusic(name))
                return;

            _musicSourse.clip = _music.GetMusic(name);
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
