using System;
using UnityEngine;
using UnityEngine.Events;

public class ObjectiveManager : MonoBehaviour
{
    [SerializeField] private int totalButtons = 3;
    [SerializeField] private AlienSpawner spawner;
    [SerializeField] private UnityEvent onAllPressed;

    [Header("Press Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip firstPressClip;
    [SerializeField] private AudioClip secondPressClip;
    [SerializeField] private AudioClip thirdPressClip;
    [SerializeField, Range(0f, 1f)] private float pressVolume = 1f;

    public int Pressed { get; private set; }
    public event Action<int> ButtonPressed;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        spawner.SetTier(0);
    }

    public void RegisterPress()
    {
        Pressed++;
        PlayPressClip(Pressed);
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

    private void PlayPressClip(int pressNumber)
    {
        AudioClip clip = pressNumber switch
        {
            1 => firstPressClip,
            2 => secondPressClip,
            3 => thirdPressClip,
            _ => null
        };

        if (clip != null)
        {
            audioSource.PlayOneShot(clip, pressVolume);
        }
    }
}