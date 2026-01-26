using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OP<T>
{
    [SerializeField] private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                onValueChanged?.Invoke(_value);
            }
        }
    }

    public Action<T> onValueChanged;

    public void AddListener(Action<T> listener)
    {
        onValueChanged += listener;
    }

    public void RemoveListener(Action<T> listener)
    {
        onValueChanged -= listener;
    }

    public void RemoveAllListeners()
    {
        onValueChanged = null;
    }
}