using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum GameState
{
    NAVIGATING,
    DROPPING,
    DEAD,
};

public class GameManager : MonoBehaviour {
    private GameState m_gs = GameState.NAVIGATING;

    public GameObject m_tile_prefab;
    public GameObject m_obstacle_prefab;
    public List<GameObject> m_tiles;
    private Transform m_target_tr;
    private Transform m_camera_tr;
    private GameObject m_player_obj;
    private Transform m_player_tr;
    private Transform m_dead_zone_tr;

    public Text m_score_text;
    public Slider m_health_slider;
    public RectTransform m_touch_pad_tr;
    public RectTransform m_touch_bar_rect_tr;

    private const float MOVE_TARGET_FACTOR = 100f;
    private const float MOVE_TARGET_SPEED = 3f;
    private const float DISTANCE_TO_DESTROY_TITLE = 4f;
    private Vector3 TILE_OFFSET = new Vector3(0f, 0f, 1f);

    private const float PLAYER_MOVE_SPEED = 5f;
    private const float PLAYER_ELASTIC_FACTOR = 100f;

    private Vector3 CAMERA_OFFSET = new Vector3(0f, 3.25f, -1.5f);
    private const float CAMERA_ELASTIC_FACTOR = 100f;

    private const int MAX_OBSTACLE_COUNT = 3;

    private float[] obstacle_x_positions = new float[] { -4.5f, -3.5f, -2.5f, -1.5f, -0.5f, 0.5f, 1.5f, 2.5f, 3.5f, 4.5f };

    private const float MIN_PLAYER_NAVIGATABLE_POS = -5.5f;
    private const float MAX_PLAYER_NAVIGATABLE_POS = 5.5f;

    private Vector3 m_player_last_pos;

    private const float DEADZONE_DISTANCE = 20.0f;

    private int m_player_hp = 10;

    private bool m_is_touch_pad_pressed = false;
    private Vector3 m_touch_pad_pressed_pos;
    private Vector3[] m_touch_pad_position = new Vector3[4];

    private float TOUCH_BAR_PIXEL_LEFT = 0.0f;
    private float TOUCH_BAR_PIXEL_RIGHT = 0.0f;
    private float TOUCH_BAR_PIXEL_WIDTH = 1f;

    private float TOUCH_PAD_ANCHORED_CENTER = 0.0f;
    private float TOUCH_PAD_ANCHORED_LEFT = 0.0f;
    private float TOUCH_PAD_ANCHORED_RIGHT = 0.0f;

    private float m_touch_pad_value = 0f;

    // Use this for initialization
    void Start () {
        m_target_tr = GameObject.FindGameObjectWithTag("TARGET").GetComponent<Transform>();
        m_camera_tr = Camera.main.GetComponent<Transform>();

        m_player_obj = GameObject.FindGameObjectWithTag("Player");
        m_player_tr = m_player_obj.GetComponent<Transform>();
        m_player_last_pos = m_player_tr.position;

        m_dead_zone_tr = GameObject.FindGameObjectWithTag("DEAD_ZONE").GetComponent<Transform>();

        MovePlayer(0.0f);
        MoveDeadZone();
        MoveCamera();

        m_health_slider.maxValue = m_player_hp;
        m_health_slider.value = m_player_hp;

        m_touch_bar_rect_tr.GetWorldCorners(m_touch_pad_position);
        TOUCH_BAR_PIXEL_LEFT = m_touch_pad_position[0].x;
        TOUCH_BAR_PIXEL_RIGHT = m_touch_pad_position[2].x;
        TOUCH_BAR_PIXEL_WIDTH = TOUCH_BAR_PIXEL_RIGHT - TOUCH_BAR_PIXEL_LEFT;
        // Debug.Log(m_touch_pad_position[0]);
        // Debug.Log(m_touch_pad_position[1]);
        // Debug.Log(m_touch_pad_position[2]);
        // Debug.Log(m_touch_pad_position[3]);
        Debug.Log(TOUCH_BAR_PIXEL_WIDTH);

        float touch_bar_width = m_touch_bar_rect_tr.sizeDelta.x;
        Debug.Log(touch_bar_width);

        TOUCH_PAD_ANCHORED_CENTER = m_touch_pad_tr.anchoredPosition.x;
        TOUCH_PAD_ANCHORED_LEFT = TOUCH_PAD_ANCHORED_CENTER - touch_bar_width * 0.5f;
        Debug.Log(TOUCH_PAD_ANCHORED_LEFT);
        TOUCH_PAD_ANCHORED_RIGHT = TOUCH_PAD_ANCHORED_CENTER + touch_bar_width * 0.5f;
        Debug.Log(TOUCH_PAD_ANCHORED_RIGHT);
    }

