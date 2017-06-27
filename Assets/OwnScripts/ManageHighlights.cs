﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageHighlights : MonoBehaviour
{
    private MediaPlayer         mp;                                 //Instance of the MediaPlayer script
    private SaveData            sd;                                 //Instance of the SaveData script

    private List<GameObject>    hList = new List<GameObject>();     //List of all managed highlights
    public GameObject           highLight;                          //Highlight prefab to be managed

    private float               range = 3;                          //Range in which no other highlight should be spawned
    private float               timeRange = 3;                      //The time range in which this highlight should be shown in the UI (x2 in video)

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
		foreach(GameObject g in hList)
        {
            //Check if this highlight has to be shown now
            if (mp.GetCurrentPos().TotalSeconds > g.GetComponent<HighlightMemory>().getTime().TotalSeconds - timeRange && mp.GetCurrentPos().TotalSeconds < g.GetComponent<HighlightMemory>().getTime().TotalSeconds + timeRange)
            {
                //Show this highlight
                g.GetComponent<Renderer>().enabled = true;
                g.GetComponent<MeshCollider>().enabled = true;
            }
            else
            {
                //Check if the next or previous chain highlight of this highlight is shown if it has one
                if (g.GetComponent<HighlightMemory>().getNext() != null || g.GetComponent<HighlightMemory>().getPrev() != null)
                {
                    //Check if the selected highlights next or previous highlight is visible
                    if (g.GetComponent<HighlightMemory>().getNext().GetComponent<Renderer>().enabled == true || g.GetComponent<HighlightMemory>().getPrev().GetComponent<Renderer>().enabled == true)
                    {
                        //Show this highlight
                        g.GetComponent<Renderer>().enabled = true;
                        g.GetComponent<MeshCollider>().enabled = true;

                        //Check if the selected highlights next highlight is not null
                        if (g.GetComponent<HighlightMemory>().getNext() != null)
                        {
                            //Draw a line from this highlight to its next highlight to show connection in world space
                            DrawLine(g, g.GetComponent<HighlightMemory>().getNext().transform.position);
                        }
                    }
                    else
                    {
                        //Do not show this highlight anymore
                        g.GetComponent<Renderer>().enabled = false;
                        g.GetComponent<MeshCollider>().enabled = false;
                    }
                }
                else
                {
                    //Do not show this highlight anymore
                    g.GetComponent<Renderer>().enabled = false;
                    g.GetComponent<MeshCollider>().enabled = false;
                }
            }
        }
	}

    //Spawns highlight with if no oher highlight collides with it
    GameObject SpawnHighlight(Vector3 pos, GameObject other)
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

        //Check if the other variable is not null
        if(other != null)
        {
            //Check for every existing highlight if it is colliding with the one to be created
            if (Vector3.Distance(other.transform.position, spawnPos) <= range && other.GetComponent<Renderer>().enabled == true)
            {
                // Returns error object (null) for AddItem() to handle
                return null;
            }
            else
            {
                //Spawn highlight at position spawnPos and with rotation spawnRot
                return Instantiate(highLight, spawnPos, spawnRot);
            }
        }
        else
        {
            //Spawn highlight at position spawnPos and with rotation spawnRot
            return Instantiate(highLight, spawnPos, spawnRot);
        }
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

    //Public function to create and add new highlight to video
    public String AddItem(Vector3 pos, TimeSpan ts, String type, Vector2 texPos, String video)
    {
        GameObject current = null;

        //Check if this highlight is the first one to be created
        if (hList.Count != 0)
        {
            //Iterate through the list of all highlights
            foreach (GameObject g in hList)
            {
                //Check if spawning this highlight is possible
                current = SpawnHighlight(pos, g);

                //Check if the current is null (SpawnHighlight() has an invalid output)
                if (current == null)
                {
                    break;
                }
            }
        }
        else
        {
            //Spawn highlight at position spawnPos and with rotation spawnRot
            current = SpawnHighlight(pos, null);
        }

        //Check if SpawnHighlight detected an already existing highlight
        if (current != null)
        {
            //Add new highlight to list of managed highlights
            hList.Add(current);

            //Add parameters of new highlight
            current.GetComponent<HighlightMemory>().setTime(ts);
            current.GetComponent<HighlightMemory>().setType(type);
            current.GetComponent<HighlightMemory>().setTexPos(texPos);
            current.GetComponent<HighlightMemory>().setVideo(video);
            current.GetComponent<HighlightMemory>().setPrev(null);
            current.GetComponent<HighlightMemory>().setNext(null);

            return "Highlight successfully created";
        }
        else
        {
            return "Highlight could not be created";
        }
    }

    //Modifies an already existing highlights parameters
    public void ModifyItem(GameObject current, Vector3 pos, TimeSpan ts, String type, Vector2 texPos)
    {
        //Check if the currently selected highlight exists
        if (current != null)
        {
            //Locate and rotate highlight according to current movement
            MoveItem(current, pos);

            //Add parameters of new highlight
            current.GetComponent<HighlightMemory>().setTime(ts);
            current.GetComponent<HighlightMemory>().setType(type);
            current.GetComponent<HighlightMemory>().setTexPos(texPos);
        }
        else
        {
            Debug.Log("No existing highligt selected to modify");
        }
    }

    //Translates a currently selected highlight to a new position
    public void MoveItem(GameObject current, Vector3 pos)
    {
        //Calculate the position of this highlight
        Vector3 modPos = pos - Camera.main.transform.position;
        modPos.x = modPos.x * 0.9f;
        modPos.y = modPos.y * 0.9f;
        modPos.z = modPos.z * 0.9f;

        //Calculate the Rotation of this highlight
        Quaternion modRot = Quaternion.Euler(Camera.main.transform.eulerAngles);
        var eulRot = modRot.eulerAngles;
        eulRot.x = eulRot.x + 90;
        modRot.eulerAngles = eulRot;

        //Translate the selected highlight
        current.transform.position = modPos;
        current.transform.rotation = modRot;

        //Check if the selected highlight has a next highlight
        if (current.GetComponent<HighlightMemory>().getNext() != null)
        {
            //Draws a line from the selected highlight to its next highlight
            DrawLine(current, current.GetComponent<HighlightMemory>().getNext().transform.position);
        }
        //Check if the selected highlight has a previous highlight
        else if (current.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //Draws a line from the previous highlight to the selected highlight
            DrawLine(current.GetComponent<HighlightMemory>().getPrev(), current.transform.position);
        }
    }

    //Empties the list of all highlights and destroys all highlight gameObjects
    public void ClearList()
    {
        //Destroy all highlights from list
        foreach (GameObject g in hList)
        {
            //Delete the gameObject of the currently active highlight
            DeleteItem(g);
        }

        //Clear list itself
        hList.Clear();
    }

    //Removes highlight from the list of managed highlights and then destroys it
    public String DeleteItem(GameObject current)
    {
        //Check if highlight is in between two other connected highlights
        if (current.GetComponent<HighlightMemory>().getNext() != null && current.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //Connect the next and the previous highlight to each other to safely remove the selected highlight
            current.GetComponent<HighlightMemory>().getNext().GetComponent<HighlightMemory>().setPrev(current.GetComponent<HighlightMemory>().getPrev());
            current.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(current.GetComponent<HighlightMemory>().getNext());
        }
        //Check if highlight has next highlight
        else if (current.GetComponent<HighlightMemory>().getNext() != null)
        {
            //Delete highlight as previous highlight from next highlight
            current.GetComponent<HighlightMemory>().getNext().GetComponent<HighlightMemory>().setPrev(null);
        }

        //Check if highlight has previous highlight
        else if (current.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //Disable line renderer of selected highlights previous highlight
            current.GetComponent<HighlightMemory>().getPrev().GetComponent<LineRenderer>().enabled = false;

            //Delete highlight as next highlight from previous highlight
            current.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(null);
        }
        
        //Delete highlight from list and destroy the highlight
        hList.RemoveAt(hList.IndexOf(current));
        Destroy(current);
        
        return "Highlight successfully removed";
    }

    //Connects two given highlights automatically with each other
    public String ConnectItems(GameObject selected, GameObject other)
    {
        //Check if selected highlight and other highlight are the same highlight
        if (selected != other)
        {
            switch (TimeSpan.Compare(selected.GetComponent<HighlightMemory>().getTime(), other.GetComponent<HighlightMemory>().getTime()))
            {
                //selected.time -> other.time
                case -1:
                    //Check if the selected highlight already has a next highlight
                    if (selected.GetComponent<HighlightMemory>().getNext() != null)
                    {
                        return "First highlight already has a next highlight\nPlease disonnect first";
                    }
                    //Check if the other highlight already has a previous highlight
                    else if (other.GetComponent<HighlightMemory>().getPrev() != null)
                    {
                        return "Second highlight already has a previous highlight\nPlease disonnect first";
                    }
                    //Check if both highlights have the necessary connections free
                    else
                    {
                        //Set both highligts in each others connection parameter
                        selected.GetComponent<HighlightMemory>().setNext(other);
                        other.GetComponent<HighlightMemory>().setPrev(selected);

                        //Change type of both highlights to "Chain"
                        selected.GetComponent<HighlightMemory>().setType("Chain");
                        other.GetComponent<HighlightMemory>().setType("Chain");

                        //Draws a line from the selected highlight to the other highlight
                        DrawLine(selected, other.transform.position);
                        return "Highlights successfully connected";
                    }
                //selected.time = other.time
                case 0:
                    //selected.texPos != other.texPos
                    if (!selected.GetComponent<HighlightMemory>().getTexPos().Equals(other.GetComponent<HighlightMemory>().getTexPos()))
                    {
                        //Set both highligts in each others connection parameter
                        selected.GetComponent<HighlightMemory>().setNext(other);
                        other.GetComponent<HighlightMemory>().setPrev(selected);

                        //Change type of both highlights to "Chain"
                        selected.GetComponent<HighlightMemory>().setType("Chain");
                        other.GetComponent<HighlightMemory>().setType("Chain");

                        //Draws a line from the selected highlight to the other highlight
                        DrawLine(selected, other.transform.position);
                        return "Highlights successfully connected";
                    }
                    //selected.texPos = other.texPos
                    else
                    {
                        return "Both highlights are at the same position and time in video";
                    }
                //other.time -> selected.time
                case 1:
                    //Check if the selected highlight already has a previous highlight
                    if (selected.GetComponent<HighlightMemory>().getPrev() != null)
                    {
                        return "First highlight already has a previous highlight\nPlease disonnect first";
                    }
                    //Check if the other highlight already has a next highlight
                    else if (other.GetComponent<HighlightMemory>().getNext() != null)
                    {
                        return "Second highlight already has a next highlight\nPlease disonnect first";
                    }
                    //Check if both highlights have the necessary connections free
                    else
                    {
                        //Set both highligts in each others connection parameter
                        selected.GetComponent<HighlightMemory>().setPrev(other);
                        other.GetComponent<HighlightMemory>().setNext(selected);

                        //Change type of both highlights to "Chain"
                        selected.GetComponent<HighlightMemory>().setType("Chain");
                        other.GetComponent<HighlightMemory>().setType("Chain");

                        //Draws a line from the selected highlight to the other highlight
                        DrawLine(other, selected.transform.position);
                        return "Highlights successfully connected";
                    }
                default:
                    return "ERROR: Comparison failes";
            }
        }
        else
        {
            return "Can not connect highlight to itself";
        }
    }

    //Disconnects two given highlights automatically from each other
    public String DisconnectItems(GameObject selected)
    {
        //Check if the selected highlight has a next and a previous highlight
        if (selected.GetComponent<HighlightMemory>().getNext() != null && selected.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //Connects the selected highlights previous highlight to the selected highlights next highlight
            ConnectItems(selected.GetComponent<HighlightMemory>().getPrev(), selected.GetComponent<HighlightMemory>().getNext());

            //Disables the connection line from the selected highlight to its next highlight
            selected.GetComponent<LineRenderer>().enabled = false;

            //Change type of the selected highlight to "Single"
            selected.GetComponent<HighlightMemory>().setType("Single");

            //Change both next and prev parameters of the selected highlight to null
            selected.GetComponent<HighlightMemory>().setPrev(null);
            selected.GetComponent<HighlightMemory>().setNext(null);

            return "Successfully disconnected this highlight from any connections";
        }
        //Check if the selected highlight only has a next highlight
        else if (selected.GetComponent<HighlightMemory>().getNext() != null)
        {
            //Disables the connection line from the selected highlight to its next highlight
            selected.GetComponent<LineRenderer>().enabled = false;

            //Change type of the selected highlight to "Single"
            selected.GetComponent<HighlightMemory>().setType("Single");

            //Change the prev parameter of the selected highlights next highlight and the next parameter of the selected highlight
            selected.GetComponent<HighlightMemory>().getNext().GetComponent<HighlightMemory>().setPrev(null);
            selected.GetComponent<HighlightMemory>().setNext(null);

            return "Successfully disconnected this highlight\nfrom his connection to the next highlight";
        }
        //Check if the selected highlight only has a previous highlight
        else if (selected.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //Disables the connection line from the selected highlights previous highlight to the selected highlight
            selected.GetComponent<HighlightMemory>().getPrev().GetComponent<LineRenderer>().enabled = false;

            //Change type of the selected highlight to "Single"
            selected.GetComponent<HighlightMemory>().setType("Single");

            //Change the prev parameter of the selected highlight and the next parameter of the selected highlights previous highlight
            selected.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(null);
            selected.GetComponent<HighlightMemory>().setPrev(null);

            return "Successfully disconnected this highlight\nfrom his connection to the previous highlight";
        }
        //Check if the selected has no connection at all
        else
        {
            return "This highlight has no connections to disconnect";
        }
    }

    //Draws a line from a currently selected highlight to a given position in world space
    public void DrawLine(GameObject current, Vector3 otherPos)
    {
        //The line renderer component of the current highlight
        LineRenderer lr = current.GetComponent<LineRenderer>();

        //Check if line renderer is already enabled
        if(lr.enabled == false)
        {
            //Activate line renderer
            lr.enabled = true;
        }

        //Set start- and endpoint of line renderer
        lr.SetPosition(0, current.transform.position);
        lr.SetPosition(1, otherPos);


    }
}
