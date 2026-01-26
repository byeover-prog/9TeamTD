using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private int moveSpeed;

    private void Update()
    {
        IsMoveTest();
    }

    private void IsMoveTest()
    {
        float H = Input.GetAxisRaw("Horizontal");
        transform.Translate(Vector3.forward * moveSpeed * H * Time.deltaTime);
        
        float V = Input.GetAxisRaw("Vertical");
        transform.Translate(Vector3.right * moveSpeed * V * Time.deltaTime);
    }
}
