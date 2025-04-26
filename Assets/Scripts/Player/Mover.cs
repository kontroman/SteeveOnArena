using UnityEngine;

public class Mover : MonoBehaviour
{
    private const string Horizontal = nameof(Horizontal);
    private const string Vertical = nameof(Vertical);

    [SerializeField] private float _moveSpeed;

    private float _vericaleDirection;
    private float _horizontalDirection;
    private float _distance;

    private void Update()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        _vericaleDirection = Input.GetAxis(Vertical);
        _distance = _vericaleDirection * _moveSpeed * Time.deltaTime;
        transform.Translate(_distance * Vector3.right);
    }

    private void Rotate()
    {
        _horizontalDirection = Input.GetAxis(Horizontal);
        _distance = _horizontalDirection * _moveSpeed * Time.deltaTime;
        transform.Translate(_distance * Vector3.back);
    }
}
