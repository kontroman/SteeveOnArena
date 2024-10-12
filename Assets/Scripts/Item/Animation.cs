using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

public class Animation : MonoBehaviour
{
    [SerializeField] private Vector3 _position;
    [SerializeField] private Vector3 _rotation;
    [SerializeField] private float _duration;
    [SerializeField] private int _repeats;

    private void Start()
    {
        transform.DOMove(_position, _duration).SetLoops(_repeats, LoopType.Yoyo).SetEase(Ease.Linear);
        transform.DORotate(_rotation, _duration).SetLoops(_repeats, LoopType.Restart).SetEase(Ease.Linear);
    }
}
