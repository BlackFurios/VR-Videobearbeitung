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
    private int                     hlShowTime = 2;                             //How long a highlight item should be shown in seconds

    public class Highlight
    {
        private List<Vector3>       pos = new List<Vector3>();                  //List of all world positions of the highlight
        private List<Vector2>       texPos = new List<Vector2>();               //List of all texture positions of the highlight
        private List<TimeSpan>      time = new List<TimeSpan>();                //List of all time positions of the highlight
        private String              type;                                       //The type of the highlight

        public List<GameObject>     objList = new List<GameObject>();           //List of currently shown highlights

        public Highlight(List<Vector3> initPos, List<Vector2> initTexPos,
            List<TimeSpan> initTime, String initType)                           //Constructor of the Highlight class
        {
            pos = initPos.ToList();
            texPos = initTexPos.ToList();
            time = initTime.ToList();
            type = initType;
        }

        //Returns the world position of the highlight
        public List<Vector3> getPos()
        {
            return pos;
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
        //Save the current time for further checks
        TimeSpan currentTime = mp.GetCurrentPos();

        //Iterate through the list of all highlights
        foreach (Highlight h in hList)
        {
            //Check if the highlight should be shown
            if (currentTime.TotalMilliseconds > h.getTime().First<TimeSpan>().TotalMilliseconds &&
                currentTime.TotalMilliseconds < h.getTime().Last<TimeSpan>().TotalMilliseconds)
            {
                //Get the index of the current time in the time list of the highlight
                int index = h.getTime().IndexOf(currentTime);

                //Check if the current time is present in the list of all highlight times
                if (index != -1)
                {
                    //Check if this highlight has world positions (save file or edl file)
                    if (h.getPos().Count > 0)
                    {
                        //Spawn an object at the current hitghlight position
                        GameObject currentObj = SpawnHighlight(h.getPos()[index]);

                        //Add the object to the lst of all objects of this highlight
                        h.objList.Add(currentObj);

                        //Destroy the object after a ceratin delay --> hlShowTime
                        Destroy(currentObj, hlShowTime);
                    }
                    else
                    {
                        //Show current time position in active highlight without world position
                        ShowText("-!- " + mp.GetCurrentPos().Subtract(h.getTime()[index]) + " -!-");
                    }
                }
            }
            else if (h.objList.Count > 0)
            {
                h.objList.Clear();
            }
        }
    }

    //Public function to create and add new highlight to video
    public void AddItem(List<Vector3> pos, List<Vector2> texPos, List<TimeSpan>  ts, String type)
    {
        //Set current to the newly spawned highlight object
        Highlight current = new Highlight(pos, texPos, ts, type);

        //Add new highlight to list of managed highlights
        hList.Add(current);
    }

    //Spawns highlight with if no oher highlight collides with it
    public GameObject SpawnHighlight(Vector3 pos)
    {
        //Spawn the highlight
        return Instantiate(highLight, pos, CalculateHighlightRotation());
    }

    public Vector3 CalculateHighlightPosition(Vector3 pos)
    {
        //Calculate the position of this highlight
        Vector3 spawnPos = pos;
        spawnPos.x = spawnPos.x * 0.8f;
        spawnPos.y = spawnPos.y * 0.8f;
        spawnPos.z = spawnPos.z * 0.8f;

        return spawnPos;
    }

    Quaternion CalculateHighlightRotation()
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

    //Empties the list of all highlights and destroys all highlight gameObjects
    public void DeleteAllHighlights()
    {
        //Check if highlights were already spawned
        if (GetList().Count != 0)
        {
            //Iterate through the list of all managed highlights
            foreach (Highlight h in hList)
            {
                DeleteHighlight(h);
            }
        }
    }

    //Removes highlight from the list of managed highlights
    public void DeleteHighlight(Highlight current)
    {
        //Check if the currently selected highlight is a real highlight
        if (current != null)
        {
            //Check if this highlight has a gameObject
            if (current.objList.Count > 0)
            {
                //Iterate through the list of objects of the highlight
                foreach (GameObject g in current.objList)
                {
                    //Destroy the gameObject of this highlight
                    DestroyImmediate(g);
                }
            }

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
