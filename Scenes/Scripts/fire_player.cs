using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class fire_player : MonoBehaviour
{
    public float fireCooldown;
    private float currentCooldown;
    public GameObject ammoProjectile;
    
    void Update()
    {
        if (currentCooldown > 0)
        {   
             currentCooldown -= Time.deltaTime;
        }
        if (Input.GetMouseButton(0)){
            if (currentCooldown <= 0)
            {
                fire();
            }
        }
    }
    void fire()
    {
        Vector3 diference = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float rotateZ = Mathf.Atan2(diference.y, diference.x) * Mathf.Rad2Deg;
        Instantiate(ammoProjectile, transform.position, Quaternion.Euler(0f, 0f, rotateZ - 90));
        currentCooldown = fireCooldown;
    }
}
