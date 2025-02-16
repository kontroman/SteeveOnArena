using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private int _damage;
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _enemyLayer;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out Health health))
        {
            Debug.Log(health.gameObject.name);
            health.TakeDamage(_damage);
        }
    }

    public void StartAction()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_attackPoint.position, _attackRange, _enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.gameObject.TryGetComponent(out Health health))
                health.TakeDamage(_damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_attackPoint == null)
            return;

        Gizmos.DrawSphere(_attackPoint.position, _attackRange);
    }
}