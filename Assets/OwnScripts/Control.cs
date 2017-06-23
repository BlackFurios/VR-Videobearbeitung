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
    private ManageHighlights mh;                                //Instance of the ManageHighlights script
    private MediaPlayer mp;                                     //Instance of the MediaPlayer script

    private Dropdown list;                                      //Instance of the dropdown list object

    private Canvas vrMenu;                                      //Instance of the VRMenu object
    private Canvas stMenu;                                      //Instance of the StartMenu object

    private RaycastHit hit;                                     //Point where the raycast hits
    private int layerMask = 1 << 8;                             //LayerMask with layer of the highlights

    private GameObject conObject;                               //Highlight which the user is connecting
    private GameObject modObject;                               //Highlight which the user is modifying
    private int selectedIndex;                                  //Dropdown index which the user has selected

    private List<String> videoList = new List<string>();        //List of currently possible movies
    
    private bool opened = false;                                //Is the drodown list opened
    private bool pausing = false;                               //Is the video currently paused
    private int showTime = 1;                                   //How long texts should be shown in seconds

    private String savePath;                                    //Absolute path of the save files

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
#if (UNITY_ANDROID && !UNITY_EDITOR)
        savePath = @"/storage/emulated/0/Movies/VR-Videoschnitt/Saves";
#else
        savePath = @"C:/Users/" + Environment.UserName + "/Documents/VR-Videoschnitt/Saves";
