using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManageHighlights : MonoBehaviour
{
    private MediaPlayer             mp;                                                     //Instance of the MediaPlayer script
    private SaveData                sd;                                                     //Instance of the SaveData script

    private Canvas                  vrMenu;                                                 //Instance of the VRMenu object

    private List<Highlight>         hList = new List<Highlight>();                          //List of all managed highlights

    [SerializeField]
    private  GameObject             highLight;                                              //Highlight prefab to be managed

    private TimeSpan                lastSpawn = TimeSpan.Zero;                              //The time of the last spawned highlight
    
    private float                   timeRange = 0.5f;                                       //The time range in which this highlight should be shown in the UI (x2 in video)

    private Vector3                 impPos = new Vector3(0,0,0);                            //Fixed position for all from an edl file imported highlights

    private bool                    textShown = false;                                      //Is currently a text shown
    private int                     showTime = 1;                                           //How long texts should be shown in seconds

    public class Highlight
    {
        private Vector3     pos;                                                            //The world position of the gameObject of the highlight
        private Vector2     texPos;                                                         //The texture position of the highlight
        private TimeSpan    time;                                                           //The time position of the highlight
        private String      type;                                                           //The type of the highlight
        private GameObject  highlight;                                                      //The gameObject representation of the highlight

        public Highlight(Vector3 val1, Vector2 val2, TimeSpan val3,
            String val4, GameObject val5)                                                   //Constructor of the Highlight class
        {
            pos = val1;
            texPos = val2;
            time = val3;
            type = val4;
            highlight = val5;

        }

        //Returns the world position of the highlight
        public Vector3 getPos()
        {
            return pos;
        }

        //Returns the texture position of the highlight
        public Vector2 getTexPos()
        {
            return texPos;
        }

        //Returns the time of the highlight
        public TimeSpan getTime()
        {
            return time;
        }

        //Returns the type of the highlight
        public String getType()
        {
            return type;
        }

        //Returns the type of the highlight
        public GameObject getRepresentation()
        {
            return highlight;
        }

        //Returns the type of the highlight
        public void setRepresentation(GameObject val)
        {
            highlight = val;
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
            //Check if this highlight has to be shown now
            if (h.getRepresentation() == null && mp.GetCurrentPos().TotalSeconds >= h.getTime().TotalSeconds - timeRange &&
                mp.GetCurrentPos().TotalSeconds <= h.getTime().TotalSeconds + timeRange)
            {
                //Check if this highlight is a from an edl imported highlight with no texture position
                if (h.getPos() != impPos)
                {
                    //Spawn the highlight gameObject
                    h.setRepresentation(SpawnHighlight(h.getPos()));
                }
                else
                {
                    //Show a exclamation mark to show an edl imported highlight with no texture position is currently showed
                    ShowText("!");
                }
            }
            else
            {
                //Check if this highlight is a from an edl imported highlight with no texture position
                if (h.getRepresentation() != null && h.getPos() != impPos)
                {
                    //Destroy the gameObject which represents the highlight
                    Destroy(h.getRepresentation());

                    //Set the shown parameter of the highlight
                    h.setRepresentation(null);
                }
                else
                {
                    //Show no text
                    ShowText(String.Empty);
                }
            }
        }
    }

    //Public function to create and add new highlight to video
    public void AddItem(Vector3 pos, Vector2 texPos, TimeSpan ts, String type)
    {
        if (ts.Subtract(lastSpawn) >= TimeSpan.FromMilliseconds(10))
        {
            //Set current to the newly spawned highlight object
            Highlight current = new Highlight(pos, texPos, ts, type, null);

            //Add new highlight to list of managed highlights
            hList.Add(current);

            //Set the last spawn time to the current spawn time
            lastSpawn = ts;
        }
    }

    //Spawns highlight with if no oher highlight collides with it
    GameObject SpawnHighlight(Vector3 pos)
    {
        //Calculate the position of this highlight
        Vector3 spawnPos = pos - Camera.main.transform.position;
        spawnPos.x = spawnPos.x * 0.9f;
        spawnPos.y = spawnPos.y * 0.9f;
        spawnPos.z = spawnPos.z * 0.9f;

        //Calculate the Rotation of this highlight
        Quaternion spawnRot = Quaternion.Euler(Camera.main.transform.eulerAngles);
        var eulRot = spawnRot.eulerAngles;
        eulRot.x = eulRot.x + 90;
        spawnRot.eulerAngles = eulRot;

        //Spawn the highlight
        return Instantiate(highLight, spawnPos, spawnRot);
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
    public void DeleteAllItems()
    {
        //Check if highlights were already spawned
        if (GetList().Count != 0)
        {
            //Iterate through the list of all managed highlights
            foreach (Highlight h in hList)
            {
                DeleteItem(h);
            }
        }
    }

    //Removes highlight from the list of managed highlights
    public void DeleteItem(Highlight current)
    {
        //Check if the currently selected highlight is a real highlight
        if (current != null)
        {
            //Check if the highlight currently has a gameObject which represents it
            if (current.getRepresentation() != null)
            {
                //Destroy the gameObject which represents the highlight
                Destroy(current.getRepresentation());
            }

            //Delete highlight from list and destroy the highlight
            hList.RemoveAt(hList.IndexOf(current));
        }
    }
    
    //Returnes a complete chain/single highlight of near highlights
    public List<Highlight> CreateItemChain(Highlight active)
    {
        List<Highlight> chain = new List<Highlight>();

        //Add the active highlight to the chain
        chain.Add(active);

        //Check for the first item in the list
        Highlight prev = getTimelyPrevItem(active);

        //Check if the the active highlight even has a previous chain item
        if (prev != null && active.getTime().Subtract(prev.getTime()).TotalMilliseconds <= 2000)
        {
            //Search for the first highlight of the found chain
            while (prev != null && active.getTime().Subtract(prev.getTime()).TotalMilliseconds <= 2000)
            {
                //Make the new first highlight the previous highlight of the old first highlight
                prev = getTimelyPrevItem(prev);

                if (prev != null)
                {
                    //Add the previous highlight in the chain
                    chain.Insert(0, prev);
                } 
            }
        }

        //Check for the last item in the list
        Highlight next = getTimelyNextItem(active);

        //Check if the the active highlight even has a next chain item
        if (next != null && next.getTime().Subtract(active.getTime()).TotalMilliseconds <= 1000)
        {
            //Search for the last highlight of the found chain
            while (next != null && next.getTime().Subtract(active.getTime()).TotalMilliseconds <= 1000)
            {
                //Make the new last highlight the next highlight of the old last highlight
                next = getTimelyNextItem(next);

                if (next != null)
                {
                    //Add the next highlight in the chain
                    chain.Add(next);
                }
            }
        }

        return chain;
    }

    //Returns the timely previous highlight of the given highlight
    public Highlight getTimelyPrevItem(Highlight active)
    {
        Highlight output = null;
        TimeSpan minDif = TimeSpan.MaxValue;

        //Iterate through all highlights
        foreach (Highlight current in GetList())
        {
            //Get the time difference between the given and the currently checked highlight
            TimeSpan difference = active.getTime().Subtract(current.getTime());

            //Check if the new difference is shorter than the currently shortest time difference
            if (difference > TimeSpan.Zero && difference.TotalMilliseconds < minDif.TotalMilliseconds)
            {
                //Set the new difference as new shortest time difference
                minDif = difference;

                //Define the currently checked highlight as new previous highlight to return
                output = current;
            }
        }

        return output;
    }

    //Returns the timely next highlight of the given highlight
    public Highlight getTimelyNextItem(Highlight active)
    {
        Highlight output = null;
        TimeSpan minDif = TimeSpan.MaxValue;

        //Iterate through all highlights
        foreach (Highlight current in GetList())
        {
            //Get the time difference between the given and the currently checked highlight
            TimeSpan difference = current.getTime().Subtract(active.getTime());

            //Check if the new difference is shorter than the currently shortest time difference
            if (difference.TotalMilliseconds < minDif.TotalMilliseconds && difference > TimeSpan.Zero)
            {
                //Set the new difference as new shortest time difference
                minDif = difference;

                //Define the currently checked highlight as new next highlight to return
                output = current;
            }
        }

        return output;
    }

    //Returns the position next highlight of the given highlight
    public Highlight getPosNextItem(Highlight active)
    {
        Highlight output = null;
        float minDif = float.MaxValue;

        //Iterate through all highlights
        foreach (Highlight current in GetList())
        {
            //Get the time difference between the given and the currently checked highlight
            float difference = Vector2.Distance(active.getTexPos(), current.getTexPos());

            //Check if the new difference is shorter than the currently shortest position difference
            if (difference <= minDif)
            {
                //Set the new difference as new shortest position difference
                minDif = difference;

                //Define the currently checked highlight as new next highlight to return
                output = current;
            }
        }

        return output;
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
