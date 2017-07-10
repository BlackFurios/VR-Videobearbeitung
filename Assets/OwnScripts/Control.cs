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
    private ManageHighlights mh;                                //Instance of the ManageHighlights script
    private MediaPlayer mp;                                     //Instance of the MediaPlayer script
    private EDLConverter ec;                                    //Instance of the EDLConverter script

    private Dropdown list;                                      //Instance of the dropdown list object

    private Canvas vrMenu;                                      //Instance of the VRMenu object
    private Canvas stMenu;                                      //Instance of the StartMenu object

    private RaycastHit hit;                                     //Point where the raycast hits
    private int layerMask = 1 << 8;                             //LayerMask with layer of the highlights
    
    private int selectedIndex;                                  //Dropdown index which the user has selected

    private List<String> videoList = new List<string>();        //List of currently possible movies
    
    private bool opened = false;                                //Is the drodown list opened
    private bool pausing = false;                               //Is the video currently paused
    private bool textShown = false;                             //Is currently a text shown
    private int showTime = 1;                                   //How long texts should be shown in seconds

    private float delTimer;                                     //Timer for long button press on X-Button
    private float saveTimer;                                    //Timer for long button press on L2-Button

    private String savePath;                                    //Absolute path of the save files
    private String edlPath;                                     //Absolute path of the edl files

    public class localId                                        //Struct for a highlight and its own local if (without next and prev parameters)
    {
        public GameObject g;                                    //The highlight as gameObject
        public String localID;                                  //The highlights localID

        public localId(GameObject a, String b)                  //Constructor of the localID struct
        {
            g = a;
            localID = b;
        }
    }

    public class nextId                                         //Struct for a highlight and its next highlight as id
    {
        public GameObject g;                                    //The highlight as gameObject
        public String nextLocalID;                              //The highlights next highlight as its localID

        public nextId(GameObject a, String b)                   //Constructor of the nextID struct
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
                //Delete selected highlight
                mh.DeleteItem(hit.transform.gameObject);
            }
            else
            {
                //Clear complete highlight list and delete all highlights at once
                mh.ClearList();
            }  
        }

        //Check if the Y-Button is pressed
        if (Input.GetButtonDown("Y-Android"))
        {
            //Check if text is already shown
            if (!textShown)
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
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name != "Highlight(Clone)")
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;

                //Scale the texture coordinates on the whole picture size
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Starts creation of the new highlight
                mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords);
            }
        }

        //Check if the L1-Button is pressed
        if (Input.GetButtonDown("L1-Android"))
        {
            //
            if (mh.GetList().Count == 0)
            {
                //
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
            // Check if the StartMenu is enabled and the dropdown list is closed
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
                            //
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
            //
            if (mh.GetList().Count == 0)
            {
                //
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

                        //
                        if (index < 0)
                        {
                            //
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

                        //
                        if (index < 0)
                        {
                            //
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
                //Delete selected highlight
                mh.DeleteItem(hit.transform.gameObject);
            }
            else
            {
                //Clear complete highlight list and delete all highlights at once
                mh.ClearList();
            }  
        }

        //Check if the Y-Button is pressed
        if (Input.GetButtonDown("Y-Windows"))
        {
            //Check if text is already shown
            if (!textShown)
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
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && hit.transform.gameObject.name != "Highlight(Clone)")
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;

                //Scale the texture coordinates on the whole picture size
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Starts creation of the new highlight
                mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords);
            }
        }

        //Check if the L1-Button is pressed
        if (Input.GetButtonDown("L1-Windows"))
        {
            //
            if (mh.GetList().Count == 0)
            {
                //
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
            // Check if the StartMenu is enabled and the dropdown list is closed
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
                            //
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
            //
            if (mh.GetList().Count == 0)
            {
                //
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

                        //
                        if (index < 0)
                        {
                            //
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

                        //
                        if (index < 0)
                        {
                            //
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
        if (textShown)
        {
            ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString());
        }
    }

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

        //Create/Open the edl file
        StreamWriter sw = new StreamWriter(filePath);

        //Write edl header
        sw.WriteLine("TITLE: " + video.ToUpper() + "SEQUENCE");
        sw.WriteLine("FCM: NON-DROP FRAME");
        sw.WriteLine();

        //List of all already checked highlights
        List<GameObject> usedHl = new List<GameObject>();

        //The currently checked highlight
        GameObject current;

        //Line count
        int lineCnt = 1;

        //Source start and end point
        TimeSpan srcIN;
        TimeSpan srcOUT;

        //Record start and end point
        TimeSpan recIN = TimeSpan.Zero;
        TimeSpan recOUT = TimeSpan.Zero;

        //Iterate through all spawned highlights
        for (int i = 0; i <= mh.GetList().Count; i++)
        {
            //Set the currently checked item as the current item of the list of highlights
            current = mh.GetItem(i);

            //Check if the currently checked highlight is not used already
            if (!usedHl.Contains(current))
            {
                //Get all highlight that are related to another (Determine a possible chain)
                List<GameObject> chain =  mh.CreateItemChain(current);

                //Cheeck if it is a chain or a single highlight
                if (chain.Count == 1)
                {
                    //Set the source start point and source end point of the clip of the single highlight
                    srcIN = current.GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(2));
                    srcOUT = current.GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(2));
                }
                else
                {
                    //Set the source start point and source end point of the clip of the chain
                    srcIN = chain.First<GameObject>().GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(1));
                    srcOUT = chain.Last<GameObject>().GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(1));
                }

                //Set the end point of the clip
                recOUT += srcOUT.Subtract(srcIN);

                //Add the recently used highlights to the used highlights list
                usedHl.AddRange(chain);

                //Convert the given parameters to a complete edl line
                sw.WriteLine(ec.ConvertEdlLine(lineCnt, 0, 1, srcIN, srcOUT, recIN, recOUT));
                sw.WriteLine("* FROM CLIP NAME: " + video.ToUpper() + ".MP4");
                sw.WriteLine();

                //Set the start point of the new clip at the end of the last clip
                recIN += srcOUT;

                //Increase the line count
                lineCnt += 1;
            }
        }

        //Close the streamwriter
        sw.Close();

        //Show the user it finished the saving process
        StartCoroutine(ShowTextForTime("EDL for " + video + " is created"));
    }

    //Opens an already existing edl file
    void OpenEDL(String video)
    {
        StartCoroutine(ShowTextForTime("Opening EDL not yet implemented"));
    }

    //Returns the transition mode for the selected highlight/chain
    int GetTransitionNumber(GameObject highlight)
    {
        int trans;

        //Define the type of highlight for the edl (3 types possible)
        switch (highlight.GetComponent<HighlightMemory>().getType())
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

        //Write for each highlight a line of parameters in the save file
        foreach (GameObject g in mh.GetList())
        {
            String str = String.Empty;

            str += g.transform.position;
            str += "|" + g.GetComponent<HighlightMemory>().getTime();
            str += "|" + g.GetComponent<HighlightMemory>().getType();
            str += "|" + g.GetComponent<HighlightMemory>().getTexPos();

            sw.WriteLine(str);
        }

        //Close the streamwriter
        sw.Close();

        //Show the user it finished the saving process
        StartCoroutine(ShowTextForTime(video + " is saved"));
    }

    //Load the previously saved highlights for the active video
    void Load(String video)
    {
        //Show the user it started the loading process
        StartCoroutine(ShowTextForTime(video + " is loading..."));

        //Create edl file path + name
        String filePath = savePath + "/" + video + ".hl";

        //Check if directory is not created
        if (!Directory.Exists(savePath))
        {
            //Instead try to open a edl file
            OpenEDL(video);
        }

        //Check if file is already created
        if (!File.Exists(filePath))
        {
            //Instead try to open a edl file
            OpenEDL(video);
        }
        else
        {
            //Read all lines from the found file
            String[] content = File.ReadAllLines(filePath);

            //Iterate through all lines of the file
            foreach (String line in content)
            {
                String shortLine = line;

                //Extract the world position part of the line string
                String posStr = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Parse the world possition string into a Vector3
                Vector3 pos = ParseStringToVector3(posStr);

                //Extract the time part of the line string
                String timeStr = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Parse the time string into a TimeSpan
                TimeSpan time = TimeSpan.Parse(timeStr);

                //Extract the type part of the line string
                String type = shortLine.Substring(0, shortLine.IndexOf("|"));
                shortLine = shortLine.Substring(shortLine.IndexOf("|") + 1);

                //Extract the texture position part of the line string and parse it into a Vector2
                Vector2 texPos = ParseStringToVector2(shortLine);
                
                //Create the highlight from the string
                mh.AddItem(pos, time, type, texPos);
            }

            //Show the user it finished the loading process
            StartCoroutine(ShowTextForTime(video + " is loaded"));
        }
    }

    //Parses a given string to a Vector2
    Vector2 ParseStringToVector2(String str)
    {
        //Check if the vector string is in brackets
        if (str.StartsWith("(") && str.EndsWith(")"))
        {
            //Delete both brackets from the end and the start of the string
            str = str.Substring(1, str.Length-2);
        }

        //Split the position string into the x and the y coordinate
        String[] array = str.Split(',');

        //Create the Vector2 from both coordinate strings
        Vector2 result = new Vector2(float.Parse(array[0]), float.Parse(array[1]));

        return result;
    }

    //Parses a given string to a Vector3
    Vector3 ParseStringToVector3(String str)
    {
        //Check if the vector string is in brackets
        if (str.StartsWith("(") && str.EndsWith(")"))
        {
            //Delete both brackets from the end and the start of the string
            str = str.Substring(1, str.Length-2);
        }

        //Split the position string into the x, y and z coordinate
        String[] array = str.Split(',');

        //Create the Vector§ from all three coordinate strings
        Vector3 result = new Vector3(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));

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
