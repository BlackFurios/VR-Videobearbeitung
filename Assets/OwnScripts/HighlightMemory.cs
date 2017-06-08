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

    // Use this for initialization
    void Start ()
    {

    }

    // Update is called once per frame
    void Update ()
    {

    }

    public void setTime(TimeSpan val)
    {
        ts = val;
    }

    public void setType(String val)
    {
        type = val;
    }

    public void setTexPos(Vector2 val)
    {
        texPos = val;
    }

    public void setVideo(String val)
    {
        video = val;
    }

    public void setNext(GameObject val)
    {
        next = val;
    }

    public void setPrev(GameObject val)
    {
        prev = val;
    }

    public TimeSpan getTime()
    {
        return ts;
    }

    public String getType()
    {
        return type;
    }

    public Vector2 getTexPos()
    {
        return texPos;
    }

    public String getVideo()
    {
        return video;
    }

    public GameObject getNext()
    {
        return next;
    }

    public GameObject getPrev()
    {
        return prev;
    }
}
