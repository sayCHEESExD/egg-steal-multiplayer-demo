// 1. Update BiomeSignSpawner.cs to display the massive text values
using UnityEngine;
using TMPro;

public class BiomeSignSpawner : MonoBehaviour
{
    public GameObject signPrefab;
    private readonly int[] biomeCenters = { 100, 200, 300, 400, 500, 600, 700, 800 };
    
    // Hardcoded Roblox-style massive numbers
    private readonly string[] recommendedSpeedTexts = { "1K", "5K", "10K", "25K", "40K", "100k", "300k", "1M" };

    void Start()
    {
        for (int i = 0; i < biomeCenters.Length; i++)
        {
            bool isEven = i % 2 == 0;
            float signX = isEven ? 30f : -30f; 
            float signZ = biomeCenters[i] + 10f; 

            GameObject sign = Instantiate(signPrefab, new Vector3(signX, 0f, signZ), Quaternion.Euler(0, 180, 0));
            
            TMP_Text tmp = sign.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = $"<sprite=0> {recommendedSpeedTexts[i]}\nrecommended";
            }
        }
    }
}