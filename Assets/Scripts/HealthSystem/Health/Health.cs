using UnityEngine;
using System;
using Devotion.Controllers;

public class Health : MonoBehaviour
{
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;

    private int _minHealth = 0;

    public event Action<float,float> OnHealthChanged; // change on "message"

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

    private void Start()
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (damage > 0)
            _currentHealth -= damage;

        _currentHealth = DetermineValue(_currentHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Heal(float extraHealth)
    {
        if (extraHealth > 0)
            _currentHealth += extraHealth;

        _currentHealth = DetermineValue(_currentHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private float DetermineValue(float currentValue)
        => Mathf.Clamp(currentValue, _minHealth, _maxHealth);
}