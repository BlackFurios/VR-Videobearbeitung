using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageHighlights : MonoBehaviour
{
    private MediaPlayer         mp;                                 //Instance of the MediaPlayer script
    private SaveData            sd;                                 //Instance of the SaveData script

    private List<GameObject>    hList = new List<GameObject>();     //List of all managed highlights
    public GameObject           highLight;                          //Highlight prefab to be managed
    
    private float               timeRange = 0.5f;                   //The time range in which this highlight should be shown in the UI (x2 in video)

    //Use this for initialization
    void Start ()
    {
        //Sets the mediaPlayer script
        mp = GetComponent<MediaPlayer>();
	}
	
	//Update is called once per frame
	void Update ()
    {
        //Iterate through the list of all highlights
        foreach (GameObject g in hList)
        {
            //Check if this highlight has to be shown now
            if (mp.GetCurrentPos().TotalSeconds > g.GetComponent<HighlightMemory>().getTime().TotalSeconds - timeRange &&
                mp.GetCurrentPos().TotalSeconds < g.GetComponent<HighlightMemory>().getTime().TotalSeconds + timeRange)
            {
                //Show this highlight
                g.GetComponent<Renderer>().enabled = true;
                g.GetComponent<MeshCollider>().enabled = true;
            }
            else
            {
                //Show this highlight
                g.GetComponent<Renderer>().enabled = false;
                g.GetComponent<MeshCollider>().enabled = false;
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
        if (current != null)
        {
            //Delete highlight from list and destroy the highlight
            hList.RemoveAt(hList.IndexOf(current));
            DestroyImmediate(current);
        }
    }
    
    //
    public List<GameObject> CreateItemChain(GameObject active)
    {
        List<GameObject> chain = new List<GameObject>();

        //Add the active highlight to the chain
        chain.Add(active);

        //Check for the first item in the list
        GameObject prev = getTimelyPrevItem(active);
        Debug.Log(active.GetComponent<HighlightMemory>().getTime());
        Debug.Log(prev.GetComponent<HighlightMemory>().getTime());          //<--- ERROR: NullReferenceException
        //Check if the the active highlight even has a previous chain item
        if (active.GetComponent<HighlightMemory>().getTime().Subtract(prev.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
        {
            //Search for the first highlight of the found chain
            while (prev != null && active.GetComponent<HighlightMemory>().getTime().Subtract(prev.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
            {
                //Make the new first highlight the previous highlight of the old first highlight
                prev = getTimelyPrevItem(prev);

                //Add the previous highlight in the chain
                chain.Insert(0, prev);
            }
        }

        //Check for the last item in the list
        GameObject next = getTimelyNextItem(active);

        //Check if the the active highlight even has a next chain item
        if (next.GetComponent<HighlightMemory>().getTime().Subtract(active.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
        {
            //Search for the last highlight of the found chain
            while (next != null && next.GetComponent<HighlightMemory>().getTime().Subtract(active.GetComponent<HighlightMemory>().getTime()).TotalMilliseconds <= 1000)
            {
                //Make the new last highlight the next highlight of the old last highlight
                next = getTimelyNextItem(next);

                //Add the next highlight in the chain
                chain.Add(next);
            }
        }

        return chain;
    }

    public GameObject getTimelyPrevItem(GameObject active)
    {
        GameObject output = null;
        TimeSpan minDif = TimeSpan.MaxValue;

        foreach (GameObject current in GetList())
        {
            TimeSpan difference = active.GetComponent<HighlightMemory>().getTime().Subtract(current.GetComponent<HighlightMemory>().getTime());

            if (difference.TotalMilliseconds < minDif.TotalMilliseconds && difference > TimeSpan.Zero)
            {
                minDif = difference;
                output = current;
            }
        }

        return output;
    }

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
}