#endif

        mh = GetComponent<ManageHighlights>();
        mp = GetComponent<MediaPlayer>();

        //Invert the layerMask to only ignore the the highlight layer
        layerMask = ~layerMask;

        for (int i = 0; i < mp.GetMovieList().Count; i++)
        {
            //Fill movieList with detected videos
            videoList.Add(mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")));
        }

        //Search for VRMenu and highlightMenu
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            if (c.name == "VRMenu")
            {
                vrMenu = c;
            }
            if (c.name == "StartMenu")
            {
                stMenu = c;
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
        if (Input.GetButton("A-Android"))
        {

        }

        //Check if the B-Button is pressed
        if (Input.GetButton("B-Android"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }

        //Check if the X-Button is pressed
        if (Input.GetButton("X-Android"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                StartCoroutine(ShowText(mh.DeleteItem(hit.transform.gameObject)));
            }
        }

        if (Input.GetButton("Y-Android"))
        {

        }

        if (Input.GetButtonDown("R1-Android"))
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                if (conObject != null && hit.transform.gameObject.name ==  "Highlight(Clone)")
                {
                    mh.ConnectItems(conObject, hit.transform.gameObject);
                    conObject = null;
                }
                else if (conObject != null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    Vector3 hitPos = hit.point - Camera.main.transform.position;
                    hitPos.x = hitPos.x * 0.9f;
                    hitPos.y = hitPos.y * 0.9f;
                    hitPos.z = hitPos.z * 0.9f;

                    mh.DrawLine(conObject, hitPos);
                }
                else
                {
                    conObject = hit.transform.gameObject;
                }
            }
        }

        if (Input.GetButton("L1-Android"))
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                if (conObject != null)
                {
                    mh.DisconnectItems(conObject);
                    conObject = null;
                }
                else if (conObject == null)
                {
                    conObject = hit.transform.gameObject;
                }
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButtonDown("R2-Android"))
        {
            // Check if the StartMenu is enabled and the dropdown list is closed
            if (stMenu.enabled == true && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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
                    default:
                        break;
                }
            }

            //Check if the StartMenu is enabled and the dropdown list is opened
            if (stMenu.enabled == true && opened == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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

            //Check if raycast hits the media sphere
            if (stMenu.enabled == false && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check if highlight was already spawned and needs to be manipulated
                if (modObject == null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    //Get the correct texture coordinates on the video texture
                    Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                    Vector2 coords = hit.textureCoord;
                    coords.x *= tex.width;
                    coords.y *= tex.height;

                    //Starts creation of the new highlight
                    StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords, mp.GetMovieName())));

                    //Iterate through list of all highlights
                    foreach (GameObject g in mh.GetList())
                    {
                        //Check for newly created highlight in list of all highlights
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
                        {
                            //Make the newly created highlight the modObject
                            modObject = g;
                            break;
                        }
                    }
                }

                //Check if user wants to manipulate an already existing highlight
                if (modObject == null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Set hit highlight as modObject
                    modObject = hit.transform.gameObject;
                }
            }
        }

        if (modObject != null && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Translate highlight while it is selected
            modObject.GetComponent<HighlightMemory>().setTime(mp.GetCurrentPos());
            mh.MoveItem(modObject, hit.point);
        }

        //Check if the R2-Button is not pressed anymore
        if (Input.GetButtonUp("R2-Android"))
        {
            if (modObject != null && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;
                coords.x *= tex.width;
                coords.y *= tex.height;

                mh.ModifyItem(modObject, hit.point, mp.GetCurrentPos(), modObject.GetComponent<HighlightMemory>().getType(), coords);

                //Deselect highlight
                modObject = null;
            }
        }

        if (Input.GetButton("L2-Android"))
        {
            Save(mp.GetMovieName());
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Android") > 0)
        {
            if ((mp.GetMovieLength().TotalSeconds - mp.GetCurrentPos().TotalSeconds) < 5) 
            {
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        int index = (i + 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                mp.JumpToPos((int) mp.GetMovieLength().TotalSeconds - 3);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Android") < 0)
        {
            if (mp.GetCurrentPos().TotalSeconds < 5)
            {
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        int index = (i - 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                mp.Rewind();
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") > 0)
        {
            mp.SetPlaybackSpeed(1);
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") < 0)
        {
            mp.SetPlaybackSpeed(2);
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Android") == 0)
        {
            mp.SetPlaybackSpeed(0);
        }
#else
        if (Input.GetButton("A-Windows"))
        {

        }

        //Check if the B-Button is pressed
        if (Input.GetButton("B-Windows"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }

        //Check if the X-Button is pressed
        if (Input.GetButton("X-Windows"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                StartCoroutine(ShowText(mh.DeleteItem(hit.transform.gameObject)));
            }
        }

        if (Input.GetButton("Y-Windows"))
        {

        }

        if (Input.GetButtonDown("R1-Windows"))
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                if (conObject != null && hit.transform.gameObject.name ==  "Highlight(Clone)")
                {
                    mh.ConnectItems(conObject, hit.transform.gameObject);
                    conObject = null;
                }
                else if (conObject != null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    Vector3 hitPos = hit.point - Camera.main.transform.position;
                    hitPos.x = hitPos.x * 0.9f;
                    hitPos.y = hitPos.y * 0.9f;
                    hitPos.z = hitPos.z * 0.9f;

                    mh.DrawLine(conObject, hitPos);
                }
                else
                {
                    conObject = hit.transform.gameObject;
                }
            }
        }

        if (Input.GetButton("L1-Windows"))
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name == "Highlight(Clone)")
            {
                if (conObject != null)
                {
                    mh.DisconnectItems(conObject);
                    conObject = null;
                }
                else if (conObject == null)
                {
                    conObject = hit.transform.gameObject;
                }
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButtonDown("R2-Windows"))
        {
            // Check if the StartMenu is enabled and the dropdown list is closed
            if (stMenu.enabled == true && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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
                    default:
                        break;
                }
            }

            //Check if the StartMenu is enabled and the dropdown list is opened
            if (stMenu.enabled == true && opened == true && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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

            //Check if raycast hits the media sphere
            if (stMenu.enabled == false && opened == false && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check if highlight was already spawned and needs to be manipulated
                if (modObject == null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    //Get the correct texture coordinates on the video texture
                    Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                    Vector2 coords = hit.textureCoord;
                    coords.x *= tex.width;
                    coords.y *= tex.height;

                    //Starts creation of the new highlight
                    StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords, mp.GetMovieName())));

                    //Iterate through list of all highlights
                    foreach (GameObject g in mh.GetList())
                    {
                        //Check for newly created highlight in list of all highlights
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
                        {
                            //Make the newly created highlight the modObject
                            modObject = g;
                            break;
                        }
                    }
                }

                //Check if user wants to manipulate an already existing highlight
                if (modObject == null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Set hit highlight as modObject
                    modObject = hit.transform.gameObject;
                }
            }
        }

        if (modObject != null && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Translate highlight while it is selected
            modObject.GetComponent<HighlightMemory>().setTime(mp.GetCurrentPos());
            mh.MoveItem(modObject, hit.point);
        }

        //Check if the R2-Button is not pressed anymore
        if (Input.GetButtonUp("R2-Windows"))
        {
            if (modObject != null && Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;
                coords.x *= tex.width;
                coords.y *= tex.height;

                mh.ModifyItem(modObject, hit.point, mp.GetCurrentPos(), modObject.GetComponent<HighlightMemory>().getType(), coords);

                //Deselect highlight
                modObject = null;
            }
        }

        if (Input.GetButton("L2-Windows"))
        {
            Save(mp.GetMovieName());
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Windows") > 0)
        {
            if ((mp.GetMovieLength().TotalSeconds - mp.GetCurrentPos().TotalSeconds) < 5) 
            {
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        int index = (i + 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                mp.JumpToPos((int) mp.GetMovieLength().TotalSeconds - 3);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Windows") < 0)
        {
            if (mp.GetCurrentPos().TotalSeconds < 5)
            {
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        int index = (i - 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                mp.Rewind();
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") > 0)
        {
            mp.SetPlaybackSpeed(1);
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") < 0)
        {
            mp.SetPlaybackSpeed(2);
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Windows") == 0)
        {
            mp.SetPlaybackSpeed(0);
        }
#endif
    }

    void Save(String video)
    {
        StartCoroutine(ShowText(video + " is saving..."));

        //Create Formatter and save file path + name
        BinaryFormatter bf = new BinaryFormatter();
        String filePath = savePath + "/" + video + ".hl";

        //Check if directory is not created
        if (!Directory.Exists(savePath))
        {
            //Create the directory
            Directory.CreateDirectory(savePath);
        }
        
        //Check if file is already created
        if (File.Exists(filePath))
        {
            //Clear the file
            File.WriteAllText(filePath, String.Empty);
        }

        //Create/Open the save file
        FileStream fs = File.Open(filePath, FileMode.OpenOrCreate);

        //Write data in savaData
        SaveData data = new SaveData();
        CreateIDList(data);

        StartCoroutine(ShowText(data.idList.Count.ToString()));

        //Serialize the file and close the filestream
        bf.Serialize(fs, data);
        fs.Close();

        //StartCoroutine(ShowText(video + " is saved"));
    }

    void Load(String video)
    {
        String filePath = savePath + video + ".hl";

        //Clear old list from highlights
        mh.ClearList();

        if (File.Exists(filePath))
        {
            StartCoroutine(ShowText(video + " is loading..."));

            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Open(filePath, FileMode.Open);

            SaveData data = (SaveData)bf.Deserialize(fs);
            fs.Close();

            CreateHighlights(data, video);

            StartCoroutine(ShowText(video + " is loaded"));
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
            }
            //Create global ID from localID and nextLocalID
            data.idList.Add(localIdList[i].localID + "|" + nextLocalId);
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

    private Vector2 ParseToVector2(String val)
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

    private void ConfigureMenu(Canvas menu, bool status)
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
