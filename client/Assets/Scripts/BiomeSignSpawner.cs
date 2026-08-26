using UnityEngine;
using TMPro;

public class BiomeSignSpawner : MonoBehaviour
{
    public GameObject signPrefab;
    private readonly int[] biomeCenters = { 100, 200, 300, 400, 500, 600, 700, 800 };

    void Start()
    {
        for (int i = 0; i < biomeCenters.Length; i++)
        {
            bool isEven = i % 2 == 0;
            
            // Align with guard X, place 10 units in front of the guard/nest on Z
            float signX = isEven ? 30f : -30f; 
            float signZ = biomeCenters[i] + 15f; 

            // Rotate 180 degrees on Y so it faces the player running towards it
            GameObject sign = Instantiate(signPrefab, new Vector3(signX, 0f, signZ), Quaternion.Euler(0, 180, 0));
            
            // Calculate recommended speed
            // Adjust this formula to match what you feel is required to outrun the guard
            int recommendedSpeed = 15 + (i * 10); 
            
            TMP_Text tmp = sign.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                // Assuming sprite index 0 is your shoe sprite
                tmp.text = $"<sprite=0>{recommendedSpeed}\nrecommended";
            }
        }
    }
}