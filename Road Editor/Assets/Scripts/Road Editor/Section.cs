using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Section
{
    public int from {get; private set;}
    public int to  {get; private set;}
    public int index  {get; private set;}
    public GameObject worldObject {get; private set;}

    public Section(int from, int to, int index, GameObject worldObject = null)
    {
        this.from = from;
        this.to = to;
        this.index = index;
        SetWorldObject(worldObject);
    }

    public void SetWorldObject(GameObject gameObject)
    {
        this.worldObject = gameObject;
        if(worldObject != null)
            this.worldObject.name = "Section " + index;
    }

    public void DestroySection()
    {
        //Debug.Log("Section " + index + " Destroying " + worldObject);
        GameObject.Destroy(worldObject);
    }
}
