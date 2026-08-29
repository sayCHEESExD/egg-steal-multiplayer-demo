using UnityEngine;
using TMPro;

public class EggTimerUI : MonoBehaviour
{
    public TMP_Text timerText;

    void Update()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.room != null && NetworkManager.Instance.room.State != null)
        {
            float timeRemaining = NetworkManager.Instance.room.State.eggTimer;
            
            // Prevent visual negatives
            if (timeRemaining < 0) timeRemaining = 0;

            int minutes = Mathf.FloorToInt(timeRemaining / 60F);
            int seconds = Mathf.FloorToInt(timeRemaining - minutes * 60);
            
            timerText.text = string.Format("Eggs reset in {0:00}m {1:00}s", minutes, seconds);
        }
    }
}