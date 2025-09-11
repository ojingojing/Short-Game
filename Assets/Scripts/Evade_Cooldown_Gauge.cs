using UnityEngine;
using UnityEngine.UI;

public class Evade_Cooldown_Gauge : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player; 
    public Image fill_image;        

    [Header("Options")]
    [Tooltip("게이지가 비활성일 때 숨길지 여부")]
    public bool hide_when_ready = true;

    void Reset()
    {
        fill_image = GetComponent<Image>();
    }

    void Update()
    {
        if (player == null || fill_image == null) return;

        float total = Mathf.Max(0.0001f, player.evade_cooldown);
        float remaining = Mathf.Clamp(player.evade_cooldown_remaining, 0f, total);

        float progress = 1f - (remaining / total);
        fill_image.fillAmount = progress;

        if (hide_when_ready)
        {
            bool ready = (player.evade_cooldown <= 0f) || (player.evade_cooldown_remaining <= 0f);
            fill_image.enabled = !ready;
        }
    }
}