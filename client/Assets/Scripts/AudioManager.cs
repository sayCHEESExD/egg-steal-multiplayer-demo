using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource safeZoneSource;
    public AudioSource biomeSource;

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

    public void ToggleBiomeMusic(bool inBiome)
    {
        if (safeZoneSource == null || biomeSource == null) return;

        if (inBiome)
        {
            if (!biomeSource.isPlaying)
            {
                safeZoneSource.Stop();
                biomeSource.Play();
            }
        }
        else
        {
            if (!safeZoneSource.isPlaying)
            {
                biomeSource.Stop();
                safeZoneSource.Play();
            }
        }
    }
}