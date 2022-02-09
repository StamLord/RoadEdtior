using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyCam : MonoBehaviour
{

    public float MainSpeed = 100.0f; //regular speed
    public float ShiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    public float MaxShift = 1000.0f; //Maximum speed when holdin gshift
    public float CamSens = 0.25f; //How sensitive it with mouse

  
    private float TotalRun = 1.0f;


    void Update()
    {

        //Keyboard commands
        float f = 0.0f;
        var p = GetBaseInput();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            TotalRun += Time.deltaTime;
            p = p * TotalRun * ShiftAdd;
            p.x = Mathf.Clamp(p.x, -MaxShift, MaxShift);
            p.y = Mathf.Clamp(p.y, -MaxShift, MaxShift);
            p.z = Mathf.Clamp(p.z, -MaxShift, MaxShift);
        }
        else
        {
            TotalRun = Mathf.Clamp(TotalRun * 0.5f, 1, 1000);
            p = p * MainSpeed;
        }

        p = p * Time.deltaTime;

        transform.position += p;
    }

    /// <summary>
    //returns the basic values, if it's 0 than it's not active.
    /// </summary>
    /// <returns></returns>
    private Vector3 GetBaseInput()
    {
        Vector3 p_Velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }

        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}
