using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    public Material m_white_material;
    public Material m_red_material;

    private bool m_hit_by_obstacle = false;
    private float m_hit_tick;

    private const float BLEEDING_TIME = 0.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "OBSTACLE")
        {
            GameManager.Instance.HitByObstacle();

            m_hit_tick = Time.time;
            m_hit_by_obstacle = true;

            GetComponent<Renderer>().material = m_red_material;
        }
    }

    private void Update()
    {
        if(m_hit_by_obstacle)
        {
            if(Time.time >= m_hit_tick + BLEEDING_TIME)
            {
                GetComponent<Renderer>().material = m_white_material;
            }
        }
    }
}
