using UnityEngine;
using System.Collections.Generic;
using Colyseus.Schema;

public class ScoreboardManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer; 
    public GameObject rowPrefab;       

    private List<ScoreboardRow> activeRows = new List<ScoreboardRow>();
    private float updateTimer = 0f;

    void Start()
    {
        if (contentContainer == null)
        {
            Debug.LogError("[Scoreboard] contentContainer is not assigned in the Inspector!");
            return;
        }

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= 0.5f)
        {
            updateTimer = 0f;
            UpdateScoreboard();
        }
    }

    private void UpdateScoreboard()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.room == null) return;
        if (NetworkManager.Instance.room.State == null || NetworkManager.Instance.room.State.players == null) return;

        var players = NetworkManager.Instance.room.State.players;
        
        List<KeyValuePair<string, Player>> playerList = new List<KeyValuePair<string, Player>>();
        players.ForEach((key, p) => playerList.Add(new KeyValuePair<string, Player>(key, p)));

        playerList.Sort((a, b) => b.Value.coins.CompareTo(a.Value.coins));

        while (activeRows.Count < playerList.Count)
        {
            GameObject newRowObj = Instantiate(rowPrefab, contentContainer);
            ScoreboardRow newRow = newRowObj.GetComponent<ScoreboardRow>();
            
            if (newRow == null)
            {
                Debug.LogError("[Scoreboard] RowPrefab does not have the ScoreboardRow component attached!");
                return;
            }
            
            activeRows.Add(newRow);
        }
        
        while (activeRows.Count > playerList.Count)
        {
            int lastIndex = activeRows.Count - 1;
            Destroy(activeRows[lastIndex].gameObject);
            activeRows.RemoveAt(lastIndex);
        }

        for (int i = 0; i < playerList.Count; i++)
        {
            ScoreboardRow row = activeRows[i];

            if (row.nameText == null || row.coinsText == null || row.speedText == null)
            {
                Debug.LogError($"[Scoreboard] Text references missing on Row {i}! Open the RowPrefab asset and assign Name, Coins, and Speed text slots.");
                continue;
            }
            
            string playerId = playerList[i].Key;
            string displayName = playerId.Length >= 4 ? playerId.Substring(0, 4) : playerId;
            
            row.nameText.text = "Player " + displayName;
            row.coinsText.text = FormatNumber(playerList[i].Value.coins);
            row.speedText.text = FormatNumber(playerList[i].Value.moveSpeed);
        }
    }

    private string FormatNumber(float num)
    {
        if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
        if (num >= 1000) return (num / 1000f).ToString("0.#") + "K";
        return num.ToString("0");
    }
}