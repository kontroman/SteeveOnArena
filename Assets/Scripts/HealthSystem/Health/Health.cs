using UnityEngine;
using System;
using Devotion.Basics;

public class Health : MonoBehaviour, IProgressBar
{
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;

    public event Action<float, float> OnHealthChanged;

    public float MaxValue => _maxHealth;
    public float CurrentValue => _currentHealth;

    private void Start()
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    //TODO: ICommand TakeDamage
    //TODO: ICommand Heal

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
        => Mathf.Clamp(currentValue, Constants.PlayerSettings.MinHealth, _maxHealth);

    
}