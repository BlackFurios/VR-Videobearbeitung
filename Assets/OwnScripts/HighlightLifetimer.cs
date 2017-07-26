using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightLifetimer : MonoBehaviour
{
    private List<Vector3>       posList = new List<Vector3>();          //
    private List<Quaternion>    rotList = new List<Quaternion>();       //

    private int                 index = 0;                              //
    private float               lerpTime = 0.1f;                        //
    private float               currentTime = 0f;                       //

	// Use this for initialization
	void Start ()
    {

	}
	
	// Update is called once per frame
	void Update ()
    {
        //Check if the highlight has mor than one positions and enough time has passed for lerp
        if (posList.Count > 1 && currentTime <= lerpTime)
        {
            //
            currentTime += Time.deltaTime;

            //
            transform.position = Vector3.Lerp(posList[index], posList[index + 1], currentTime / lerpTime);
            transform.rotation = Quaternion.Lerp(rotList[index], rotList[index + 1], currentTime / lerpTime);
        }
        else
        {
            //
            currentTime = 0f;
        }

        //
        InvokeRepeating("NextWaypoint", 0, lerpTime);

        //
        if (index == posList.Count - 1)
        {
            //
            Destroy(this.gameObject);
        }
    }

    //
    void NextWaypoint()
    {
        //
        if (index < posList.Count - 1)
        {
            //
            index++;
        }
        else
        {
            //
            transform.position = Vector3.Lerp(posList[index], posList[index + 1], currentTime / lerpTime);
            transform.rotation = Quaternion.Lerp(rotList[index], rotList[index + 1], currentTime / lerpTime);
        }
    }

    //
    public void SetPosList(List<Vector3> initPosList)
    {
        posList = initPosList;
    }

    //
    public void SetRotList(List<Quaternion> initRotList)
    {
        rotList = initRotList;
    }
}
