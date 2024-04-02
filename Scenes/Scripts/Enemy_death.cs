using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_death : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Friendly_projectile")
        {
            Console.WriteLine("collision");
            Destroy(collision.gameObject);
            Destroy(this.gameObject);
        }
    }
}
