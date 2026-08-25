using UnityEngine;
using TMPro; // Add this if you want to modify alpha/opacity

public class SleepingIndicator : MonoBehaviour
{
    [Header("References")]
    public GameObject zzzContainer; // Assign the TextMeshPro GameObject here

    [Header("Animation Settings")]
    public float loopDuration = 2f;
    public Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 endScale = new Vector3(1.2f, 1.2f, 1.2f);
    
    private float animTimer = 0f;
    private Vector3 lastGuardPosition;
    private Transform guardTransform;

    void Start()
    {
        // Assuming this Canvas is a child of the Guard root object
        guardTransform = transform.parent; 
        lastGuardPosition = guardTransform.position;
    }

    void LateUpdate()
    {
        // 1. Keep the canvas facing the camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        // 2. Check if the guard is moving (chasing or returning)
        float moveDistance = Vector3.Distance(guardTransform.position, lastGuardPosition);
        lastGuardPosition = guardTransform.position;

        // If moving more than a tiny threshold, it is awake
        if (moveDistance > 0.005f)
        {
            zzzContainer.SetActive(false);
            animTimer = 0f; // Reset animation for when it falls asleep again
            return;
        }

        // 3. Guard is stationary (sleeping) - Run the animation
        if (!zzzContainer.activeSelf)
        {
            zzzContainer.SetActive(true);
        }

        animTimer += Time.deltaTime;
        float progress = animTimer / loopDuration;

        if (progress > 1f)
        {
            animTimer = 0f; // Reset loop
            progress = 0f;
        }

        // Scale up gradually
        zzzContainer.transform.localScale = Vector3.Lerp(startScale, endScale, progress);

        // Optional: Fade out right before it pops/loops for a smoother disappearance
        TextMeshProUGUI textComp = zzzContainer.GetComponent<TextMeshProUGUI>();
        if (textComp != null)
        {
            Color c = textComp.color;
            // Fades to 0 opacity in the last 20% of the animation
            c.a = progress > 0.8f ? Mathf.Lerp(1f, 0f, (progress - 0.8f) / 0.2f) : 1f; 
            textComp.color = c;
        }
    }
}