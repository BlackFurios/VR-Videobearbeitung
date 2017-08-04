using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ManageHighlights : MonoBehaviour
{
    private MediaPlayer             mp;                                         //Instance of the MediaPlayer script
    private SaveData                sd;                                         //Instance of the SaveData script

    private Canvas                  vrMenu;                                     //Instance of the VRMenu object

    private List<Highlight>         hList = new List<Highlight>();              //List of all managed highlights

    [SerializeField]
    private  GameObject             highLight;                                  //Highlight prefab to be managed

    private bool                    textShown = false;                          //Is currently a text shown
    private int                     showTime = 1;                               //How long texts should be shown in seconds

    public class Highlight
    {
        private List<Vector3>       pos = new List<Vector3>();                  //List of all world positions of the highlight
        private List<Quaternion>    rot = new List<Quaternion>();               //List of all world rotations of the highlight
        private List<Vector2>       texPos = new List<Vector2>();               //List of all texture positions of the highlight
        private List<TimeSpan>      time = new List<TimeSpan>();                //List of all time positions of the highlight
        private String              type;                                       //The type of the highlight
        private int                 index;                                      //

        public Highlight(List<Vector3> initPos, List<Quaternion> initRot, 
            List<Vector2> initTexPos, List<TimeSpan> initTime, String initType) //Constructor of the Highlight class
        {
            pos = initPos.ToList();
            rot = initRot.ToList();
            texPos = initTexPos.ToList();
            time = initTime.ToList();
            type = initType;

            index = 0;
        }

        //Returns the world position of the highlight
        public List<Vector3> getPos()
        {
            return pos;
        }

        //Returns the world rotation of the highlight
        public List<Quaternion> getRot()
        {
            return rot;
        }

        //Returns the texture position of the highlight
        public List<Vector2> getTexPos()
        {
            return texPos;
        }

        //Returns the time of the highlight
        public List<TimeSpan> getTime()
        {
            return time;
        }

        //Returns the type of the highlight
        public String getType()
        {
            return type;
        }

        //Return the current index of the highlight
        public int getIndex()
        {
            return index;
        }

        //Sets the current index of the highlight
        public void setIndex(int val)
        {
            index = val;
        }
    };

    //Use this for initialization
    void Start ()
    {
        //Sets the mediaPlayer script
        mp = GetComponent<MediaPlayer>();

        //Search for VRMenu and highlightMenu
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            //Check if the currently found canvas is the VRMenu
            if (c.name == "VRMenu")
            {
                //Sets the VRMenu
                vrMenu = c;
            }
        }
    }
	
	//Update is called once per frame
	void Update ()
    {
        //Iterate through the list of all highlights
        foreach (Highlight h in hList)
        {
            //Check if the player time is maximal 1 second after the highlights time position
            if (mp.GetCurrentPos() > h.getTime()[h.getIndex()] && mp.GetCurrentPos() < h.getTime()[h.getIndex()].Add(TimeSpan.FromSeconds(1)))
            {
                //Check if this highlight has no world positions (import from edl file)
                if (h.getPos().Count == 0)
                {
                    //Show text to user to give feedback
                    StartCoroutine(ShowTextForTime("!!!"));
                }
                else
                {
                    //Check if the end of the world position list is reached
                    if (h.getIndex() == (h.getPos().Count - 1))
                    {
                        //Reset the index variable
                        h.setIndex(0);
                    }
                    else
                    {
                        //Spawn a new highlight gameObject with the current position and rotation
                        SpawnHighlight(h.getPos()[h.getIndex()], h.getRot()[h.getIndex()]);

                        //Increase the highlights index by one
                        h.setIndex(h.getIndex() + 1);
                    }
                }
            }
        }
    }

    //Public function to create and add new highlight to video
    public void AddItem(List<Vector3> pos, List<Quaternion> rot, List<Vector2> texPos, List<TimeSpan>  ts, String type)
    {
        //Set current to the newly spawned highlight object
        Highlight current = new Highlight(pos, rot, texPos, ts, type);

        //Add new highlight to list of managed highlights
        hList.Add(current);
    }

    //Spawns highlight with if no other highlight collides with it
    public GameObject SpawnHighlight(Vector3 pos, Quaternion rot)
    {
        //Spawn the highlight
        return Instantiate(highLight, pos, rot);
    }

    //Calculates the correct world position for highlights of a given point
    public Vector3 CalculateHighlightPosition(Vector3 pos)
    {
        //Calculate the position of this highlight
        Vector3 spawnPos = pos;
        spawnPos.x = spawnPos.x * 0.8f;
        spawnPos.y = spawnPos.y * 0.8f;
        spawnPos.z = spawnPos.z * 0.8f;

        return spawnPos;
    }

    //Calculates the correct world rotation for highlights of a given point
    public Quaternion CalculateHighlightRotation()
    {
        //Calculate the Rotation of this highlight
        Quaternion spawnRot = Quaternion.Euler(Camera.main.transform.eulerAngles);
        var eulRot = spawnRot.eulerAngles;
        eulRot.x = eulRot.x + 90;
        spawnRot.eulerAngles = eulRot;

        return spawnRot;
    }

    //Returns the index of the given highlight
    public int GetIndex(Highlight h)
    {
        int index = 0;

        //Iterate through the highlight list
        for (int i = 0; i < hList.Count; i++)
        {
            //Check if the currently active highlight is the searched highlight
            if (h.getTexPos() == GetItem(i).getTexPos() && h.getTime() == GetItem(i).getTime())
            {
                //Set index the found index
                index = i;
                break;
            }
        }
        
        return index;
    }

    //Returns the item from highlight list
    public Highlight GetItem(int index)
    {
        return hList[index];
    }

    //Returns the complete highlight list
    public List<Highlight> GetList()
    {
        return hList;
    }

    //Deletes and clears the complete highlight list
    public void DeleteAllHighlights()
    {
        //Create temporary list to delete the highlight list
        List<Highlight> tempList = GetList();

        //Iterate through the temporary list (count of all highlights)
        for (int index = 0; index < tempList.Count; index++)
        {
            //Delete the currently selected highlight from the list
            DeleteHighlight(GetItem(index));
        }

        //Clears the highlight list of every value
        GetList().Clear();
    }

    //Removes highlight from the list of managed highlights
    public void DeleteHighlight(Highlight current)
    {
        //Check if the currently selected highlight is a real highlight
        if (current != null)
        {
            //Delete highlight from list and destroy the highlight
            hList.RemoveAt(hList.IndexOf(current));
        }
    }

    //Shows text for certain time in world space
    IEnumerator ShowTextForTime(String text)
    {
        String prevText = "";

        //Check if text already is shown
        if (textShown)
        {
            //Save the currently shown text
            prevText = vrMenu.GetComponent<Text>().text;
        }

        //Start showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = text;

        //Wait for a given time period (Text will be shown for this time period)
        yield return new WaitForSeconds(showTime);

        //Stop showing the input text on the VRMenu
        if (textShown)
        {
            //Start showing the previous text
            vrMenu.GetComponent<Text>().text = prevText;
        }
        else
        {
            //Show no text anymore
            vrMenu.GetComponent<Text>().text = "";
        }
    }

    //Shows text for certain time in world space
    void ShowText(String text)
    {
        //Check if there is text
        if (text != String.Empty)
        {
            //Set textShown to true
            textShown = true;
        }
        else
        {
            //Set textShown to false
            textShown = false;
        }

        //Start showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = text;
    }
}
