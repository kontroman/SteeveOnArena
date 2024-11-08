using UnityEngine;
using UnityEngine.Audio;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance { get; private set; }

    [SerializeField] private AudioSource _music;
    [SerializeField] private AudioSource _effectSource;
    [SerializeField] private AudioMixer _audioMixer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayEffect(AudioClip clip)
    {
        _effectSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (_music.clip == clip)
            return;

        _music.clip = clip;
        _music.Play();
    }

    public void StopMusic()
    {
        _music.Stop();
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
