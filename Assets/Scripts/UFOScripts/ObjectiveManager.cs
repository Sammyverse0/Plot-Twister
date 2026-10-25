using System;
using UnityEngine;
using UnityEngine.Events;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private int totalButtons = 3;
    [SerializeField] private AlienSpawner spawner;
    [SerializeField] private UnityEvent onAllPressed;

    public int Pressed { get; private set; }
    public event Action<int> ButtonPressed;

    private void Start()
    {
        spawner.SetTier(0);
    }

    public void RegisterPress()
    {
        Pressed++;
        ButtonPressed?.Invoke(Pressed);

        if (Pressed >= totalButtons)
        {
            spawner.StopSpawning();
            onAllPressed.Invoke();
        }
        else
        {
            spawner.SetTier(Pressed);
        }
    }
}