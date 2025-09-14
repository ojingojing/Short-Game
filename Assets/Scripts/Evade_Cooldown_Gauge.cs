using UnityEngine;
using UnityEngine.UI;

public class Evade_Cooldown_Gauge : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public Image fill_image;          // existing: cooldown/ready base bar (Filled, Horizontal)
    [Tooltip("Overlay image for attack charge (Filled, Horizontal, Left->Right)")]
    public Image charge_image;        // NEW: yellow overlay for charge

    [Header("Options")]
    [Tooltip("Hide the bar when evade is fully ready (no cooldown) and not charging")]
    public bool hide_when_ready = true;

    [Header("Charge Style")]
    public Color charge_color = Color.yellow;      // color for charge overlay

    
    [Tooltip("Show the bar while charging even if evade is ready")]
    public bool show_while_charging = true;

    void Reset()
    {
        fill_image = GetComponent<Image>();
    }

    void Update()
    {
        if (player == null || fill_image == null) return;

        float total_cd = Mathf.Max(0.0001f, player.evade_cooldown);
        float remaining_cd = Mathf.Clamp(player.evade_cooldown_remaining, 0f, total_cd);
        float cooldown_progress = 1f - (remaining_cd / total_cd);
        fill_image.fillAmount = cooldown_progress;

        bool is_charging = player.IsChargingAttack; // public getter in PlayerController
        if (charge_image != null)
        {
            if (is_charging && player.max_attack_charge_time > 0f) // uses public field
            {
                float ratio = Mathf.Clamp01(player.AttackChargeTime / player.max_attack_charge_time);
                charge_image.enabled = true;
                charge_image.color = charge_color;
                charge_image.fillAmount = ratio; // left→right fill
            }
            else
            {
                // not charging → hide overlay and reset
                charge_image.fillAmount = 0f;
                charge_image.enabled = false;
            }
        }

        if (hide_when_ready)
        {
            bool evade_ready = (player.evade_cooldown <= 0f) || (player.evade_cooldown_remaining <= 0f);
            bool should_show = !evade_ready;              // show if on cooldown
            if (show_while_charging && is_charging)       // OR show if charging
                should_show = true;

            fill_image.enabled = should_show;
            if (charge_image != null && is_charging)
                charge_image.enabled = true; // ensure visible during charge
        }
    }
}