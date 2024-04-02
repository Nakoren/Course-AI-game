using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    
    private Rigidbody2D rb;
    public int ricochets = 1;
    public float Speed = 10f;

    private Vector2 launch_position;
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        launch_position = this.transform.position;

        rb.velocity = transform.up * Speed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            if (ricochets > 0)
            {
                Ricochet(collision);
            }
            else
            {
                DestroyBullet();
            }
        }
    }

    private void Ricochet(Collider2D collision)
    {
        Ray2D ray = new Ray2D(transform.position, transform.up);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up);

        Vector2 inc_vect = hit.point - launch_position;
        Vector2 refl = Vector2.Reflect(inc_vect, hit.normal);
        Vector2 reflectDir = Vector2.Reflect(ray.direction, hit.normal);

        /*
        Debug.DrawRay(transform.position, hit.point);
        Debug.DrawRay(hit.point, refl, Color.green, 20f);
        Debug.DrawRay(hit.point, hit.normal, Color.cyan, 20f);
        Debug.DrawLine(transform.position, hit.point, Color.yellow, 20f);
        */

        float rot =  Mathf.Atan2(reflectDir.y,reflectDir.x) * Mathf.Rad2Deg-90;

        transform.eulerAngles = new Vector3(0, 0, rot);
        ricochets -= 1;
        rb.velocity = transform.up * Speed;
    }

    public void DestroyBullet()
    {
        Destroy(this.gameObject);
    }
}
