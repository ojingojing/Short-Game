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

    public float attack_cooldown_multiplier = 1.5f;

    public float max_attack_charge_time = 0.8f;   // max charge time
	public float charge_damage_factor = 1.0f;     // +100% when fully charged
	public float attack_cooldown = 0.25f;

    public float evade_total_lockout = 0f;           // total locked time for this evade
    public bool  IsChargingAttack => is_charging_attack;
    public float AttackChargeTime => attack_charge_time;

    private bool is_evading = false;
    Coroutine running_evade;
    Coroutine running_attack;

    bool is_attacking = false;
    bool is_charging_attack = false;
	float attack_charge_time = 0f;

    public float evade_cooldown_remaining = 0f;
    public bool IsEvadeOnCoolDown => evade_cooldown_remaining > 0f;

    //public float evade_total_lockout = 0f; 
    public bool CanEvadeNow => evade_cooldown_remaining <= 0f;

    public Sprite idle_sprite;
    public Sprite evade_sprite;
    public Sprite crouch_sprite;

    public Sprite attack_sprite;
    private SpriteRenderer sprite_renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sprite_renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerMovementControl();
        PlayerAttackControl();
    }

    #region Evade / Move

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

    public void TryEvade(Evade_Direction dir)
    {
        if (is_evading == true) return;
        if (evade_cooldown_remaining > 0f) return;
        if (running_evade != null) StopCoroutine(running_evade);
        running_evade = StartCoroutine(EvadeInChosenDirection(dir));
    }

    
    IEnumerator EvadeInChosenDirection(Evade_Direction direction)
    {
        is_evading = true;

        Vector3 start = transform.position;
        Vector3 target = start;

        //change sprite to evade sprite based on dir
        sprite_renderer.sprite = evade_sprite;
        if (direction == Evade_Direction.left)  sprite_renderer.flipX = true;
        if (direction == Evade_Direction.right) sprite_renderer.flipX = false;
        if (direction == Evade_Direction.back) sprite_renderer.sprite = crouch_sprite;

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

        float speed = Mathf.Max(0.0001f, player_evade_speed);
        float t_go = evade_distance / speed;
        float t_back = t_go;

        float hold = Mathf.Max(0f, edge_hold_time);     
        float base_cd = Mathf.Max(0f, evade_cooldown);  

        evade_total_lockout = t_go + hold + t_back + base_cd;

        float end_time = Time.time + evade_total_lockout;
        evade_cooldown_remaining = evade_total_lockout;

        while ((transform.position - target).sqrMagnitude > 0.000001f)
        {
            float step = speed * Time.deltaTime;
            Vector2 cur2 = new Vector2(transform.position.x, transform.position.y);
            Vector2 tar2 = new Vector2(target.x, target.y);
            Vector2 next2 = Vector2.MoveTowards(cur2, tar2, step);
            transform.position = new Vector3(next2.x, next2.y, transform.position.z);

            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);
            yield return null;
        }

        if (hold > 0f)
        {
            float hold_end = Time.time + hold;
            while (Time.time < hold_end)
            {
                evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);
                yield return null;
            }
        }

        while ((transform.position - start).sqrMagnitude > 0.000001f)
        {
            float step = speed * Time.deltaTime;
            Vector2 cur2 = new Vector2(transform.position.x, transform.position.y);
            Vector2 start2 = new Vector2(start.x, start.y);
            Vector2 next2 = Vector2.MoveTowards(cur2, start2, step);
            transform.position = new Vector3(next2.x, next2.y, transform.position.z);

            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);
            yield return null;
        }


        // return to normal sprite
        sprite_renderer.sprite = idle_sprite;
        sprite_renderer.flipX = false;

        while (Time.time < end_time)
        {
            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);
            yield return null;
        }

        evade_cooldown_remaining = 0f;
        is_evading = false;
    }

    #endregion

    #region  Attack

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

    public void TryAttack(float charge_ratio)
	{
		if (is_evading || is_attacking) return;
        if (evade_cooldown_remaining > 0f) return;
		if (running_attack != null) StopCoroutine(running_attack);
		running_attack = StartCoroutine(AttackRoutine(charge_ratio));
	}

	IEnumerator AttackRoutine(float charge_ratio)
    {
        is_attacking = true;

        sprite_renderer.sprite = attack_sprite;

        //transform.localScale = new Vector3(0.2f,0.2f,1f); // bigger size for smaller img

        // compute damage with charge (example scaling) – unchanged
        float final_damage = player_dmg * (1f + charge_ratio * charge_damage_factor);

        // same travel pattern as evade (forward then return)
        Vector3 start = transform.position;
        Vector3 target = new Vector3(start.x, start.y + evade_distance, start.z);

        // compute motion times like evade does
        float speed = Mathf.Max(0.0001f, player_evade_speed);
        float t_go = evade_distance / speed;
        float t_back = t_go;

        // base cooldown for attack = 1.5x of evade cooldown
        float base_cd_attack = Mathf.Max(0f, evade_cooldown * attack_cooldown_multiplier);

        // total lockout for attack (motion + base cooldown)
        float attack_total_lockout = t_go + t_back + base_cd_attack;

        // start shared lock immediately (so evade also blocked)
        float end_time = Time.time + attack_total_lockout;
        evade_cooldown_remaining = attack_total_lockout;

        // go forward
        float timeout = 0f;
        while ((transform.position - target).sqrMagnitude > 0.000001f)
        {
            float step = speed * Time.deltaTime;
            float new_y = Mathf.MoveTowards(transform.position.y, target.y, step);
            transform.position = new Vector3(transform.position.x, new_y, transform.position.z);

            // update shared lock
            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);

            timeout += Time.deltaTime;
            if (timeout > 1.0f) break;
            yield return null;
        }

        // return to start
        timeout = 0f;
        while ((transform.position - start).sqrMagnitude > 0.000001f)
        {
            float step = speed * Time.deltaTime;
            float new_y = Mathf.MoveTowards(transform.position.y, start.y, step);
            transform.position = new Vector3(transform.position.x, new_y, transform.position.z);

            // update shared lock
            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);

            timeout += Time.deltaTime;
            if (timeout > 1.0f) break;
            yield return null;
        }

        sprite_renderer.sprite = idle_sprite;
        //transform.localScale = new Vector3(0.15f,0.15f,1f);

        // keep lock for the remaining base cooldown window
        while (Time.time < end_time)
        {
            evade_cooldown_remaining = Mathf.Max(0f, end_time - Time.time);
            yield return null;
        }

        // release
        evade_cooldown_remaining = 0f;
        is_attacking = false;
    }

    #endregion

    #region Collision

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (is_attacking == true && collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(player_dmg);
            }
        }
    }

    #endregion
}

