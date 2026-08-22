using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI biomeText;

    private void Awake()
    {
        Instance = this;
        if (biomeText != null) biomeText.gameObject.SetActive(false);
    }

    public void UpdateStats(float coins, float speed)
    {
        if (coinsText != null)
        {
            // Formats as $82,689
            coinsText.text = "$" + coins.ToString("N0"); 
        }

        if (speedText != null)
        {
            // Formats large numbers with 'K' like in the image, with a shoe emoji
            if (speed >= 1000)
                speedText.text = "👟 " + (speed / 1000f).ToString("0.#") + "K";
            else
                speedText.text = "👟 " + speed.ToString("F0");
        }
    }

    public void ShowBiomeText(string text)
    {
        if (biomeText != null)
        {
            biomeText.text = text;
            biomeText.gameObject.SetActive(true);
            
            StopAllCoroutines();
            StartCoroutine(HideBiomeTextRoutine());
        }
    }

    private IEnumerator HideBiomeTextRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (biomeText != null) biomeText.gameObject.SetActive(false);
    }
}