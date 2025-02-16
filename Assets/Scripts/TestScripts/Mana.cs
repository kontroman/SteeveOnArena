using System;
using UnityEngine;

public class Mana : MonoBehaviour
{
    [SerializeField] private float _maxMana;
    [SerializeField] private float _value;

    private float _currentMana;

    public event Action<float, float> OnManaChanged; // change on "message"
    public float MaxMana => _maxMana;
    public float CurrentMana => _currentMana;

    private void Start()
    {
        _currentMana = _maxMana;
        OnManaChanged.Invoke(_currentMana, _maxMana);
    }

    private void Update()
    {
        UseSpell(_value);
    }

    public void UseSpell(float value)
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _currentMana -= value;
            OnManaChanged.Invoke(_currentMana, _maxMana);
        }
    }
}
