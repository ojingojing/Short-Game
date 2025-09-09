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

    private float evade_cooldown = 5f;

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
            float random = Random.Range(3.0f,6.0f);

            //Debug.Log(random);

            evade_cooldown = random;
        }
        else if (this.evade_time >= evade_cooldown)
        {
            is_evading = true;
            StartCoroutine(EvadeInChosenDirection());
            this.evade_time = 0;
        }

        // if (this.evade_time >= evade_cooldown)
        // {
        //     is_evading;
        //     StartCoroutine(EvadeInChosenDirection());
        //     this.evade_time = 0;
        // }
    }

    IEnumerator EvadeInChosenDirection()
    {
        Debug.Log("Evaded after : " + evade_cooldown + " seconds.");

        yield return new WaitForSeconds(0.01f);

        is_evading = false;
        evade_cooldown = 0;
    }
}
