using UnityEngine;
using TMPro;

public class NetworkTreadmill : MonoBehaviour
{
    public string treadmillId;
    [HideInInspector] public Treadmill serverState;
    
    [Header("UI Sign")]
    public TMP_Text upgradeSignText; 

    private void Update()
    {
        if (serverState == null) return;

        if (upgradeSignText != null)
        {
            if (string.IsNullOrEmpty(serverState.ownerId))
            {
                upgradeSignText.text = "Unowned";
            }
            else
            {
                upgradeSignText.text = $"Lvl {serverState.level}\n<color=yellow>{serverState.upgradeCost} Coins</color>\n[F] Upgrade";
            }
        }
    }
}