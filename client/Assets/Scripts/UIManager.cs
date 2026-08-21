using UnityEngine;
using TMPro; // Added TMPro namespace

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI coinsText; // Changed to TextMeshProUGUI

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateCoins(float coins)
    {
        if (coinsText != null)
        {
            coinsText.text = "Coins: " + coins.ToString("F0");
        }
    }
}