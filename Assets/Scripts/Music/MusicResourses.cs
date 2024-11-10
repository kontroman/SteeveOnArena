using System.Collections.Generic;
using UnityEngine;

namespace Devotion.MusicResourses
{
    public class MusicResourses : MonoBehaviour
    {
        [Header("MusicClips")]
        [SerializeField] private List<Music> _music = new List<Music>();

        [Header("EffectClips")]
        [SerializeField] private List<Music> _effects = new List<Music>();

        private Music _instanceElement;

        public AudioClip GetMusic(string name)
        {            
            foreach (var music in _music)
            {
                if (music.Name == name)
                    _instanceElement = music;
            }

            return _instanceElement.Clip;
        }

        public AudioClip GetEffect(string name)
        {
            foreach (var effect in _effects)
            {
                if (effect.Name == name)
                    _instanceElement = effect;
            }

            return _instanceElement.Clip;
        }

    }
}

