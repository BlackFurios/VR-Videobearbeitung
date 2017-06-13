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

    private float               range = 3;                          //Range in which no other highlight should be spawned
    private float               timeRange = 5;                      //The time range in which this highlight should be shown in the UI

    //Use this for initialization
    void Start ()
    {
        mp = GetComponent<MediaPlayer>();
	}
	
	//Update is called once per frame
	void Update ()
    {
		foreach(GameObject g in hList)
        {
            //Check if this highlight has to be shown now
            if (mp.GetCurrentPos().TotalSeconds > g.GetComponent<HighlightMemory>().getTime().TotalSeconds - timeRange && mp.GetCurrentPos().TotalSeconds < g.GetComponent<HighlightMemory>().getTime().TotalSeconds + timeRange)
            {
                //Show this highlight
                g.GetComponent<Renderer>().enabled = true;
            }
            else
            {
                //Check if the next or previous chain highlight of this highlight is shown if it has one
                if (g.GetComponent<HighlightMemory>().getNext() != null || g.GetComponent<HighlightMemory>().getPrev() != null && g.GetComponent<HighlightMemory>().getNext().GetComponent<Renderer>().enabled == true || g.GetComponent<HighlightMemory>().getPrev().GetComponent<Renderer>().enabled == true)
                {
                    //Show this highlight
                    g.GetComponent<Renderer>().enabled = true;
                }
                else
                {
                    //Do not show this highlight anymore
                    g.GetComponent<Renderer>().enabled = false;
                }
            }

            //Draw line if this highlight is shown and it is connected to a next highlight
            if (g.GetComponent<Renderer>().enabled == true && g.GetComponent<HighlightMemory>().getNext() != null)
            {
                //Draws a line to show the connection
                g.GetComponent<LineRenderer>().SetPosition(0, transform.position);
                g.GetComponent<LineRenderer>().SetPosition(1, g.GetComponent<HighlightMemory>().getNext().transform.position);
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

    //Get item from highlight list
    public GameObject GetItem(int index)
    {
        return hList[index];
    }

    //Get complete highlight list
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
            foreach (GameObject g in hList)
            {
                current = SpawnHighlight(pos, g);
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

    public void ClearList()
    {
        //Destroy all highlights from list
        foreach (GameObject g in hList)
        {
            DeleteItem(g);
        }

        //Clear list itself
        hList.Clear();
    }

    //Removes highlight from the list of managed highlights and then destroys it
    public void DeleteItem(GameObject current)
    {
        if (current.GetComponent<HighlightMemory>().getNext() != null && current.GetComponent<HighlightMemory>().getPrev() != null)
        {
            //
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
            //Delete highlight as next highlight from previous highlight
            current.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(null);
        }
        
        //Delete highlight from list and destroy the highlight
        hList.RemoveAt(hList.IndexOf(current));
        Destroy(current);
    }

    public String ConnectItems(GameObject selected)
    {
        RaycastHit hit;

        //Check if Raycast hits a highlight
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
        {
            var other = hit.transform.gameObject;

            //Checks if currently selected highlight comes after the other highlight
            if (selected.GetComponent<HighlightMemory>().getTime() > other.GetComponent<HighlightMemory>().getTime())
            {
                //Checks if one of the highlights already has this kind of connection
                if (selected.GetComponent<HighlightMemory>().getPrev() != null || other.GetComponent<HighlightMemory>().getNext() != null)
                {
                    return "One of the highlights is already connected";
                }
                else
                {
                    //Connects both highlights to each other
                    selected.GetComponent<HighlightMemory>().setPrev(other);
                    other.GetComponent<HighlightMemory>().setNext(selected);

                    //Changes type of both highlights to chain
                    selected.GetComponent<HighlightMemory>().setType("Chain");
                    other.GetComponent<HighlightMemory>().setType("Chain");

                    //Draws a line to show the connection
                    //Gizmos.DrawLine(selected.transform.position, other.transform.position);

                    return "Highlights successfully connected";
                }
            }
            //Checks if currently selected highlight comes before the other highlight
            else if (selected.GetComponent<HighlightMemory>().getTime() < other.GetComponent<HighlightMemory>().getTime())
            {
                //Checks if one of the highlights already has this kind of connection
                if (selected.GetComponent<HighlightMemory>().getNext() != null || other.GetComponent<HighlightMemory>().getPrev() != null)
                {
                    return "One of the highlights is already connected";
                }
                else
                {
                    //Connects both highlights to each other
                    selected.GetComponent<HighlightMemory>().setNext(other);
                    other.GetComponent<HighlightMemory>().setPrev(selected);

                    //Changes type of both highlights to chain
                    selected.GetComponent<HighlightMemory>().setType("Chain");
                    other.GetComponent<HighlightMemory>().setType("Chain");

                    //Draws a line to show the connection
                    //Gizmos.DrawLine(selected.transform.position, other.transform.position);

                    return "Highlights successfully connected";
                }
            }
            else
            {
                return "No seperate highlights selected";
            }
        }
        else
        {
            return "No highlight to connect to selected";
        }
    }

    public String DisconnectItems(GameObject selected, String mode)
    {
        if (selected.GetComponent<HighlightMemory>().getPrev() != null || selected.GetComponent<HighlightMemory>().getNext() != null)
        {
            RaycastHit hit;

            //Check if Raycast hits a highlight
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                var other = hit.transform.gameObject;

                switch (mode)
                {
                    case "Prev":
                        //
                        if (selected.GetComponent<HighlightMemory>().getPrev() != null)
                        {
                            //
                            selected.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(null);
                            selected.GetComponent<HighlightMemory>().setPrev(null);
                        }
                        break;
                    case "Next":
                        //
                        if (selected.GetComponent<HighlightMemory>().getNext() != null)
                        {
                            //
                            selected.GetComponent<HighlightMemory>().getNext().GetComponent<HighlightMemory>().setPrev(null);
                            selected.GetComponent<HighlightMemory>().setNext(null);
                        }
                        break;
                    case "Both":
                        //
                        if (selected.GetComponent<HighlightMemory>().getPrev() != null && selected.GetComponent<HighlightMemory>().getNext() != null)
                        {
                            selected.GetComponent<HighlightMemory>().getPrev().GetComponent<HighlightMemory>().setNext(null);
                            selected.GetComponent<HighlightMemory>().setPrev(null);

                            //
                            selected.GetComponent<HighlightMemory>().getNext().GetComponent<HighlightMemory>().setPrev(null);
                            selected.GetComponent<HighlightMemory>().setNext(null);
                        }
                        break;
                    default:
                        break;
                }

                //Checks if currently selected highlight comes after the other highlight
                if (selected.GetComponent<HighlightMemory>().getTime() > other.GetComponent<HighlightMemory>().getTime())
                {
                    //
                    selected.GetComponent<HighlightMemory>().setPrev(null);
                    other.GetComponent<HighlightMemory>().setNext(null);

                    return "Highlights successfully disconnected";
                }
                //Checks if currently selected highlight comes before the other highlight
                else if (selected.GetComponent<HighlightMemory>().getTime() < other.GetComponent<HighlightMemory>().getTime())
                {
                    //
                    selected.GetComponent<HighlightMemory>().setNext(null);
                    other.GetComponent<HighlightMemory>().setPrev(null);

                    return "Highlights successfully disconnected";
                }
                else
                {
                    return "No timely seperate highlights selected";
                }
            }
            else
            {
                return "No highlight to disconnect selected";
            }
        }
        else
        {
            return "Slected highlight is not connected to anything";
        }
    }
}
