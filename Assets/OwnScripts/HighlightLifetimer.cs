using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightLifetimer : MonoBehaviour
{
    private List<Vector3>       posList = new List<Vector3>();          //List of all world positions of the represented highlight
    private List<Quaternion>    rotList = new List<Quaternion>();       //List of all world rotations of the represented highlight

    private int                 index = 0;                              //The current index at which point this highlight representation is in the lists
    private float               lerpTime = 0.1f;                        //The time period in seconds in which the object moves/rotates between two waypoints
    private float               currentTime = 0f;                       //The current time during the lerpTime (max value -> 100ms)

	// Use this for initialization
	void Start ()
    {
        Debug.Log("Highlight ab " + Time.time + " erzeugt.");

        //Define the next waypoints every 100ms
        InvokeRepeating("NextWaypoint", 0, lerpTime);
    }
	
	// Update is called once per frame
	void Update ()
    {
        //Check if the highlight has mor than one positions and enough time has passed for lerp
        if (posList.Count > 1 && currentTime <= lerpTime)
        {
            //Add the passed time since last frame to the current time
            currentTime += Time.deltaTime;

            //Move and rotate the gameObject through lerp over a time of 100ms
            transform.position = Vector3.Lerp(posList[index], posList[index + 1], currentTime / lerpTime);
            transform.rotation = Quaternion.Lerp(rotList[index], rotList[index + 1], currentTime / lerpTime);
        }
        else
        {
            //Reset the current time to zero to restart the lerp movements
            currentTime = 0f;
        }
    }

    //Sets the index to index of the next waypoint
    void NextWaypoint()
    {
        //Check if the current index is a possible value in the list
        if (index < posList.Count - 2)
        {
            //Increase the index by one
            index++;
        }
        else
        {
            //Move and rotate the gameObject through lerp over a time of 100ms
            transform.position = Vector3.Lerp(posList[index], posList[index + 1], currentTime / lerpTime);
            transform.rotation = Quaternion.Lerp(rotList[index], rotList[index + 1], currentTime / lerpTime);

            Debug.Log("Highlight um " + Time.time + " gelöscht.");

            //Destroy this gameObject
            Destroy(this.gameObject);
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
}
