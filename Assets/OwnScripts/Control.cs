using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Control : MonoBehaviour
{
    private ManageHighlights    mh;                                             //Instance of the ManageHighlights script
    private MediaPlayer         mp;                                             //Instance of the MediaPlayer script
    private EDLConverter        ec;                                             //Instance of the EDLConverter script

    private Dropdown            list;                                           //Instance of the dropdown list object

    private Canvas              vrMenu;                                         //Instance of the VRMenu object
    private Canvas              stMenu;                                         //Instance of the StartMenu object

    private RaycastHit          hit;                                            //Point where the raycast hits
    private int                 layerMask = 1 << 8;                             //LayerMask with layer of the highlights
    
    private int                 selectedIndex;                                  //Dropdown index which the user has selected

    private List<String>        videoList = new List<string>();                 //List of currently possible movies

    private List<Vector3>       spawnPosList = new List<Vector3>();             //The world position list of the currently created highlight
    private List<Vector2>       spawnTexPosList = new List<Vector2>();          //The texture position list of the currently created highlight
    private List<TimeSpan>      spawnTimeList = new List<TimeSpan>();           //The time positione list of the currently created highlight
    private List<GameObject>    spawnObjectList = new List<GameObject>();       //The object list of the currently created highlight

    private int                 spawnRate = 100;                                //The spawn rate in which new highlight positions should be created (in milliseconds)
    private TimeSpan            lastSpawn = TimeSpan.Zero;                      //The time position of the last highlight position that was created 

    private bool                opened = false;                                 //Is the drodown list opened
    private bool                pausing = false;                                //Is the video currently paused
    private bool                timeShown = false;                              //Is currently a text shown
    private int                 showTime = 1;                                   //How long texts should be shown in seconds

    private float               delTimer;                                       //Timer for long button press on X-Button
    private float               saveTimer;                                      //Timer for long button press on L2-Button

    private String              savePath;                                       //Absolute path of the save files
    private String              edlPath;                                        //Absolute path of the edl files

    public class localId                                                        //Struct for a highlight and its own local if (without next and prev parameters)
    {
        public ManageHighlights.Highlight g;                                    //The highlight
        public String localID;                                                  //The highlights localID

        public localId(ManageHighlights.Highlight a, String b)                  //Constructor of the localID struct
        {
            g = a;
            localID = b;
        }
    }

    public class nextId                                                         //Struct for a highlight and its next highlight as id
    {
        public ManageHighlights.Highlight g;                                    //The highlight
        public String nextLocalID;                                              //The highlights next highlight as its localID

        public nextId(ManageHighlights.Highlight a, String b)                   //Constructor of the nextID struct
        {
            g = a;
            nextLocalID = b;
        }
    }

    // Use this for initialization
    void Start ()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        //The save path on a android system
        savePath = @"/storage/emulated/0/Movies/VR-Videoschnitt/saves";
        edlPath = @"/storage/emulated/0/Movies/VR-Videoschnitt";
#else
        //The save path on a windows system
        savePath = @"C:/Users/" + Environment.UserName + "/Documents/VR-Videoschnitt/saves";
        edlPath = @"C:/Users/" + Environment.UserName + "/Documents/VR-Videoschnitt";
#endif

        //Sets the manageHighlights script
        mh = GetComponent<ManageHighlights>();

        //Sets the mediaPlayer script
        mp = GetComponent<MediaPlayer>();

        //Sets the edlConverter script
        ec = GetComponent<EDLConverter>();

        //Invert the layerMask to only ignore the the highlight layer
        layerMask = ~layerMask;

        //Iterate through the list of all found videos
        for (int i = 0; i < mp.GetMovieList().Count; i++)
        {
            //Fill movieList with detected videos
            videoList.Add(mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")));
        }

        //Search for VRMenu and highlightMenu
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            //Check if the currently found canvas is the VRMenu
            if (c.name == "VRMenu")
            {
                //Sets the VRMenu
                vrMenu = c;
            }

            //Check if the currently found canvas is the StartMenu
            if (c.name == "StartMenu")
            {
                //Sets the StartMenu
                stMenu = c;
            }
        }

        //Search for VideoDropdownList
        list = FindObjectOfType<Dropdown>();

        //Add all found videos to the dropdown list
        list.GetComponent<Dropdown>().AddOptions(videoList);
    }
	
	// Update is called once per frame
	void Update ()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        //Check if the A-Button is pressed
        if (Input.GetButtonDown("A-Android"))
        {

        }

        //Check if the B-Button is pressed
        if (Input.GetButtonDown("B-Android"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }

        //Check if the X-Button is pressed
        if (Input.GetButtonDown("X-Android"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Iterate through the list of all managed highlights
                foreach (ManageHighlights.Highlight h in mh.GetList())
                {
                    //Check for the currently selected highlight in the list of all managed highlights
                    if (h.getPos().Contains(hit.transform.position) && h.getTime().Contains(mp.GetCurrentPos()))
                    {
                        //Delete selected highlight
                        mh.DeleteHighlight(h);
                        break;
                    }
                }
            }
            else
            {
                //Clear complete highlight list and delete all highlights at once
                mh.DeleteAllHighlights();
            }  
        }

        //Check if the Y-Button is pressed
        if (Input.GetButtonDown("Y-Android"))
        {
            //Check if text is already shown
            if (!timeShown)
            {
                //Show the timeline of the currently played video (Current time position|Total video length)
                ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString());
            }
            else
            {
                //Show nothing
                ShowText(String.Empty);
            }
        }

        //Check if the R1-Button is pressed
        if (Input.GetButton("R1-Android"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name != "Highlight(Clone)" && 
                mp.GetCurrentPos().Subtract(lastSpawn).TotalMilliseconds > spawnRate)
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;

                //Scale the texture coordinates on the whole picture size
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Add new parameters to spawn lists
                spawnPosList.Add(mh.CalculateHighlightPosition(hit.point));
                spawnTexPosList.Add(coords);
                spawnTimeList.Add(mp.GetCurrentPos());
                
                //Set the lastSpawn time position to the current time position
                lastSpawn = mp.GetCurrentPos();

                //Spawn a highlight to give feedback to the user
                spawnObjectList.Add(mh.SpawnHighlight(hit.point));
            }
        }

        //Check if the R1-Button not pressed anymore
        if (Input.GetButtonUp("R1-Android") && spawnPosList.Count != 0 && spawnTexPosList.Count != 0 && spawnTimeList.Count != 0)
        {
            //Create highlight
            mh.AddItem(spawnPosList, spawnTexPosList, spawnTimeList, "Cut");

            //Empty all spawn parameter lists
            spawnPosList.Clear();
            spawnTexPosList.Clear();
            spawnTimeList.Clear();

            //Destroy all spawn highlight objects
            foreach (GameObject g in spawnObjectList)
            {
                Destroy(g);
            }
            //Empty the list of spawned highlight objects
            spawnObjectList.Clear();

            //Notify user that a highlight was created
            StartCoroutine(ShowTextForTime("Highlight created"));
        }

        //Check if the L1-Button is pressed
        if (Input.GetButtonDown("L1-Android"))
        {
            //Check if the highlight list is empty
            if (mh.GetList().Count == 0)
            {
                //Show text to the user to inform him that the highlight list is empty
                StartCoroutine(ShowTextForTime("You need to spawn Highlights in order to create an EDL"));
            }
            else
            {
                //Create a edl file from the current highlights for the selected video
                CreateEDL(mp.GetMovieName());
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButton("R2-Android"))
        {
            //Check if the StartMenu is enabled and the dropdown list is closed
            if (stMenu.enabled == true && opened == false && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                switch (hit.transform.gameObject.name)
                {
                    //The dropdown list is selected
                    case "VideoDropdown":
                        //Open the dropdown list
                        list.Show();

                        //Give every Item own box collider
                        for (int i = 1; i < list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.childCount; i++)
                        {
                            //Create a collider for every item in the dropdown list
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.AddComponent<BoxCollider>();
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.GetComponent<BoxCollider>().size = new Vector3(158, 28, 1);
                        }

                        //Notify that the dropdown list is currently opened
                        opened = true;
                        break;
                    //The "Play Video" button is pressed
                    case "PlayVideo":
                        //Toggle the visibility and interaction of the StartMenu
                        ConfigureMenu(stMenu, false);

                        //Load save file for currently selected video
                        Load(list.options[list.value].text);

                        //Set the chosen video in the player and start the playback
                        mp.SetMovieName(list.options[list.value].text);

                        //Start the selected video
                        StartCoroutine(ShowTextForTime(mp.StartVideo()));
                        break;
                    default:
                        break;
                }
            }

            //Check if the StartMenu is enabled and the dropdown list is opened
            if (stMenu.enabled == true && opened == true && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check for all videos if the shown item is part of possible videos
                foreach (String video in videoList)
                {
                    //Check if raycast hits a valid video option
                    if (hit.transform.gameObject.name.Contains(video))
                    {
                        //Get item index from hitted objects name
                        selectedIndex = int.Parse(Regex.Replace(hit.transform.gameObject.name.Substring(0, hit.transform.gameObject.name.IndexOf(":")), "[^0-9]", ""));

                        //Set selected item as new top item
                        list.value = selectedIndex;

                        //Hide the dropdown list
                        list.Hide();

                        //Notify that the dropdown list is closed
                        opened = false;
                        break;
                    }
                }

                //Refresh the dropdown list with new parameters
                list.RefreshShownValue();
            }
        }

        //Check if the L"-Button is pressed
        if (Input.GetButtonDown("L2-Android"))
        {
            //Check if the highlight list is empty
            if (mh.GetList().Count == 0)
            {
                //Show text to the user to inform him that the highlight list is empty
                StartCoroutine(ShowTextForTime("You need to spawn Highlights in order to save a file"));
            }
            else
            {
                //Save the current state of all highlights for the selected video
                Save(mp.GetMovieName());
            }
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxis("DPad-Vertical-Android") == 1)
        {
            //Check if the currently playing video is at its end
            if ((mp.GetMovieLength().TotalSeconds - mp.GetCurrentPos().TotalSeconds) < 5)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i + 1) % mp.GetMovieList().Count;

                        //Check if the index reached the end of the list
                        if (index < 0)
                        {
                            //Let the index wrap around to make the list circular
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowTextForTime(mp.StartVideo()));
            }
            else
            {
                //Jump to the end of the video
                mp.JumpToPos((int)mp.GetMovieLength().TotalSeconds - 3);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxis("DPad-Vertical-Android") == -1)
        {
            //Check if the currently playing video is at its beginning
            if (mp.GetCurrentPos().TotalSeconds < 5)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i - 1) % mp.GetMovieList().Count;

                        //Check if the index reached the end of the list
                        if (index < 0)
                        {
                            //Let the index wrap around to make the list circular
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowTextForTime(mp.StartVideo()));
            }
            else
            {
                //Rewind to the start of the video
                StartCoroutine(ShowTextForTime(mp.Rewind()));
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") > 0)
        {
            //Set the playback speed to double
            mp.SetPlaybackSpeed(1);
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") < 0)
        {
            //Set the playback speed to half
            mp.SetPlaybackSpeed(2);
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Android") == 0)
        {
            //Set the playback speed to normal
            mp.SetPlaybackSpeed(0);
        }
#else
        //Check if the A-Button is pressed
        if (Input.GetButtonDown("A-Windows"))
        {

        }

        //Check if the B-Button is pressed
        if (Input.GetButtonDown("B-Windows"))
        {
            //Pauses current video
            pausing = !pausing;
            mp.SetPaused(pausing);
        }

        //Check if the X-Button is pressed
        if (Input.GetButtonDown("X-Windows"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Iterate through the list of all managed highlights
                foreach (ManageHighlights.Highlight h in mh.GetList())
                {
                    //Check for the currently selected highlight in the list of all managed highlights
                    if (h.getPos().Contains(hit.transform.position) && h.getTime().Contains(mp.GetCurrentPos()))
                    {
                        //Delete selected highlight
                        mh.DeleteHighlight(h);

                        StartCoroutine(ShowTextForTime("Highlight deleted"));
                        break;
                    }
                }
            }
            else
            {
                //Clear complete highlight list and delete all highlights at once
                mh.DeleteAllHighlights();

                StartCoroutine(ShowTextForTime("All Highlights deleted"));
            }  
        }

        //Check if the Y-Button is pressed
        if (Input.GetButtonDown("Y-Windows"))
        {
            //Check if text is already shown
            if (!timeShown)
            {
                //Show the timeline of the currently played video (Current time position|Total video length)
                ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString());
            }
            else
            {
                //Show nothing
                ShowText(String.Empty);
            }
        }

        //Check if the R1-Button is pressed
        if (Input.GetButton("R1-Windows"))
        {
            //Check if raycast hits the media sphere
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name != "Highlight(Clone)" && 
                mp.GetCurrentPos().Subtract(lastSpawn).TotalMilliseconds > spawnRate)
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;

                //Scale the texture coordinates on the whole picture size
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Add new parameters to spawn lists
                spawnPosList.Add(mh.CalculateHighlightPosition(hit.point));
                spawnTexPosList.Add(coords);
                spawnTimeList.Add(mp.GetCurrentPos());

                //Set the lastSpawn time position to the current time position
                lastSpawn = mp.GetCurrentPos();

                //Spawn a highlight to give feedback to the user
                spawnObjectList.Add(mh.SpawnHighlight(hit.point));
            }
        }

        //Check if the R1-Button not pressed anymore
        if (Input.GetButtonUp("R1-Windows") && spawnPosList.Count != 0 && spawnTexPosList.Count != 0 && spawnTimeList.Count != 0)
        {
            //Create highlight
            mh.AddItem(spawnPosList, spawnTexPosList, spawnTimeList, "Cut");

            //Empty all spawn parameter lists
            spawnPosList.Clear();
            spawnTexPosList.Clear();
            spawnTimeList.Clear();

            //Destroy all spawn highlight objects
            foreach (GameObject g in spawnObjectList)
            {
                Destroy(g);
            }
            //Empty the list of spawned highlight objects
            spawnObjectList.Clear();

            //Notify user that a highlight was created
            StartCoroutine(ShowTextForTime("Highlight created"));
        }

        //Check if the L1-Button is pressed
        if (Input.GetButtonDown("L1-Windows"))
        {
            //Check if the highlight list is empty
            if (mh.GetList().Count == 0)
            {
                //Show text to the user to inform him that the highlight list is empty
                StartCoroutine(ShowTextForTime("You need to spawn Highlights in order to create an EDL"));
            }
            else
            {
                //Create a edl file from the current highlights for the selected video
                CreateEDL(mp.GetMovieName());
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButton("R2-Windows"))
        {
            //Check if the StartMenu is enabled and the dropdown list is closed
            if (stMenu.enabled == true && opened == false && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                switch (hit.transform.gameObject.name)
                {
                    //The dropdown list is selected
                    case "VideoDropdown":
                        //Open the dropdown list
                        list.Show();

                        //Give every Item own box collider
                        for (int i = 1; i < list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.childCount; i++)
                        {
                            //Create a collider for every item in the dropdown list
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.AddComponent<BoxCollider>();
                            list.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.gameObject.GetComponent<BoxCollider>().size = new Vector3(158, 28, 1);
                        }

                        //Notify that the dropdown list is currently opened
                        opened = true;
                        break;
                    //The "Play Video" button is pressed
                    case "PlayVideo":
                        //Toggle the visibility and interaction of the StartMenu
                        ConfigureMenu(stMenu, false);

                        //Load save file for currently selected video
                        Load(list.options[list.value].text);

                        //Set the chosen video in the player and start the playback
                        mp.SetMovieName(list.options[list.value].text);

                        //Start the selected video
                        StartCoroutine(ShowTextForTime(mp.StartVideo()));
                        break;
                    default:
                        break;
                }
            }

            //Check if the StartMenu is enabled and the dropdown list is opened
            if (stMenu.enabled == true && opened == true && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check for all videos if the shown item is part of possible videos
                foreach (String video in videoList)
                {
                    //Check if raycast hits a valid video option
                    if (hit.transform.gameObject.name.Contains(video))
                    {
                        //Get item index from hitted objects name
                        selectedIndex = int.Parse(Regex.Replace(hit.transform.gameObject.name.Substring(0, hit.transform.gameObject.name.IndexOf(":")), "[^0-9]", ""));

                        //Set selected item as new top item
                        list.value = selectedIndex;

                        //Hide the dropdown list
                        list.Hide();

                        //Notify that the dropdown list is closed
                        opened = false;
                        break;
                    }
                }

                //Refresh the dropdown list with new parameters
                list.RefreshShownValue();
            }
        }

        //Check if the L"-Button is pressed
        if (Input.GetButtonDown("L2-Windows"))
        {
            //Check if the highlight list is empty
            if (mh.GetList().Count == 0)
            {
                //Show text to the user to inform him that the highlight list is empty
                StartCoroutine(ShowTextForTime("You need to spawn Highlights in order to save a file"));
            }
            else
            {
                //Save the current state of all highlights for the selected video
                Save(mp.GetMovieName());
            }
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxis("DPad-Vertical-Windows") == 1)
        {
            //Check if the currently playing video is at its end
            if ((mp.GetMovieLength().TotalSeconds - mp.GetCurrentPos().TotalSeconds) < 5)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i + 1) % mp.GetMovieList().Count;

                        //Check if the index reached the end of the list
                        if (index < 0)
                        {
                            //Let the index wrap around to make the list circular
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowTextForTime(mp.StartVideo()));
            }
            else
            {
                //Jump to the end of the video
                mp.JumpToPos((int)mp.GetMovieLength().TotalSeconds - 3);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxis("DPad-Vertical-Windows") == -1)
        {
            //Check if the currently playing video is at its beginning
            if (mp.GetCurrentPos().TotalSeconds < 5)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i - 1) % mp.GetMovieList().Count;

                        //Check if the index reached the end of the list
                        if (index < 0)
                        {
                            //Let the index wrap around to make the list circular
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowTextForTime(mp.StartVideo()));
            }
            else
            {
                //Rewind to the start of the video
                StartCoroutine(ShowTextForTime(mp.Rewind()));
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") > 0)
        {
            //Set the playback speed to double
            mp.SetPlaybackSpeed(1);
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") < 0)
        {
            //Set the playback speed to half
            mp.SetPlaybackSpeed(2);
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Windows") == 0)
        {
            //Set the playback speed to normal
            mp.SetPlaybackSpeed(0);
        }
#endif
        //Check if time is already shown
        if (timeShown)
        {   //Show the updated time to the user
            ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString());
        }
    }

    //Creates or updates an existing edl file with all currently spawned highlights of the currently active video
    void CreateEDL(String video)
    {
        //Show the user it started the saving process
        StartCoroutine(ShowTextForTime("EDL for " + video + " is creating..."));

        //Create edl file path + name
        String filePath = edlPath + "/" + video + ".edl";

        //Check if directory is not created
        if (!Directory.Exists(edlPath))
        {
            //Create the directory
            Directory.CreateDirectory(edlPath);
        }

        //Check if file is already created
        if (File.Exists(filePath))
        {
            //Clear the file
            File.WriteAllText(filePath, String.Empty);
        }

        //Check if the highlight list is not empty
        if (mh.GetList().Count > 0)
        {
            //Create/Open the edl file
            StreamWriter sw = new StreamWriter(filePath);

            //Write edl header
            sw.WriteLine("TITLE: " + video.ToUpper() + "SEQUENCE");
            sw.WriteLine("FCM: NON-DROP FRAME");
            sw.WriteLine();

            //The currently checked highlight
            ManageHighlights.Highlight current;

            //The type of the highlight (Cut, Dissolve, Wipe)
            String type;

            //Source start and end point
            TimeSpan srcIN;
            TimeSpan srcOUT;

            //Record start and end point
            TimeSpan recIN = TimeSpan.Zero;
            TimeSpan recOUT = TimeSpan.Zero;

            //Iterate through all spawned highlights
            for (int i = 1; i <= mh.GetList().Count; i++)
            {
                //Set the currently checked item as the current item of the list of highlights
                current = mh.GetItem(i - 1);

                //Set the source start point and source end point of the clip of the single highlight
                srcIN = current.getTime().First<TimeSpan>();
                srcOUT = current.getTime().Last<TimeSpan>();

                //Set the type
                type = current.getType();

                //Set the end point of the clip
                recOUT += srcOUT.Subtract(srcIN);

                //Convert the given parameters to a complete edl line
                sw.WriteLine(ec.ConvertToEdlLine(i, 0, type, srcIN, srcOUT, recIN, recOUT));
                sw.WriteLine("* FROM CLIP NAME: " + video.ToUpper() + ".MP4");
                sw.WriteLine();

                //Set the start point of the new clip at the end of the last clip
                recIN += recOUT.Subtract(recIN);
            }

            //Close the streamwriter
            sw.Close();

            //Show the user it finished the saving process
            StartCoroutine(ShowTextForTime("EDL for " + video + " is created"));
        }
        else
        {
            //Show the user it finished the saving process
            StartCoroutine(ShowTextForTime("No Highlights are spawned"));
        }
    }

    //Opens an already existing edl file
    void OpenEDL(String video)
    {
        //Show the user it started the loading process
        StartCoroutine(ShowTextForTime("EDL for " + video + " is loading..."));

        //Create edl file path + name
        String filePath = edlPath + "/" + video + ".edl";

        //Check if file is already created
        if (!File.Exists(filePath))
        {

        }
        else
        {
            //Characters which should be used to split the edl line
            char[] whitespace = new char[] { ' ', '\t' };

            //Read all lines from the found file
            String[] content = File.ReadAllLines(filePath);

            //All edl lines which belong to each other
            List<String> body = new List<String>();

            //The video from which the lines in body are extracted
            String extrVideo = String.Empty;

            //Iterate through all lines of the file
            for (int i = 0; i < content.Length; i++)
            {
                //Check if the edl line is empty
                if (content[i] == String.Empty)
                {
                    //The extracted parameters of the analysed edl line (transition, mode, srcIN, srcOUT, etc)
                    String[] parameters = new String[5];

                    List<TimeSpan> hlTime = new List<TimeSpan>();

                    //Iterate through the body
                    foreach (String str in body)
                    {
                        //All words of the analysed edl line seperated from each other
                        String[] words = str.Split(whitespace);
                        
                        switch(words[0])
                        {
                            //Check if the currently analysed line is the first line of the edl file
                            case "TITLE:":
                                break;
                            //Check if the currently analysed line is the second line of the edl file
                            case "FCM:":
                                break;
                            //Check if the currently analysed line is a FROM CLIP NAME: <videofile>
                            case "*":
                                extrVideo = words[4];
                                break;
                            //Check if the currently analysed line is a effect line (Those are currently ignored)
                            case "M2":
                                Debug.Log("Effects will be ignored");
                                break;
                            //Check if the currently analysed line is a parameter line which can be used to create highlights
                            default:
                                parameters = ec.ConvertFromEdlLine(words);
                                break;
                        }
                    }

                    //Check if the new highlight/chain is for the currently active video
                    if (extrVideo.Substring(0, extrVideo.LastIndexOf(".")).ToUpper() == video.ToUpper())
                    {
                        //Set currentTime to the start time of the highlight/chain
                        TimeSpan currentTime = TimeSpan.Parse(parameters[3]);

                        //Add the next time position to the list of all time positions of the highlight
                        hlTime.Add(currentTime);

                        //While the line has not ended spawn new highlights
                        while(currentTime < TimeSpan.Parse(parameters[4]))
                        {
                            //Set currentTime to the next time position (currentTime + 0.5 seconds)
                            currentTime.Add(TimeSpan.FromMilliseconds(spawnRate));

                            //Add the next time position to the list of all time positions of the highlight
                            hlTime.Add(currentTime);
                        }
                    }

                    //Create a highlight from the parameters
                    mh.AddItem(new List<Vector3>(), new List<Vector2>(), hlTime, parameters[2]);

                    //Clear the complete body after creating the necessary highlights
                    body.Clear();
                }
                else
                {
                    //Add the current line to the body of connected lines
                    body.Add(content[i]);
                }
            }

            //Show the user it finished the loading process
            StartCoroutine(ShowTextForTime("EDL for " + video + " is loaded"));
        }
    }

    //Returns the transition mode for the selected highlight/chain
    int GetTransitionNumber(ManageHighlights.Highlight highlight)
    {
        int trans;

        //Define the type of highlight for the edl (3 types possible)
        switch (highlight.getType())
        {
            //2 -> Wipe
            case "Wipe":
                trans = 2;
                break;
            //1 -> Dissolve
            case "Dissolve":
                trans = 1;
                break;
            //0 -> Cut
            default:
                trans = 0;
                break;
        }
        
        return trans;
    }

    //Saves the highlights of the video into a *.hl file
    void Save(String video)
    {
        //Show the user it started the saving process
        StartCoroutine(ShowTextForTime(video + " is saving..."));

        //Create edl file path + name
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

        //Create/Open the edl file
        StreamWriter sw = new StreamWriter(filePath);

        //Check if the highlight list is not empty
        if (mh.GetList().Count > 0)
        {
            //Write for each highlight a line of parameters in the save file
            foreach (ManageHighlights.Highlight g in mh.GetList())
            {
                String str = "";

                //Iterate through all world positions in the world position list of the highlight
                foreach (Vector3 pos in g.getPos())
                {
                    //Add the current world position to the save string
                    str += pos + ";";
                }
                //Delete last semicolon of the list
                str = str.Remove(str.Length - 1);

                //Add a pipe as separation mark
                str += "|";

                //Iterate through all texture positions in the texture position list of the highlight
                foreach (Vector2 texPos in g.getTexPos())
                {
                    //Add the current texture position to the save string
                    str += texPos + ";";
                }
                //Delete last semicolon of the list
                str = str.Remove(str.Length - 1);

                //Add a pipe as separation mark
                str += "|";

                //Iterate through all time positions in the time position list of the highlight
                foreach (TimeSpan time in g.getTime())
                {
                    //Add the current time position to the save string
                    str += time + ";";
                }
                //Delete last semicolon of the list
                str = str.Remove(str.Length - 1);

                //Add a pipe as separation mark
                str += "|" + g.getType();

                //Write the constructed save string to the file
                sw.WriteLine(str);
            }

            //Close the streamwriter
            sw.Close();

            //Show the user it finished the saving process
            StartCoroutine(ShowTextForTime(video + " is saved"));
        }
        else
        {
            //Show the user it finished the saving process
            StartCoroutine(ShowTextForTime("No Highlights are spawned"));
        }
    }

    //Load the previously saved highlights for the active video
    void Load(String video)
    {
        //Show the user it started the loading process
        StartCoroutine(ShowTextForTime(video + " is loading..."));

        //Create edl file path + name
        String filePath = savePath + "/" + video + ".hl";

        //Check if file is already created
        if (!File.Exists(filePath))
        {
            //Instead try to open a edl file
            OpenEDL(video);
        }
        else
        {
            //Read all lines from the found file
            string[] content = File.ReadAllLines(filePath);

            //Iterate through all lines of the file
            foreach (String line in content)
            {
                String shortLine = line;

                //Extract the world position part of the line string
                String posListStr = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Parse the world possition string into a Vector3
                List<Vector3> posList = ParseStringToVector3List(posListStr);

                //Extract the texture position part of the line string
                String texPosListStr = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Extract the texture position part of the line string and parse it into a Vector2
                List<Vector2> texPosList = ParseStringToVector2List(texPosListStr);

                //Extract the time part of the line string
                String timeListStr = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Parse the time string into a TimeSpan
                List<TimeSpan> timeList = ParseStringToTimeSpanList(timeListStr);

                //Extract the type part of the line string
                String type = shortLine;



                //Create the highlight from the string
                mh.AddItem(posList, texPosList, timeList, type);
            }
            Debug.Log("Anzahl erstellter Highlights: " + mh.GetList().Count);
            //Show the user it finished the loading process
            StartCoroutine(ShowTextForTime(video + " is loaded"));
        }
    }

    //Parses a given string to a Vector2
    List<Vector2> ParseStringToVector2List(String str)
    {
        List<Vector2> result = new List<Vector2>();

        //Split the String in its single position items
        String[] texPosList = str.Split(';');

        //Iterate through all found vector items in the list
        foreach (String item in texPosList)
        {
            String texPos = item;

            //Check if the vector string is in brackets
            if (texPos.StartsWith("(") && texPos.EndsWith(")"))
            {
                //Delete both brackets from the end and the start of the string
                texPos = texPos.Substring(1, texPos.Length - 2);
            }

            //Split the position string into the x and the y coordinate
            String[] array = texPos.Split(',');

            //Create the Vector2 from both coordinate strings
            result.Add(new Vector2(float.Parse(array[0]), float.Parse(array[1])));
        }

        return result;
    }

    //Parses a given string to a Vector3
    List<Vector3> ParseStringToVector3List(String str)
    {
        List<Vector3> result = new List<Vector3>();

        //Split the String in its single position items
        String[] posList = str.Split(';');

        //Iterate through all found vector items in the list
        foreach (String item in posList)
        {
            String pos = item;

            //Check if the vector string is in brackets
            if (pos.StartsWith("(") && pos.EndsWith(")"))
            {
                //Delete both brackets from the end and the start of the string
                pos = pos.Substring(1, pos.Length - 2);
            }

            //Split the position string into the x and the y coordinate
            String[] array = pos.Split(',');

            //Create the Vector2 from both coordinate strings
            result.Add(new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2])));
        }

        return result;
    }

    List<TimeSpan> ParseStringToTimeSpanList(String str)
    {
        List<TimeSpan> result = new List<TimeSpan>();

        //Split the String in its single position items
        String[] timeList = str.Split(';');

        //Iterate through all found vector items in the list
        foreach (String item in timeList)
        {
            //Create the TimeSpan from the string
            result.Add(TimeSpan.Parse(item));
        }

        return result;
    }

    //Toggles the visibility and interaction of a given menu
    private void ConfigureMenu(Canvas menu, bool status)
    {
        //Iterate through all direct children of the menu
        foreach (Transform child in menu.transform)
        {
            //Check if the current found child is the DisconnectMenu
            if (child.name != "DisconnectMenu")
            {
                //Enable/Disable the collider on the buttons of the menu
                child.GetComponent<Collider>().enabled = status;
            }
        }

        //Enable/Disable this menu
        menu.enabled = status;
    }

    //Shows text for certain time in world space
    IEnumerator ShowTextForTime(String text)
    {
        String prevText = "";

        //Check if text already is shown
        if (timeShown)
        {
            //Save the currently shown text
            prevText = vrMenu.GetComponent<Text>().text;
        }

        //Start showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = text;

        //Wait for a given time period (Text will be shown for this time period)
        yield return new WaitForSeconds(showTime);

        //Stop showing the input text on the VRMenu
        if (timeShown)
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
        if (text != String.Empty && text.Contains(" | "))
        {
            //Set textShown to true
            timeShown = true;
        }
        else
        {
            //Set textShown to false
            timeShown = false;
        }

        //Start showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = text;
    }
}
