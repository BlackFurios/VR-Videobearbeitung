using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightLifetimer : MonoBehaviour
{
    private List<Vector3>       posList = new List<Vector3>();          //List of all world positions of the represented highlight
    private List<Quaternion>    rotList = new List<Quaternion>();       //List of all world rotations of the represented highlight
    private List<TimeSpan>      timeList = new List<TimeSpan>();        //List of all time positions of the represented highlight

    private int                 index = 0;                              //The current index at which point this highlight representation is in the lists
    //private float               lerpTime = 0.1f;                        //The time period in seconds in which the object moves/rotates between two waypoints
    private float               currentTime = 0f;                       //The current time during the lerpTime

	// Use this for initialization
	void Start ()
    {
        Debug.Log("Highlight ab " + Time.time + " erzeugt.");

        //Starts the highlight movement
        StartCoroutine(MoveHighlight());
    }
	
	// Update is called once per frame
	void Update ()
    {
        
    }

    //Manages the path of the highlight object
    IEnumerator MoveHighlight ()
    {
        //Infinite loop
        while (true)
        {
            //Set the current needed time for the lerp to the next time interval in milliseconds
            currentTime = timeList[index + 1].Subtract(timeList[index]).Milliseconds / 1000;
            
            //Starts the transformation of the highlight object via lerp
            yield return StartCoroutine(LerpToTransform());

            //Check if the indey is the last possible index of the list
            if (index >= posList.Count - 2)
            {
                //Destroys the highlight object
                Destroy(this.gameObject);
            }

            //Increase the index by one
            index++;

            //Wait for the time period the lerp needs to transform the highlight object
            yield return new WaitForSeconds(currentTime);
        }
    }

    //Transforms (moves, rotates) the highlight object
    IEnumerator LerpToTransform ()
    {
        float timeStep = 0.0f;

        //Loop until the lerp is completed
        while (timeStep < 1.0f)
        {
            //Add the next step in the lerp
            timeStep += Time.deltaTime / currentTime;

            //Lerp the position and rotation between the current and next transform
            transform.position = Vector3.Lerp(posList[index], posList[index + 1], timeStep);
            transform.rotation = Quaternion.Lerp(rotList[index], rotList[index + 1], timeStep);
            yield return null;
        }
    }
    
    //Sets the given world position list of the highlight as variable
    public void SetPosList(List<Vector3> initPosList)
    {
        posList = initPosList;
    }

    //Sets the given world rotation list of the highlight as variable
    public void SetRotList(List<Quaternion> initRotList)
    {
        rotList = initRotList;
    }

    //Sets the given world rotation list of the highlight as variable
    public void SetTimeList(List<TimeSpan> initTimeList)
    {
        timeList = initTimeList;
    }
}
