using UnityEngine;
using System.Collections;

public enum Evade_Direction { back = 0, left = 1, right = 2 }

public class EnemyController : MonoBehaviour
{

    public float enemy_health = 100f;

    public float enemy_dmg = 10f;

    public float enemy_evade_speed = 10f;
    public float enemy_evade_distance = 2f;

    public float enemy_edge_hold_time = 0.06f;

    private bool is_evading = false;

    Coroutine running_evade;

    private float evade_time = 0f;

    private float evade_distance = 2f;

    private float evade_cooldown = 0f;

    private int evade_direction = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.evade_time += Time.deltaTime;

        if (evade_cooldown == 0)
        {
            float random = Random.Range(1.0f,3.0f);

            //Debug.Log(random);

            evade_cooldown = random;
        }

        if (evade_direction == 0)
        {
            evade_direction = Random.Range(1,4);
        }

        else if (this.evade_time >= evade_cooldown && !is_evading)
        {
            is_evading = true;
            StartCoroutine(EvadeInChosenDirection(evade_direction));
            //Debug.Log(evade_direction);
            this.evade_time = 0;
        }

        // if (this.evade_time >= evade_cooldown)
        // {
        //     is_evading;
        //     StartCoroutine(EvadeInChosenDirection());
        //     this.evade_time = 0;
        // }
    }

    IEnumerator EvadeInChosenDirection(int direction)
    {
        is_evading = true;

        Vector3 start = transform.position;
        Vector3 target = start;

        switch (direction)
        {
            case 1:
                target.x = start.x;
                target.y = start.y + evade_distance;
                break;
            case 2:
                target.x = start.x - evade_distance;
                target.y = start.y;
                break;
            case 3:
                target.x = start.x + evade_distance;
                target.y = start.y;
                break;
        }

        Debug.Log("Evaded to " + direction +" after : " + evade_cooldown + " seconds.");

        float timeout = 0f;
        while ((transform.position - target).sqrMagnitude > 0.000001f)
        {
            float step = enemy_evade_speed * Time.deltaTime;
            Vector2 cur2 = new Vector2(transform.position.x, transform.position.y);
            Vector2 tar2 = new Vector2(target.x, target.y);
            Vector2 next2 = Vector2.MoveTowards(cur2, tar2, step);
            transform.position = new Vector3(next2.x, next2.y, transform.position.z);

            timeout += Time.deltaTime;
            if (timeout > 1.0f) break;
            yield return null;
        }

        // wait at the end for a while
        if (enemy_edge_hold_time > 0f) yield return new WaitForSeconds(enemy_edge_hold_time);

        // return
        timeout = 0f;
        while ((transform.position - start).sqrMagnitude > 0.000001f)
        {
            float step = enemy_evade_speed * Time.deltaTime;
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
        evade_cooldown = 0;
        evade_direction= 0;
    }

    public void TakeDamage(float dmg)
    {
        enemy_health -= dmg;
        Debug.Log("Got hit by player receiving : " + dmg);
    }
}
