using UnityEngine;
using TMPro;

public class EnclosureUpgradeSign : MonoBehaviour
{
    public TMP_Text signText;
    private NetworkPlayer localPlayer;

    void Update()
    {
        // 1. Find the local player if we haven't already
        if (localPlayer == null)
        {
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.isLocalPlayer)
                {
                    localPlayer = p;
                    break;
                }
            }
        }

        // 2. Display the stats from the local player's serverState
        if (localPlayer != null && localPlayer.serverState != null && signText != null)
        {
            signText.text = $"Enclosure Lvl {localPlayer.serverState.enclosureLevel}\n" +
                            $"Capacity: {localPlayer.serverState.petCapacity}\n" +
                            $"<color=yellow>{localPlayer.serverState.enclosureUpgradeCost} Coins</color>\n" +
                            $"[G] Upgrade";
        }
    }
}