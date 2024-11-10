using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Devotion.MusicResourses
{
    public class MusicResourses : MonoBehaviour
    {
        [Header("MusicClips")]
        [SerializeField] private List<Music> _music = new List<Music>();

        [Header("EffectClips")]
        [SerializeField] private List<Music> _effects = new List<Music>();

        public AudioClip GetMusic(string name)
        {
            var musicElement = _music.TakeWhile(element => element.Name == name).ToList();

            return musicElement[0].Clip;
        }

        public AudioClip GetEffect(string name)
        {
            var effectElement = _effects.TakeWhile(element => element.Name == name).ToList();

            return effectElement[0].Clip;
        }

    }
}

