using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightLifetimer : MonoBehaviour
{
    float             maxLifeTime         = 0.75f;          //The maximal time in seconds this highlight exists
    float             currentLifeTime     = 0;              //The current time in seconds this highlight exists

    Color color;                                            //Color variable to define the new opacity value

	//Use this for initialization
	void Start ()
    {
        //Set the initial color the current color (including opacity)
        color = GetComponent<MeshRenderer>().material.color;

        //Destroy this highlight after its maximal life time
        Destroy(this.gameObject, maxLifeTime);
    }
	
	//Update is called once per frame
	void Update ()
    {
        //Add the deltaTime to the current life time of this highlight
        currentLifeTime += Time.deltaTime;

        //Change the opacity of the new color according to the life time
        color.a = Mathf.Lerp(1f, 0f, currentLifeTime / maxLifeTime);

        //Change this highlights color with the new color
        GetComponent<MeshRenderer>().material.color = color;
    }
}
