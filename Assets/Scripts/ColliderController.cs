using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ColliderController : MonoBehaviour
{
    public Action<bool> IsGrounded;
    public Action FollowPlayer;
    private Collider2D coll;
    public bool enemyColl = false;

    void Start()
    {
        //LLamar al enemigo cuando se detecte al player
        if(enemyColl)
        {
            EnemyController enemy = transform.parent.GetComponent<EnemyController>();
            if(enemy != null)
                FollowPlayer += enemy.StartFollowing;
        }
        else
        {
            //Suscribimos el totem al evento de tocar o no el suelo
            IsGrounded += TotemController.totem.SetGrounded;
        }
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        if(enemyColl && coll.tag == Globals.tagPlayer)
        {
            FollowPlayer();
        }
        else if(!enemyColl && coll.tag == Globals.tagGround)
        {
            IsGrounded(true);
        }
    }

    void OnTriggerStay2D(Collider2D coll)
    {
        if(!enemyColl && coll.tag == Globals.tagGround)
        {
            IsGrounded(true);
        }
    }

    void OnTriggerExit2D(Collider2D coll)
    {
        if(!enemyColl && coll.tag == Globals.tagGround)
        {
            IsGrounded(false);
        }
    }
}
