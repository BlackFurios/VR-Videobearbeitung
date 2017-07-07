using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightMemory : MonoBehaviour
{
    [SerializeField]
    private TimeSpan        ts;             //Point curing video at which this highlight is
    [SerializeField]
    private String          type;           //What type of highlight is this (Single, Chain)
    [SerializeField]
    private Vector2         texPos;         //The position of this highlight on the video itself

    //Use this for initialization
    void Start ()
    {

    }

    //Update is called once per frame
    void Update ()
    {

    }

    //Sets the new value of the time parameter
    public void setTime(TimeSpan val)
    {
        ts = val;
    }

    //Sets the new value of the type parameter
    public void setType(String val)
    {
        type = val;
    }

    //Sets the new value of the texPos parameter
    public void setTexPos(Vector2 val)
    {
        texPos = val;
    }

    //Returns the time parameter
    public TimeSpan getTime()
    {
        return ts;
    }

    //Returns the type parameter
    public String getType()
    {
        return type;
    }

    //Returns the texPos parameter
    public Vector2 getTexPos()
    {
        return texPos;
    }
}
