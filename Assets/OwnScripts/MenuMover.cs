using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMover : MonoBehaviour
{
    Camera      cam;                //The main camera (player view)
    float       angle;              //The angle which the menu needs to rotate around to align with the main camera
    float       prevAngle;          //The angle in the previous frame (Shows a movement)

	// Use this for initialization
	void Start ()
    {
        cam = Camera.main;

        angle = cam.transform.rotation.eulerAngles.y;
        prevAngle = cam.transform.rotation.eulerAngles.y;
    }
	
	// Update is called once per frame
	void Update ()
    {
        //Set the angle to the current euler angle of the camera
        angle = cam.transform.rotation.eulerAngles.y;

        //transform the menu to look look at the camera
        transform.RotateAround(Vector3.zero, Vector3.up, angle - prevAngle);

        //Check if the camera was rotated
        if (prevAngle != angle)
        {
            //Set the previous angle to the current angle
            prevAngle = angle;
        }
    }

    //Sets the angle and prevAngle to the current camera look rotation
    public void CenterMenu()
    {
        angle = cam.transform.rotation.eulerAngles.y;
        prevAngle = cam.transform.rotation.eulerAngles.y;
    }
}
