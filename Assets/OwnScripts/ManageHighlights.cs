using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManageHighlights : MonoBehaviour
{
    private MediaPlayer             mp;                                     //Instance of the MediaPlayer script
    private SaveData                sd;                                     //Instance of the SaveData script

    private Canvas                  vrMenu;                                 //Instance of the VRMenu object

    private List<GameObject>        hList = new List<GameObject>();         //List of all managed position highlights
    public GameObject               highLight;                              //Highlight prefab to be managed
    
    private float                   timeRange = 0.5f;                       //The time range in which this highlight should be shown in the UI (x2 in video)

    private Vector3                 impPos = new Vector3(0,0,0);            //Fixed position for all from an edl file imported highlights

    private bool                    textShown = false;                      //Is currently a text shown
    private int                     showTime = 1;                           //How long texts should be shown in seconds

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
        //Iterate through the list of all position highlights
        foreach (GameObject g in hList)
        {
            //Check if this highlight has to be shown now
            if (mp.GetCurrentPos().TotalSeconds > g.GetComponent<HighlightMemory>().getTime().TotalSeconds - timeRange &&
                mp.GetCurrentPos().TotalSeconds < g.GetComponent<HighlightMemory>().getTime().TotalSeconds + timeRange)
            {
                //Check if this highlight is a from an edl imported highlight with no texture position
                if (g.transform.position == impPos)
                {
                    //Show a exclamation mark to show an edl imported highlight with no texture position is currently showed
                    ShowText("!");
                }
                else
                {
                    //Show this highlight
                    g.GetComponent<Renderer>().enabled = true;
                    g.GetComponent<MeshCollider>().enabled = true;
                }
            }
            else
            {
                //Check if this highlight is a from an edl imported highlight with no texture position
                if (g.transform.position == impPos)
                {
                    //Do not show an edl imported highlight with no texture position anymore    
                    ShowText(String.Empty);
                }
                else
                {
                    //Show this highlight
                    g.GetComponent<Renderer>().enabled = false;
                    g.GetComponent<MeshCollider>().enabled = false;
                }
            }
        }
    }

    //Public function to create and add new highlight to video
    public void AddItem(Vector3 pos, TimeSpan ts, String type, Vector2 texPos)
    {
        //Set current to the newly spawned highlight object
        GameObject current = SpawnHighlight(pos);

        //Add new highlight to list of managed highlights
        hList.Add(current);

        //Add parameters of new highlight
        current.GetComponent<HighlightMemory>().setTime(ts);
        current.GetComponent<HighlightMemory>().setType(type);
        current.GetComponent<HighlightMemory>().setTexPos(texPos);
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

    //Returns the item from highlight list
    public GameObject GetItem(int index)
    {
        return hList[index];
    }

    //Returns the complete highlight list
    public List<GameObject> GetList()
    {
        return hList;
    }

    //Empties the list of all highlights and destroys all highlight gameObjects
    public void ClearList()
    {
        //Check if highlights were already spawned
        if (GetList().Count != 0)
        {
            //Destroy all highlights from list
            for (int i = 0; i < GetList().Count; i++)
            {
                //Delete the gameObject of the currently active highlight
                DeleteItem(GetItem(i));
            }

            //Clear list itself
            hList.Clear();
        }
    }

    //Removes highlight from the list of managed highlights and then destroys it
    public void DeleteItem(GameObject current)
    {
        //Check if the currently selected highlight is a real highlight
        if (current != null)
        {
            //Delete highlight from list and destroy the highlight
            hList.RemoveAt(hList.IndexOf(current));
            DestroyImmediate(current);
        }
    }
    
    //Returnes a complete chain/single highlight of near highlights
    public List<GameObject> CreateItemChain(GameObject active)
    {
        List<GameObject> chain = new List<GameObject>();

        //Add the active highlight to the chain
        chain.Add(active);

        //Check for the first item in the list
        GameObject prev = getTimelyPrevItem(active);

        //Check if the the active highlight even has a previous chain item
        if (prev != null && active.GetComponent<HighlightMemory>().getTime().Subtract(prev.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
        {
            //Search for the first highlight of the found chain
            while (prev != null && active.GetComponent<HighlightMemory>().getTime().Subtract(prev.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
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
        GameObject next = getTimelyNextItem(active);

        //Check if the the active highlight even has a next chain item
        if (next != null && next.GetComponent<HighlightMemory>().getTime().Subtract(active.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
        {
            //Search for the last highlight of the found chain
            while (next != null && next.GetComponent<HighlightMemory>().getTime().Subtract(active.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
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

    //
    public GameObject getTimelyPrevItem(GameObject active)
    {
        GameObject output = null;
        TimeSpan minDif = TimeSpan.MaxValue;

        //
        foreach (GameObject current in GetList())
        {
            //
            TimeSpan difference = active.GetComponent<HighlightMemory>().getTime().Subtract(current.GetComponent<HighlightMemory>().getTime());

            //
            if (difference.TotalMilliseconds < minDif.TotalMilliseconds && difference > TimeSpan.Zero)
            {
                //
                minDif = difference;
                output = current;
            }
        }

        return output;
    }

    //
    public GameObject getTimelyNextItem(GameObject active)
    {
        GameObject output = null;
        TimeSpan minDif = TimeSpan.MaxValue;

        foreach (GameObject current in GetList())
        {
            TimeSpan difference = current.GetComponent<HighlightMemory>().getTime().Subtract(active.GetComponent<HighlightMemory>().getTime());
            
            if (difference.TotalMilliseconds < minDif.TotalMilliseconds && difference > TimeSpan.Zero)
            {
                minDif = difference;
                output = current;
            }
        }

        return output;
    }

    public GameObject getPosNextItem(GameObject active)
    {
        GameObject output = null;
        float minDif = float.MaxValue;

        foreach (GameObject current in GetList())
        {
            float difference = Vector2.Distance(active.GetComponent<HighlightMemory>().getTexPos(), current.GetComponent<HighlightMemory>().getTexPos());

            if (difference <= minDif)
            {
                minDif = difference;
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
