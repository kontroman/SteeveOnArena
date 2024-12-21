using System;
using UnityEngine;

namespace Devotion.MusicResourses
{
    [Serializable]
    public class Music
    {
        [SerializeField] private string _name;
        [SerializeField] private AudioClip _clip;

        public string Name => _name;
        public AudioClip Clip => _clip;
    }
}
