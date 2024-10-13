using DG.Tweening;
using UnityEngine;

[ExecuteInEditMode]
public class Animation : MonoBehaviour
{
    [SerializeField] private Vector3 _positionInput;
    [SerializeField] private Vector3 _rotation;
    [SerializeField] private float _duration;
    [SerializeField] private int _repeats;

    private void Start()
    {
        transform.DOMove(transform.position + _positionInput, _duration).SetLoops(_repeats, LoopType.Yoyo).SetEase(Ease.Linear);
        transform.DORotate(_rotation, _duration).SetLoops(_repeats, LoopType.Restart).SetEase(Ease.Linear);
    }
}
