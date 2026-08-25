using UnityEngine;
using TMPro;

public class PetUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI incomeText;

    public void SetupUI(int biomeIndex)
    {
        string pName = "";
        string rName = "";
        Color rColor = Color.white;

        // Distributing 8 pets across the 5 rarities
        switch (biomeIndex)
        {
            case 0: pName = "Chicken"; rName = "Uncommon"; ColorUtility.TryParseHtmlString("#00FF00", out rColor); break;
            case 1: pName = "Desert Scorpion"; rName = "Uncommon"; ColorUtility.TryParseHtmlString("#00FF00", out rColor); break;
            case 2: pName = "Black Widow"; rName = "Epic"; ColorUtility.TryParseHtmlString("#A020F0", out rColor); break;
            case 3: pName = "Arctic Wolf"; rName = "Epic"; ColorUtility.TryParseHtmlString("#A020F0", out rColor); break;
            case 4: pName = "Stag Beetle"; rName = "Legendary"; ColorUtility.TryParseHtmlString("#FFD700", out rColor); break;
            case 5: pName = "Blue Whale"; rName = "Legendary"; ColorUtility.TryParseHtmlString("#FFD700", out rColor); break;
            case 6: pName = "T-Rex"; rName = "Mythic"; ColorUtility.TryParseHtmlString("#FF0000", out rColor); break;
            case 7: pName = "The Eyeless"; rName = "Cosmic"; ColorUtility.TryParseHtmlString("#4B0082", out rColor); break;
        }

        rarityText.text = rName;
        rarityText.color = rColor;
        nameText.text = pName;
        nameText.color = Color.white;
        
        // Matches your Node.js server formula: 1 + (biomeIndex * 2)
        int income = 1 + (biomeIndex * 2);
        incomeText.text = "$" + income + "/s";
        incomeText.color = Color.yellow;
    }

    void LateUpdate()
    {
        // Keeps the UI facing the player's camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}