    // Update is called once per frame
    void Update () {
        if(m_is_touch_pad_pressed)
        {
            float touch_pad_pos = Mathf.Clamp(Input.mousePosition.x, TOUCH_BAR_PIXEL_LEFT, TOUCH_BAR_PIXEL_RIGHT);
            touch_pad_pos -= TOUCH_BAR_PIXEL_LEFT;
            touch_pad_pos /= TOUCH_BAR_PIXEL_WIDTH;

            float anchor_x = TOUCH_PAD_ANCHORED_LEFT + (TOUCH_PAD_ANCHORED_RIGHT - TOUCH_PAD_ANCHORED_LEFT) * touch_pad_pos;
            m_touch_pad_tr.anchoredPosition = new Vector2(anchor_x, m_touch_pad_tr.anchoredPosition.y);

            m_touch_pad_value = (touch_pad_pos - 0.5f) * 2f;
        }
        else
        {
            m_touch_pad_tr.anchoredPosition = new Vector2(TOUCH_PAD_ANCHORED_CENTER, m_touch_pad_tr.anchoredPosition.y);
            m_touch_pad_value = 0f;
        }

        float h = 0f;

        if (m_gs == GameState.NAVIGATING)
        {
            h = GetHorizontalAxis();
        }

        if (m_gs != GameState.DEAD)
        {
            Vector3 target_pos = m_target_tr.position + m_target_tr.forward * MOVE_TARGET_SPEED * Time.deltaTime; ;
            m_target_tr.position = Vector3.Lerp(m_target_tr.position, target_pos, MOVE_TARGET_FACTOR * Time.deltaTime);
        }

        MovePlayer(h);
        MoveDeadZone();
        MoveCamera();

        List<GameObject> to_detroy_titles = new List<GameObject>();

        foreach(GameObject tile in m_tiles)
        {
            // Debug.Log(tile.transform.position.z);

            float distance_tile_to_target = m_target_tr.position.z - tile.transform.position.z;
            if(distance_tile_to_target >= DISTANCE_TO_DESTROY_TITLE)
            {
                to_detroy_titles.Add(tile);
            }
            else
            {
                break;
            }
        }

        foreach(GameObject to_destroy_tile in to_detroy_titles)
        {
            m_tiles.Remove(to_destroy_tile);
            Destroy(to_destroy_tile);

            Transform tail_tile_tr = m_tiles[m_tiles.Count - 1].GetComponent<Transform>();

            Vector3 new_tile_pos = tail_tile_tr.position + TILE_OFFSET;

            GameObject tile = Instantiate(m_tile_prefab, new_tile_pos, Quaternion.identity);

            int obstacle_count = Random.Range(1, MAX_OBSTACLE_COUNT + 1);
            HashSet<float> obstacle_xs = new HashSet<float>();
            while(obstacle_xs.Count < obstacle_count)
            {
                float x = obstacle_x_positions[Random.Range(0, obstacle_x_positions.Length)];
                if (obstacle_xs.Contains(x)) continue;
                obstacle_xs.Add(x);

                Vector3 obstacle_pos = new Vector3(x, 0.5f, new_tile_pos.z);
                GameObject obstacle = Instantiate(m_obstacle_prefab, obstacle_pos, Quaternion.identity);

                obstacle.transform.parent = tile.transform;
            }

            m_tiles.Add(tile);
        }

        m_score_text.text = string.Format("{0:F01}", m_player_tr.position.z);
    }

    void MovePlayer(float h)
    {
        if (m_gs == GameState.NAVIGATING)
        {
            float player_x = m_player_tr.position.x + h * PLAYER_MOVE_SPEED * Time.deltaTime;
            Vector3 player_target_position = new Vector3(player_x, 0.25f, m_target_tr.position.z);
            m_player_tr.position = Vector3.Lerp(m_player_tr.position, player_target_position, Time.deltaTime * PLAYER_ELASTIC_FACTOR);

            if (m_player_tr.position.x < MIN_PLAYER_NAVIGATABLE_POS || m_player_tr.position.x > MAX_PLAYER_NAVIGATABLE_POS)
            {
                m_gs = GameState.DROPPING;

                m_player_obj.GetComponent<Rigidbody>().isKinematic = false;

                Vector3 player_velocity = (m_player_tr.position - m_player_last_pos) / Time.deltaTime;
                m_player_obj.GetComponent<Rigidbody>().velocity = player_velocity;
            }
            else
            {
                m_player_last_pos = m_player_tr.position;
            }
        }
    }

    void MoveCamera()
    {
        Vector3 camera_target_positon = m_player_tr.position + CAMERA_OFFSET;
        // Quaternion camera_target_rotation = Quaternion.LookRotation(m_player_tr.position - m_camera_tr.position);

        m_camera_tr.position = Vector3.Lerp(m_camera_tr.position, camera_target_positon, CAMERA_ELASTIC_FACTOR * Time.deltaTime);
        // m_camera_tr.rotation = Quaternion.Slerp(m_camera_tr.rotation, camera_target_rotation, CAMERA_ELASTIC_FACTOR * Time.deltaTime);

        // Debug.Log(m_camera_tr.position + "," + m_camera_tr.rotation);
    }

    void MoveDeadZone()
    {
        if (m_gs == GameState.NAVIGATING)
        {
            Vector3 deadzone_pos = m_player_tr.position - m_player_tr.up * DEADZONE_DISTANCE;

            m_dead_zone_tr.position = deadzone_pos;
        }
    }

    public void HitByObstacle()
    {
        --m_player_hp;
        m_health_slider.value = m_player_hp;

        if (m_player_hp == 0)
        {
            m_gs = GameState.DEAD;
        }
    }

    public void Dead()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnTouchPadPressed()
    {
        m_is_touch_pad_pressed = true;
        m_touch_pad_pressed_pos = Input.mousePosition;
    }

    public void OnTouchPadReleased()
    {
        m_is_touch_pad_pressed = false;
    }

    float GetHorizontalAxis()
    {
        if(m_is_touch_pad_pressed)
        {
            return m_touch_pad_value;
        }
        else
        {
            return Input.GetAxis("Horizontal");
        }
    }

    private static GameManager s_instance;
    public static GameManager Instance
    {
        get
        {
            return s_instance;
        }
    }

    private void Awake()
    {
        s_instance = this;
    }
}
