using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private int _damage;
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _attackCooldown = 2f;

    private bool _canAttack = true;

    private void OnTriggerEnter(Collider other)
    {
        if (_canAttack && other.TryGetComponent(out Health health))
        {
            StartCoroutine(StartAction());
        }
    }

    private IEnumerator StartAction()
    {
        _canAttack = false;
        AttackOnCooldown();
        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }

    public void AttackOnCooldown()
    {
        Collider[] hitEnemies = Physics.OverlapCapsule(transform.position, _attackPoint.position, _attackRange, _enemyLayer);

        foreach (Collider enemy in hitEnemies)
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