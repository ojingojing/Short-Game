using UnityEngine;
using System.Collections;

//public enum Evade_Direction { back = 0, left = 1, right = 2 }

public class PlayerController : MonoBehaviour
{
    public float player_health = 100f;

    public float player_dmg = 10f;

    public float player_evade_speed= 10f;
    public float evade_distance = 2f;

    public float edge_hold_time = 0.06f;
    public float evade_cooldown = 0.15f;

    public float max_attack_charge_time = 0.8f;   // max charge time
	public float charge_damage_factor = 1.0f;     // +100% when fully charged
	public float attack_cooldown = 0.25f;

    private bool is_evading = false;
    Coroutine running_evade;
    Coroutine running_attack;

    bool is_attacking = false;
    bool is_charging_attack = false;
	float attack_charge_time = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        PlayerMovementControl();
        PlayerAttackControl();
    }

    void PlayerMovementControl()
    {
        Evade_Direction? chosen_dir = null;

        if (Input.GetKeyDown(KeyCode.A))       chosen_dir = Evade_Direction.left;
		else if (Input.GetKeyDown(KeyCode.D))  chosen_dir = Evade_Direction.right;
		else if (Input.GetKeyDown(KeyCode.S))  chosen_dir = Evade_Direction.back;

        if (chosen_dir != null)
        {
            if (is_evading)
            {
                //Debug.Log("[Evade] ignored: already evading");
                return;
            }

            if (is_attacking)
            {
                return;
            }

            //Debug.Log("[Evade] start: " + chosen_dir.Value);
            TryEvade(chosen_dir.Value);
        }
    }

    void PlayerAttackControl()
	{
		// start charging
		if (Input.GetMouseButtonDown(0))
		{
			if (is_evading || is_attacking) return; // cannot start while busy
			is_charging_attack = true;
			attack_charge_time = 0f;
			// TODO: trigger charge start animation/sfx
		}

		// accumulate charge while holding
		if (is_charging_attack && Input.GetMouseButton(0))
		{
			attack_charge_time += Time.deltaTime;
			if (attack_charge_time > max_attack_charge_time)
			{
				attack_charge_time = max_attack_charge_time;
			}
			// Optional: show UI charge bar using (attack_charge_time / max_attack_charge_time)
		}

		// release to attack
		if (is_charging_attack && Input.GetMouseButtonUp(0))
		{
			float charge_ratio = 0f;
			if (max_attack_charge_time > 0f)
			{
				charge_ratio = attack_charge_time / max_attack_charge_time;
				if (charge_ratio < 0f) charge_ratio = 0f;
				if (charge_ratio > 1f) charge_ratio = 1f;
			}

			is_charging_attack = false;
			TryAttack(charge_ratio);
		}
	}

    public void TryEvade(Evade_Direction dir)
    {
        if (is_evading == true) return;
        if (running_evade != null) StopCoroutine(running_evade);
        running_evade = StartCoroutine(EvadeInChosenDirection(dir));
    }

    
    IEnumerator EvadeInChosenDirection(Evade_Direction direction)
    {
        is_evading = true;

        Vector3 start = transform.position;
        Vector3 target = start;

        switch (direction)
        {
            case Evade_Direction.back:
                target.x = start.x;
                target.y = start.y - evade_distance;
                break;

            case Evade_Direction.left:
                target.x = start.x - evade_distance;
                target.y = start.y;   
                break;

            case Evade_Direction.right:
                target.x = start.x + evade_distance;
                target.y = start.y;   
                break;
        }

        // go to target
        float timeout = 0f;
        while ((transform.position - target).sqrMagnitude > 0.000001f)
        {
            float step = player_evade_speed * Time.deltaTime;
            Vector2 cur2 = new Vector2(transform.position.x, transform.position.y);
            Vector2 tar2 = new Vector2(target.x, target.y);
            Vector2 next2 = Vector2.MoveTowards(cur2, tar2, step);
            transform.position = new Vector3(next2.x, next2.y, transform.position.z);

            timeout += Time.deltaTime;
            if (timeout > 1.0f) break;
            yield return null;
        }

        // wait at the end for a while
        if (edge_hold_time > 0f) yield return new WaitForSeconds(edge_hold_time);

        // return
        timeout = 0f;
        while ((transform.position - start).sqrMagnitude > 0.000001f)
        {
            float step = player_evade_speed * Time.deltaTime;
            Vector2 cur2 = new Vector2(transform.position.x, transform.position.y);
            Vector2 start2 = new Vector2(start.x, start.y);
            Vector2 next2 = Vector2.MoveTowards(cur2, start2, step);
            transform.position = new Vector3(next2.x, next2.y, transform.position.z);

            timeout += Time.deltaTime;
            if (timeout > 1.0f) break; 
            yield return null;
        }

        // Cooldown lockout (optional)
        if (evade_cooldown > 0f) yield return new WaitForSeconds(evade_cooldown);

        is_evading = false;
    }

    public void TryAttack(float charge_ratio)
	{
		if (is_evading || is_attacking) return;
		if (running_attack != null) StopCoroutine(running_attack);
		running_attack = StartCoroutine(AttackRoutine(charge_ratio));
	}

	IEnumerator AttackRoutine(float charge_ratio)
	{
		is_attacking = true;

		// compute damage with charge (example scaling)
		float final_damage = player_dmg * (1f + charge_ratio * charge_damage_factor);

		// forward (+X) then return, using same speed and same distance as evade
		Vector3 start = transform.position;
		Vector3 target = new Vector3(start.x, start.y + evade_distance, start.z);

		// TODO: trigger attack start animation (wind-up depends on charge_ratio if desired)

		// go forward
		float timeout = 0f;
		while ((transform.position - target).sqrMagnitude > 0.000001f)
		{
			float step = player_evade_speed * Time.deltaTime; // same speed as evade
			float new_y = Mathf.MoveTowards(transform.position.y, target.y, step);
			transform.position = new Vector3(transform.position.x, new_y, transform.position.z);

			timeout += Time.deltaTime;
			if (timeout > 1.0f) break;
			yield return null;
		}

		// return to start
		timeout = 0f;
		while ((transform.position - start).sqrMagnitude > 0.000001f)
		{
			float step = player_evade_speed * Time.deltaTime; // same speed
			float new_y = Mathf.MoveTowards(transform.position.y, start.y, step);
			transform.position = new Vector3(transform.position.x, new_y, transform.position.z);

			timeout += Time.deltaTime;
			if (timeout > 1.0f) break;
			yield return null;
		}

		// cooldown after attack
		if (attack_cooldown > 0f) yield return new WaitForSeconds(attack_cooldown);

		is_attacking = false;
	}
}

