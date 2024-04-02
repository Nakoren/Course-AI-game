using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move_player : MonoBehaviour
{
    public float moveSpeed = 30f;
        
    void Update()
    {
        Vector2 moveVector = new Vector2();

        moveVector.x = Input.GetAxis("Horizontal");
        moveVector.y = Input.GetAxis("Vertical");
        transform.Translate(moveVector * moveSpeed * Time.deltaTime, Space.World);


        if (moveVector != Vector2.zero)
        {
            transform.up = moveVector;
        }


    }
}