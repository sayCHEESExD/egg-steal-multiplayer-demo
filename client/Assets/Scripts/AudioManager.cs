// 1. Update AudioManager.cs to support 3 states
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource safeZoneSource;
    public AudioSource biomeSource;
    public AudioSource chaseSource; // Add your chase music here in the Inspector

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateMusicState(bool inBiome, bool isChased)
    {
        if (safeZoneSource == null || biomeSource == null || chaseSource == null) return;

        if (isChased)
        {
            if (!chaseSource.isPlaying)
            {
                safeZoneSource.Stop();
                biomeSource.Stop();
                chaseSource.Play();
            }
        }
        else if (inBiome)
        {
            if (!biomeSource.isPlaying)
            {
                safeZoneSource.Stop();
                chaseSource.Stop();
                biomeSource.Play();
            }
        }
        else
        {
            if (!safeZoneSource.isPlaying)
            {
                biomeSource.Stop();
                chaseSource.Stop();
                safeZoneSource.Play();
            }
        }
    }
}