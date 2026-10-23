using UnityEngine;

public class Boss : MonoBehaviour
{
    public Transform Target_Player;

    public float Boss_Hp = 1f;
    [SerializeField] private float Boss_Spd = 1f;

    [SerializeField] private float Boss_FireRate = 1f;
    //public int Boss_BulletType = 0;
    public int Boss_Pattern;


    void Awake()
    {
        Boss_Pattern = Random.Range(1, 6);
    }


    void Start()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Debug.Log("ujh");
            Boss_Hp -= 1;
        }
    }

    void Update()
    {

    }
}