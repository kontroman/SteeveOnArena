using System;
using UnityEngine;

namespace MineArena.MusicResourses
{
    [Serializable]
    public class Music
    {
        [SerializeField] private string _name;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 3f)] private float _volume = 1f;

        public string Name => _name;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
    }
}
