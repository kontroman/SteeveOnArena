using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Devotion.SoundManager
{
    public class SoundLibrary : MonoBehaviour
    {
        [Header("MusicClips")]
        [SerializeField] private List<Sound> _music = new();

        [Header("EffectClips")]
        [SerializeField] private List<Sound> _effects = new();

        public AudioClip GetMusic(SoundTags tag)
        {
            Sound musicElement = _music.FirstOrDefault(element => element.Tag == tag);

            return musicElement?.Clips[Random.Range(0, musicElement.Clips.Length)];
        }

        public AudioClip GetRandomEffect(SoundTags tag)
        {
            var effectElement = _effects.FirstOrDefault(element => element.Tag == tag);

            return effectElement?.Clips[Random.Range(0, effectElement.Clips.Length)];
        }
    }
}

