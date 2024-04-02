using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Death_player : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Enemy_projectile")
        {
            Global.Game_status = false;
            Destroy(collision.gameObject);
            Destroy(this.gameObject);
        }   
    }
}
