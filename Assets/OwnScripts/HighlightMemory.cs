using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightMemory : MonoBehaviour
{
    private TimeSpan        ts;             //Point curing video at which this highlight is
    private String          type;           //What type of highlight is this (Single, Chain)
    private Vector2         texPos;         //The position of this highlight on the video itself
    private String          video;          //The video in which this highlight is

    private GameObject      next;           //The next highlight if tis highlight is part of a chain
    private GameObject      prev;           //The previous highlight if tis highlight is part of a chain

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

    //Sets the new value of the video parameter
    public void setVideo(String val)
    {
        video = val;
    }

    //Sets the new value of the next parameter
    public void setNext(GameObject val)
    {
        next = val;
    }

    //Sets the new value of the prev parameter
    public void setPrev(GameObject val)
    {
        prev = val;
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

    //Returns the video parameter
    public String getVideo()
    {
        return video;
    }

    //Returns the next parameter
    public GameObject getNext()
    {
        return next;
    }

    //Returns the prev parameter
    public GameObject getPrev()
    {
        return prev;
    }
}
