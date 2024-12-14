using System;
using UnityEngine;

namespace Devotion.SoundManager
{
    [Serializable]
    public class Sound
    {
        [SerializeField] private SoundTags _tag;
        [SerializeField] private AudioClip[] _clips;

        public SoundTags Tag => _tag;
        public AudioClip[] Clips => _clips;
    }
}
