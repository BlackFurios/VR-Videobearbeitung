using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Control : MonoBehaviour
{
    private ManageHighlights    mh;                                 //Instance of the ManageHighlights script
    private MediaPlayer         mp;                                 //Instance of the MediaPlayer script

    private Dropdown            list;                               //Instance of the dropdown list object

    private Canvas              vrMenu;                             //Instance of the VRMenu object
    private Canvas              hlMenu;                             //Instance of the HighlightMenu object
    private Canvas              stMenu;                             //Instance of the StartMenu object
    private Canvas              dcMenu;                             //Instance of the DisconnectMenu object
    
    private RaycastHit          hit;                                //Point where the raycast hits

    private GameObject          selectedObject;                     //Highlight which the user has selected
    private int                 selectedIndex;                      //Dropdown index which the user has selected

    private List<String>        videoList = new List<string>();     //List of currently possible movies

    private bool                forwarding = false;                 //Is the video currently forwarding
    private bool                reversing = false;                  //Is the video currently reversing
    private bool                opened = false;                     //Is the drodown list opened
    private bool                pausing = false;                    //Is the video currently paused
    private int                 showTime = 1;                       //How long texts should be shown in seconds

    public class localId
    {
        public GameObject g;
        public String localID;

        public localId(GameObject a, String b)
        {
            g = a;
            localID = b;
        }
    }

    public class nextId
    {
        public GameObject g;
        public String nextLocalID;

        public nextId(GameObject a, String b)
        {
            g = a;
            nextLocalID = b;
        }
    }

    // Use this for initialization
    void Start ()
    {
        mh = GetComponent<ManageHighlights>();
        mp = GetComponent<MediaPlayer>();

        for(int i = 0; i < mp.GetMovieList().Count; i++)
        {
            //Fill movieList with detected videos
            videoList.Add(mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")));
        }

        //Search for VRMenu and highlightMenu
        foreach(Canvas c in FindObjectsOfType<Canvas>())
        {
            if(c.name == "VRMenu")
            {
                vrMenu = c;
            }
            if (c.name == "HighlightMenu")
            {
                hlMenu = c;

                ConfigureMenu(hlMenu, false);
            }
            if (c.name == "StartMenu")
            {
                stMenu = c;
            }
            if (c.name == "DisconnectMenu")
            {
                dcMenu = c;

                ConfigureMenu(dcMenu, false);
            }
        }

        //Search for VideoDropdownList
        list = FindObjectOfType<Dropdown>();

        list.GetComponent<Dropdown>().AddOptions(videoList);
	}
	
	// Update is called once per frame
	void Update ()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        //Joystick Controls for Android
        if (Input.GetButton("A-Android"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }
        
        if (Input.GetButtonDown("B-Android"))
        {
            //Shows the current time
            StartCoroutine(ShowText(mp.GetCurrentPos().ToString()));
        }
        
        if (Input.GetButtonDown("Y-Android"))
        {
            //Rewinding current video
            mp.Rewind();

            //Unpauses current video
            pausing = false;
            mp.SetPaused(pausing);
        }
        
        if (Input.GetButtonDown("X-Android"))
        {
            //Pauses current video
            pausing = true;
            mp.SetPaused(pausing);

            //Opens StartMenu to switch video
            ConfigureMenu(stMenu, !stMenu.enabled);
        }

        if (Input.GetButtonDown("L2-Android"))
        {
            //Check if raycast hits the media sphere
            if (dcMenu.enabled == false || hlMenu.enabled == false || stMenu.enabled == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Starts creation of the new highlight
                StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", hit.textureCoord, mp.GetMovieName())));
            }
        }

        if (Input.GetButtonDown("R2-Android"))
        {
            //Check if all menus are disabled and if the hit object is a highlight
            if (dcMenu.enabled == false && hlMenu.enabled == false && stMenu.enabled == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Pauses current video
                pausing = true;
                mp.SetPaused(pausing);

                //If the highlightMenu is disabled, enables it and shows it infront of the selected highlight
                ConfigureMenu(hlMenu, true);

                //Adjust the highlightMenu with the selected highlight
                Quaternion rot = new Quaternion(hit.transform.gameObject.transform.rotation.x, hit.transform.gameObject.transform.rotation.y, hit.transform.gameObject.transform.rotation.z, hit.transform.gameObject.transform.rotation.w);
                Vector3 pos = new Vector3(hit.transform.gameObject.transform.position.x, hit.transform.gameObject.transform.position.y, hit.transform.gameObject.transform.position.z);

                //Rotate the highlightMenu to face the camera and show it infront of highlight (not in it)
                hlMenu.transform.rotation = rot;
                hlMenu.transform.localRotation = Quaternion.Euler(hlMenu.transform.localEulerAngles.x - 90, hlMenu.transform.localEulerAngles.y, hlMenu.transform.localEulerAngles.z);
                hlMenu.transform.position = pos;
                hlMenu.transform.localPosition = new Vector3(hlMenu.transform.localPosition.x, hlMenu.transform.localPosition.y, hlMenu.transform.localPosition.z + 5);

                //Make the hit highlight the currently selected highlight
                selectedObject = hit.transform.gameObject;
            }
            //Check if the StartMenu is enabled and the dropdown list is closed
            else if (stMenu.enabled == true && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                switch (hit.transform.gameObject.name)
                {
                    case "VideoDropdown":
                        //Open the dropdown list
                        list.Show();

                        //Give every Item own box collider
                        for (int i = 1; i < list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.childCount; i++)
                        {
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.AddComponent<BoxCollider>();
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.GetComponent<BoxCollider>().size = new Vector3(158, 28, 1);
                        }
                        
                        opened = true;
                        break;
                    case "PlayVideo":
                        ConfigureMenu(stMenu, false);

                        //Load save file for currently selected video
                        Load(list.options[list.value].text);

                        //Set the chosen movie in the player and start the playback
                        mp.SetMovieName(list.options[list.value].text);
                        StartCoroutine(ShowText(mp.StartVideo()));
                        break;
                    case "Save":
                        ConfigureMenu(stMenu, false);
                        
                        //Save file for currently selected video
                        Save(list.options[list.value].text);
                        break;
                    default:
                        break;
                }
            }
            //Check if the StartMenu is enabled and the dropdown list is opened
            else if (stMenu.enabled == true && opened == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check for all videos if the shown item is part of possible videos
                foreach (String video in videoList)
                {
                    if (hit.transform.gameObject.name.Contains(video))
                    {
                        //Get item index from hitted objects name
                        selectedIndex = int.Parse(Regex.Replace(hit.transform.gameObject.name.Substring(0, hit.transform.gameObject.name.IndexOf(":")), "[^0-9]", ""));

                        //Set selected item as new top item
                        list.value = selectedIndex;

                        //Hide the dropdown list
                        list.Hide();

                        opened = false;
                        break;
                    }
                }

                //Refresh the dropdown list with new parameters
                list.RefreshShownValue();
            }
            //Check if the highlightMenu is enabled
            else if (dcMenu.enabled == false && hlMenu.enabled == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check which button of the highlightMenu was clicked
                switch (hit.transform.gameObject.name)
                {
                    case "Connect":
                        //Connects the currently selected to another highlight, if possible
                        StartCoroutine(ShowText(mh.ConnectItems(selectedObject)));
                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Disconnect":
                        //Disonnects the currently selected and another highlight
                        ConfigureMenu(dcMenu, true);
                        break;
                    case "GetInfo":
                        //Gets all information about the currently selected highlight
                        String info = selectedObject.GetComponent<HighlightMemory>().getType();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getVideo();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getTime();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getTexPos();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getPrev();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getNext();

                        StartCoroutine(ShowText(info));

                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Delete":
                        //Deletes the currently selected highlight
                        mh.DeleteItem(hit.transform.gameObject);
                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Close":
                        //Closes the highlightMenu
                        ConfigureMenu(hlMenu, false);
                        break;
                    default:
                        break;
                }
            }
            //Check if the highlightMenu is enabled
            else if (dcMenu.enabled == true && hlMenu.enabled == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check which button of the highlightMenu was clicked
                switch (hit.transform.gameObject.name)
                {
                    case "DisconnectPrev":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Prev")));
                        break;
                    case "DisconnectNext":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Next")));
                        break;
                    case "DisconnectBoth":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Both")));
                        break;
                    case "Close":
                        //Closes the disconnectMenu
                        ConfigureMenu(dcMenu, false);
                        break;
                    default:
                        break;
                }
            }
        }
        
        if (Input.GetButton("L1-Android"))
        {
            //Reversing current video
            mp.Reverse();
        }
        
        if (Input.GetButton("R1-Android"))
        {
            //Forwarding current video
            mp.Forward();
        }
#else
        //Joystick Controls for Windows
        if (Input.GetButton("A-Windows"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }

        if (Input.GetButtonDown("B-Windows"))
        {
            //Shows the current time
            StartCoroutine(ShowText(mp.GetCurrentPos().ToString()));
        }

        if (Input.GetButtonDown("Y-Windows"))
        {
            //Rewinding current video
            mp.Rewind();

            //Unpauses current video
            pausing = false;
            mp.SetPaused(pausing);
        }

        if (Input.GetButtonDown("X-Windows"))
        {
            //Pauses current video
            pausing = true;
            mp.SetPaused(pausing);

            //Opens StartMenu to switch video
            ConfigureMenu(stMenu, !stMenu.enabled);
        }

        if (Input.GetButton("L1-Windows"))
        {
            //Reversing current video
            reversing = !reversing;
            mp.Reverse(reversing);
        }

        if (Input.GetButton("R1-Windows"))
        {
            //Forwarding current video
            forwarding = !forwarding;
            mp.Forward(forwarding);
        }

        if (Input.GetButtonDown("L2-Windows"))
        {
            //Check if raycast hits the media sphere
            if (dcMenu.enabled == false || hlMenu.enabled == false || stMenu.enabled == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Starts creation of the new highlight
                StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", hit.textureCoord, mp.GetMovieName())));
            }
        }

        if (Input.GetButtonDown("R2-Windows"))
        {
            //Check if all menus are disabled and if the hit object is a highlight
            if (dcMenu.enabled == false && hlMenu.enabled == false && stMenu.enabled == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Pauses current video
                pausing = true;
                mp.SetPaused(pausing);

                //If the highlightMenu is disabled, enables it and shows it infront of the selected highlight
                ConfigureMenu(hlMenu,true);

                //Adjust the highlightMenu with the selected highlight
                Quaternion rot = new Quaternion(hit.transform.gameObject.transform.rotation.x, hit.transform.gameObject.transform.rotation.y, hit.transform.gameObject.transform.rotation.z, hit.transform.gameObject.transform.rotation.w);
                Vector3 pos = new Vector3(hit.transform.gameObject.transform.position.x, hit.transform.gameObject.transform.position.y, hit.transform.gameObject.transform.position.z);

                //Rotate the highlightMenu to face the camera and show it infront of highlight (not in it)
                hlMenu.transform.rotation = rot;
                hlMenu.transform.localRotation = Quaternion.Euler(hlMenu.transform.localEulerAngles.x - 90, hlMenu.transform.localEulerAngles.y, hlMenu.transform.localEulerAngles.z);
                hlMenu.transform.position = pos;
                hlMenu.transform.localPosition = new Vector3(hlMenu.transform.localPosition.x, hlMenu.transform.localPosition.y, hlMenu.transform.localPosition.z * 0.9f);

                //Make the hit highlight the currently selected highlight
                selectedObject = hit.transform.gameObject;
            }
            //Check if the StartMenu is enabled and the dropdown list is closed
            else if (stMenu.enabled == true && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                switch (hit.transform.gameObject.name)
                {
                    case "VideoDropdown":
                        //Open the dropdown list
                        list.Show();

                        //Give every Item own box collider
                        for (int i = 1; i < list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.childCount; i++)
                        {
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.AddComponent<BoxCollider>();
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.GetComponent<BoxCollider>().size = new Vector3(158, 28, 1);
                        }
                        
                        opened = true;
                        break;
                    case "PlayVideo":
                        ConfigureMenu(stMenu, false);

                        //Load save file for currently selected video
                        Load(list.options[list.value].text);

                        //Set the chosen movie in the player and start the playback
                        mp.SetMovieName(list.options[list.value].text);
                        StartCoroutine(ShowText(mp.StartVideo()));
                        break;
                    case "Save":
                        ConfigureMenu(stMenu, false);

                        //Save file for currently selected video
                        Save(list.options[list.value].text);
                        break;
                    default:
                        break;
                }
            }
            //Check if the StartMenu is enabled and the dropdown list is opened
            else if (stMenu.enabled == true && opened == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check for all videos if the shown item is part of possible videos
                foreach (String video in videoList)
                {
                    if (hit.transform.gameObject.name.Contains(video))
                    {
                        //Get item index from hitted objects name
                        selectedIndex = int.Parse(Regex.Replace(hit.transform.gameObject.name.Substring(0, hit.transform.gameObject.name.IndexOf(":")), "[^0-9]", ""));

                        //Set selected item as new top item
                        list.value = selectedIndex;

                        //Hide the dropdown list
                        list.Hide();

                        opened = false;
                        break;
                    }
                }

                //Refresh the dropdown list with new parameters
                list.RefreshShownValue();
            }
            //Check if the highlightMenu is enabled
            else if (dcMenu.enabled == false && hlMenu.enabled == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check which button of the highlightMenu was clicked
                switch (hit.transform.gameObject.name)
                {
                    case "Connect":
                        //Connects the currently selected to another highlight, if possible
                        StartCoroutine(ShowText(mh.ConnectItems(selectedObject)));
                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Disconnect":
                        //Disonnects the currently selected and another highlight
                        ConfigureMenu(dcMenu, true);
                        break;
                    case "GetInfo":
                        //Gets all information about the currently selected highlight
                        String info = selectedObject.GetComponent<HighlightMemory>().getType();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getVideo();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getTime();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getTexPos();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getPrev();
                        info = info + "," + selectedObject.GetComponent<HighlightMemory>().getNext();

                        StartCoroutine(ShowText(info));
                        
                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Delete":
                        //Deletes the currently selected highlight
                        mh.DeleteItem(selectedObject);
                        ConfigureMenu(hlMenu, false);
                        break;
                    case "Close":
                        //Closes the highlightMenu
                        ConfigureMenu(hlMenu, false);
                        break;
                    default:
                        break;
                }
            }
            //Check if the highlightMenu is enabled
            else if (dcMenu.enabled == true && hlMenu.enabled == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check which button of the highlightMenu was clicked
                switch (hit.transform.gameObject.name)
                {
                    case "DisconnectPrev":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Prev")));
                        break;
                    case "DisconnectNext":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Next")));
                        break;
                    case "DisconnectBoth":
                        //Close all opened Menus
                        ConfigureMenu(dcMenu, false);
                        ConfigureMenu(hlMenu, false);

                        //Starts Waiting process for user click
                        StartCoroutine(WaitForButton());

                        //
                        StartCoroutine(ShowText(mh.DisconnectItems(selectedObject, "Both")));
                        break;
                    case "Close":
                        //Closes the disconnectMenu
                        ConfigureMenu(dcMenu, false);
                        break;
                    default:
                        break;
                }
            }
        }
#endif
    }

    void Save(String video)
    {
        //Create Formatter and save file path + name
        BinaryFormatter bf = new BinaryFormatter();
        String filePath = Application.persistentDataPath + "/" + video + ".hl";

        FileStream fs = File.Create(filePath);

        //Write data in savaData
        SaveData data = new SaveData();
        CreateIDList(data);

        //Serialize the file and close the filestream
        bf.Serialize(fs, data);
        fs.Close();
    }

    void Load(String video)
    {
        String filePath = Application.persistentDataPath + "/" + video + ".hl";

        //Clear old list from highlights
        mh.ClearList();

        if (File.Exists(filePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Open(filePath, FileMode.Open);

            SaveData data = (SaveData)bf.Deserialize(fs);
            fs.Close();

            CreateHighlights(data, video);
        }
    }

    void CreateIDList(SaveData data)
    {
        localId[] localIdList = new localId[mh.GetList().Count];
        String nextLocalId = "";

        for (int i = 0; i < mh.GetList().Count; i++)
        {
            //Get local parameters of certain highlight
            String time = mh.GetItem(i).GetComponent<HighlightMemory>().getTime().ToString();
            String type = mh.GetItem(i).GetComponent<HighlightMemory>().getType();
            String texPos = mh.GetItem(i).GetComponent<HighlightMemory>().getTexPos().ToString();
            String pos = mh.GetItem(i).transform.position.ToString();

            //Concatenate all local parameters to one id string
            String idStr = time + "|" + type + "|" + texPos + "|" + pos;

            //Convert id string to id and write localID to localIdList
            localIdList[i] = new localId(mh.GetItem(i), idStr);
        }

        for (int i = 0; i < localIdList.Length; i++)
        {
            //Check if the highlight has a next highlight
            if (localIdList[i].g.GetComponent<HighlightMemory>().getNext() != null)
            {
                foreach (localId li in localIdList)
                {
                    //Check what localID the next highlight has
                    if (li.g == localIdList[i].g.GetComponent<HighlightMemory>().getNext())
                    {
                        //Set found localId to nextLocalId
                        nextLocalId = li.localID;
                        break;
                    }
                }

                //Create global ID from localID and nextLocalID
                data.idList[i] = localIdList[i].localID + "|" + nextLocalId;
            }
        }
    }

    void CreateHighlights(SaveData data, String video)
    {
        List<nextId> nextIdList = new List<nextId>();

        //Part id string for every saved highlight into its parameters
        for (int i = 0; i < data.idList.Count; i++)
        {
            String nextLocalId = "";
            String localHighlight = data.idList[i];

            //Get the time from the highlight
            String strtime = localHighlight.Substring(0, localHighlight.IndexOf("|") - 1);
            localHighlight = localHighlight.Substring(localHighlight.IndexOf("|") + 1);

            //Get the texPos from the highlight
            String strType = localHighlight.Substring(0, localHighlight.IndexOf("|") - 1);
            localHighlight = localHighlight.Substring(localHighlight.IndexOf("|") + 1);

            //Get the type from the highlight
            String strTexPos = localHighlight.Substring(0, localHighlight.IndexOf("|") - 1); ;
            localHighlight = localHighlight.Substring(localHighlight.IndexOf("|") + 1);

            //Get the world position from the highlight
            String strPos = localHighlight.Substring(0, localHighlight.IndexOf("|") - 1); ;
            localHighlight = localHighlight.Substring(localHighlight.IndexOf("|") + 1);

            //Check if this highlight has a connected highlight
            if (localHighlight.Length != 0)
            {
                //Get the nextLocalId from the next highlight
                nextLocalId = localHighlight;
            }

            //Get the time parameter as TimeSpan
            TimeSpan time = TimeSpan.Parse(strtime);

            //Get the texPos parameter as Vector2
            Vector2 texPos;
            texPos = ParseToVector2(strTexPos);

            //Get the texPos parameter as Vector2
            Vector3 pos;
            pos = ParseToVector3(strPos);

            //Get the world position parameter as Vector3
            mh.AddItem(pos, time, strType, texPos, video);

            nextIdList.Add(new nextId(mh.GetItem(i), nextLocalId));
        }

        //Go through every highlgiht with a conirmed next highlight
        foreach (nextId nId in nextIdList)
        {
            String nextLocalId = nId.nextLocalID;

            //Get the time from the highlight
            String strtime = nextLocalId.Substring(0, nextLocalId.IndexOf("|") - 1);
            nextLocalId = nextLocalId.Substring(nextLocalId.IndexOf("|") + 1);

            //Get the texPos from the highlight
            String nextType = nextLocalId.Substring(0, nextLocalId.IndexOf("|") - 1);
            nextLocalId = nextLocalId.Substring(nextLocalId.IndexOf("|") + 1);

            //Get the type from the highlight
            String strTexPos = nextLocalId.Substring(0, nextLocalId.IndexOf("|") - 1); ;
            nextLocalId = nextLocalId.Substring(nextLocalId.IndexOf("|") + 1);

            //Get the world position from the highlight
            String strPos = nextLocalId.Substring(0, nextLocalId.IndexOf("|") - 1); ;
            nextLocalId = nextLocalId.Substring(nextLocalId.IndexOf("|") + 1);

            //Get the time parameter as TimeSpan
            TimeSpan nextTime = TimeSpan.Parse(strtime);

            //Get the texPos parameter as Vector2
            Vector2 nextTexPos;
            nextTexPos = ParseToVector2(strTexPos);

            //Get the texPos parameter as Vector2
            Vector3 nextPos;
            nextPos = ParseToVector3(strPos);

            //Go through every highlight to find the next highlight of the current highlight
            foreach (GameObject go in mh.GetList())
            {
                //Check if this highlight is the next highlight of the current highlight
                if (go.transform.position == nextPos && go.GetComponent<HighlightMemory>().getTexPos() == nextTexPos && go.GetComponent<HighlightMemory>().getTime() == nextTime && go.GetComponent<HighlightMemory>().getType() == nextType)
                {
                    //Connect both highlights with eatch other
                    nId.g.GetComponent<HighlightMemory>().setNext(go);
                    go.GetComponent<HighlightMemory>().setPrev(nId.g);
                }
            }
        }
    }

    private Vector2 ParseToVector2 (String val)
    {
        //Get rid of both brackets
        String str = val.Substring(1, val.Length - 2);

        //Split vector in its both coordinates x and y as strings
        string[] sArray = str.Split(',');

        //Convert both coordinate strings to float and return the vector
        return new Vector2(float.Parse(sArray[0]), float.Parse(sArray[1]));
    }

    private Vector3 ParseToVector3(String val)
    {
        //Get rid of both brackets
        String str = val.Substring(1, val.Length - 2);

        //Get the x coordinate from pos string
        String x = str.Substring(0, str.IndexOf(",") - 1);
        String remain = str.Substring(str.IndexOf(",") + 1, str.Length);

        //Get the y coordinate from pos string
        String y = remain.Substring(0, remain.IndexOf(",") - 1);
        remain = remain.Substring(remain.IndexOf(",") + 1, remain.Length);

        //Get the z coordinate from pos string
        String z = remain.Substring(0, remain.IndexOf(",") - 1);
        remain = remain.Substring(remain.IndexOf(",") + 1, remain.Length);

        //Convert both coordinate strings to float and return the vector
        return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
    }

    private void ConfigureMenu (Canvas menu, bool status)
    {
        //Enable/Disable every collider this menu has
        foreach (Transform child in menu.transform)
        {
            if (child.name != "DisconnectMenu")
            {
                child.GetComponent<Collider>().enabled = status;
            }
        }

        //Enable/Disable this menu
        menu.enabled = status;
    }

    //Shows text for certain time in world space
    IEnumerator ShowText(String text)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        vrMenu.GetComponent<Text>().text = text;
        yield return new WaitForSecondsRealtime(showTime);
        vrMenu.GetComponent<Text>().text = "";
#elif (UNITY_STANDALONE_WIN || UNITY_EDITOR)
        vrMenu.GetComponent<Text>().text = text;
        yield return new WaitForSecondsRealtime(showTime);
        vrMenu.GetComponent<Text>().text = "";
#endif
    }

    //Waits for Input from R2
    IEnumerator WaitForButton()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        while (!Input.GetButtonDown("R2-Android"))
            yield return null;
#elif (UNITY_STANDALONE_WIN || UNITY_EDITOR)
        while (!Input.GetButtonDown("R2-Windows"))
            yield return null;
#endif
    }
}