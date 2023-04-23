using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private Rigidbody2D rb;
    public CircleCollider2D coll;
    public Material material;
    public bool following = false;
    public float acceleration = 5;
    public float maxSpeed = 5;
    public int power = 5;
    public float detectRadius = 20;

    [ColorUsage(true, true)]
    public Color color;
    static float hueVariation = 0.022f;

    private Vector3 initPos;

    public SpriteRenderer sprite;
    public Sprite[] faces; 
    

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //Cambiar radio de deteccion
        coll.radius = detectRadius;

        //Variar offset del shader de la llama
        Renderer render = GetComponent<Renderer>();
        material = render.sharedMaterial;
        material = new Material(material);
        render.sharedMaterial = material;
        material.SetFloat("_TimeOffset", Random.Range(0,20));
        
        //Si no es invencible se cambia el color en funcion de su poder
        float h,s,v; 
        Color.RGBToHSV(color,out h,out s, out v);

        //Invencible
        if(power == 0)    
            color = Color.HSVToRGB(0.08f,s,v);
        else
            color = Color.HSVToRGB(h + hueVariation*(power-1),s,v);
        
        material.SetColor("_TintColor", color);

        //Almacenar posicion inicial para resetear
        initPos = transform.position;

        //TODO: Cambiar cara en funcion del power
        sprite.sprite = faces[power];
    }

    // Update is called once per frame
    void Update()
    {
        if(following)
        {
            FollowTotem();
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = 0;
        }
    }

    public void StartFollowing()
    {
        if(power != 0 && !following)
        {
            following = true;
            AudioManager.audioManager.Play("EnemyDetect");
        }
    }

    void FollowTotem()
    {
        Vector3 dir = TotemController.totem.transform.position - transform.position;
        rb.AddForce(dir.normalized*acceleration*Time.deltaTime, ForceMode2D.Impulse);
        if(rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        //Flip en funcion de la direccion
        if(rb.velocity.x > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    //Detectamos choques con el totem
    void OnCollisionEnter2D(Collision2D col)
    {
        if(col.collider.tag == Globals.tagPlayer)
        {
            Debug.Log("COLISION ENEMIGO");
            //Comprobar si daña al jugador
            TotemController.totem.EnemyHit(power);
            Dead();
        }
    }

    void Dead()
    {
        if(power != 0)
        {
            Destroy(gameObject);
            AudioManager.audioManager.Play("KillEnemy");
            //TODO:PARTICULAS
        }
    }

    public void Reset()
    {
        transform.position = initPos;
        following = false;
    }
}
