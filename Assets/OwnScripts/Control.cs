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
    private EDLConverter ec;                                    //Instance of the EDLConverter script

    private Dropdown list;                                      //Instance of the dropdown list object

    private Canvas vrMenu;                                      //Instance of the VRMenu object
    private Canvas stMenu;                                      //Instance of the StartMenu object

    private RaycastHit hit;                                     //Point where the raycast hits
    private int layerMask = 1 << 8;                             //LayerMask with layer of the highlights

    private GameObject conObject;                               //Highlight which the user is connecting
    private GameObject disObject;                               //Highlight which the user is disconnecting
    private GameObject modObject;                               //Highlight which the user is modifying
    private int selectedIndex;                                  //Dropdown index which the user has selected

    private List<String> videoList = new List<string>();        //List of currently possible movies
    
    private bool opened = false;                                //Is the drodown list opened
    private bool pausing = false;                               //Is the video currently paused
    private int showTime = 1;                                   //How long texts should be shown in seconds

    private int hlRange = 5;                                    //From when to when will a single highlight be calculated in the edl (radius)

    private String savePath;                                    //Absolute path of the save files
    private String edlPath;                                     //Absolute path of the edl file

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
        savePath = @"/storage/emulated/0/Movies/VR-Videoschnitt/Saves";
        edlPath = @"/storage/emulated/0/Movies";
#else
        //The save path on a windows system
        savePath = @"C:/Users/" + Environment.UserName + "/Documents/VR-Videoschnitt/Saves";
        edlPath = @"C:/Users/" + Environment.UserName + "/Videos";
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
        //Creates the EDL file for the videos (Currently only the active video)
        if (Input.GetButton("A-Android"))
        {
            CreateEDL(mp.GetMovieName());
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
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Delete selected highlight
                StartCoroutine(ShowText(mh.DeleteItem(hit.transform.gameObject)));
            }
        }

        //Check if the Y-Button is pressed
        if (Input.GetButton("Y-Android"))
        {
            //Check if raycast hitss a highlight
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                String info = "";

                //Add video name, time, texture position and type of the highlight to the info string
                info += "Video:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getVideo();
                info += "\nTime:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getTime().ToString();
                info += "\nTexPos:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getTexPos().ToString();
                info += "\nType:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getType();

                //Check if selected highlight is connected to a previous highlight
                if (hit.transform.gameObject.GetComponent<HighlightMemory>().getPrev() != null)
                {
                    //Add "Yes" to the info string
                    info += "\nPrev:\tYes";
                }
                else
                {
                    //Add "No" to the info string
                    info += "\nPrev:\tNo";
                }

                //Check if selected highlight is connected to a next highlight
                if (hit.transform.gameObject.GetComponent<HighlightMemory>().getNext() != null)
                {
                    //Add "Yes" to the info string
                    info += "\nNext:\tYes";
                }
                else
                {
                    //Add "No" to the info string
                    info += "\nNext:\tNo";
                }

                //Display info string on VRMenu in world space
                StartCoroutine(ShowText(info));
            }
            else
            {
                StartCoroutine(ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString()));
            }
        }

        //Check if the R1-Button is pressed
        if (Input.GetButtonDown("R1-Android"))
        {
            disObject = null;
            //Check if raycast hits something
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check if a highlight is already selected to connect and the raycast hits a highlight
                if (conObject != null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Connect the two highlights if possible
                    mh.ConnectItems(conObject, hit.transform.gameObject);

                    //Reset the selected highlight to null
                    conObject = null;
                }
                //Check if a highlight is already selected to connect and the raycast hits no highlight
                else if (conObject != null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    //Calculate the hit position for a the new highlight
                    Vector3 hitPos = hit.point - Camera.main.transform.position;
                    hitPos.x = hitPos.x * 0.9f;
                    hitPos.y = hitPos.y * 0.9f;
                    hitPos.z = hitPos.z * 0.9f;

                    //Get the correct texture coordinates on the video texture
                    Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                    Vector2 coords = hit.textureCoord;
                    coords.x *= tex.width;
                    coords.y *= tex.height;

                    //Draw the connection from the selected highlight to the new highlight
                    mh.DrawLine(conObject, hitPos);

                    //Create the new highlight on the hit position
                    StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords, mp.GetMovieName())));

                    //Iterate through the list of all highlights
                    foreach (GameObject g in mh.GetList())
                    {
                        //Check for newly created highlight in list of all highlights
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && 
                            g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
                        {
                            //Connect the newly created  highlgight to the selected highlight
                            mh.ConnectItems(conObject, g);
                            break;
                        }
                    }

                    //Reset the selected highlight to null
                    conObject = null;
                }
                //Check if no highlight is already selected and the raycast hits a highlight
                else if (conObject == null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Select the currently hit highlight
                    conObject = hit.transform.gameObject;
                }
            }
        }

        //Check if a highlight to connect is selected
        if (conObject != null && 
            Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Calculate the currently focused position
            Vector3 hitPos = hit.point - Camera.main.transform.position;
            hitPos.x = hitPos.x * 0.9f;
            hitPos.y = hitPos.y * 0.9f;
            hitPos.z = hitPos.z * 0.9f;

            //Draw connection line from selected highlight to the currently focused position
            mh.DrawLine(conObject, hitPos);
        }

        //Check if the L1-Button is pressed
        if (Input.GetButton("L1-Android"))
        {
            conObject = null;
            //Check if the raycast hits a highlight
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Check if a highlight is already selected
                if (disObject != null)
                {
                    //Disconnect the selected highlight from the hit highlight if possible
                    mh.DisconnectItems(disObject);

                    //Reset the selected highlight to null
                    disObject = null;
                }
                //Check if no highlight is already selected
                else if (disObject == null)
                {
                    //Select the currently hit highlight
                    disObject = hit.transform.gameObject;
                }
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButtonDown("R2-Android"))
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
                        StartCoroutine(ShowText(mp.StartVideo()));
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

            //Check if raycast hits the media sphere
            if (stMenu.enabled == false && opened == false && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && 
                            g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
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

        //Check if a highlight to modify is selected
        if (modObject != null && 
            Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Translate highlight while it is selected
            modObject.GetComponent<HighlightMemory>().setTime(mp.GetCurrentPos());
            mh.MoveItem(modObject, hit.point);
        }

        //Check if the R2-Button is not pressed anymore
        if (Input.GetButtonUp("R2-Android"))
        {
            //Check if a highlight to modify is selected
            if (modObject != null && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Modify all necessary parameters of the selected highlight
                mh.ModifyItem(modObject, hit.point, mp.GetCurrentPos(), modObject.GetComponent<HighlightMemory>().getType(), coords);

                //Deselect highlight
                modObject = null;
            }
        }

        //Check if the L"-Button is pressed
        if (Input.GetButton("L2-Android"))
        {
            //Save the current state of all highlights for the selected video
            Save(mp.GetMovieName());
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Android") == 1)
        {
            //Check if the currently playing video is at its end
            if ((mp.GetMovieLength().TotalMilliseconds - mp.GetCurrentPos().TotalMilliseconds) < 5000)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i + 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                //Jump to the end of the video
                mp.JumpToPos((int)mp.GetMovieLength().TotalMilliseconds - 3000);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Android") == -1)
        {
            //Check if the currently playing video is at its beginning
            if (mp.GetCurrentPos().TotalMilliseconds < 5000)
            {
                //Iterate through the list of all videos
                for (int i = 0; i < mp.GetMovieList().Count; i++)
                {
                    //Check for the currently played video in the list of all videos
                    if (mp.GetMovieListMovie(i).Substring(0, mp.GetMovieListMovie(i).LastIndexOf(".")) == mp.GetMovieName())
                    {
                        //Make the search circular
                        int index = (i - 1) % mp.GetMovieList().Count;
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                //Rewind to the start of the video
                StartCoroutine(ShowText(mp.Rewind()));
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") > 0)
        {
            //Set the playback speed to double
            mp.SetPlaybackSpeed(1);
        
            StartCoroutine(ShowText("Faster"));
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Android") < 0)
        {
            //Set the playback speed to half
            mp.SetPlaybackSpeed(2);
        
            StartCoroutine(ShowText("Slower"));
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Android") == 0)
        {
            //Set the playback speed to normal
            mp.SetPlaybackSpeed(0);
        }
#else
        //Creates the EDL file for the videos (Currently only the active video)
        if (Input.GetButton("A-Windows"))
        {
            CreateEDL(mp.GetMovieName());
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
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Delete selected highlight
                StartCoroutine(ShowText(mh.DeleteItem(hit.transform.gameObject)));
            }
        }

        //Check if the Y-Button is pressed
        if (Input.GetButton("Y-Windows"))
        {
            //Check if raycast hitss a highlight
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                String info = "";

                //Add video name, time, texture position and type of the highlight to the info string
                info += "Video:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getVideo();
                info += "\nTime:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getTime().ToString();
                info += "\nTexPos:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getTexPos().ToString();
                info += "\nType:\t" + hit.transform.gameObject.GetComponent<HighlightMemory>().getType();

                //Check if selected highlight is connected to a previous highlight
                if (hit.transform.gameObject.GetComponent<HighlightMemory>().getPrev() != null)
                {
                    //Add "Yes" to the info string
                    info += "\nPrev:\tYes";
                }
                else
                {
                    //Add "No" to the info string
                    info += "\nPrev:\tNo";
                }

                //Check if selected highlight is connected to a next highlight
                if (hit.transform.gameObject.GetComponent<HighlightMemory>().getNext() != null)
                {
                    //Add "Yes" to the info string
                    info += "\nNext:\tYes";
                }
                else
                {
                    //Add "No" to the info string
                    info += "\nNext:\tNo";
                }

                //Display info string on VRMenu in world space
                StartCoroutine(ShowText(info));
            }
            else
            {
                StartCoroutine(ShowText(mp.GetCurrentPos().ToString() + " | " + mp.GetMovieLength().ToString()));
            }
        }

        //Check if the R1-Button is pressed
        if (Input.GetButtonDown("R1-Windows"))
        {
            disObject = null;
            //Check if raycast hits something
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
            {
                //Check if a highlight is already selected to connect and the raycast hits a highlight
                if (conObject != null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Connect the two highlights if possible
                    mh.ConnectItems(conObject, hit.transform.gameObject);

                    //Reset the selected highlight to null
                    conObject = null;
                }
                //Check if a highlight is already selected to connect and the raycast hits no highlight
                else if (conObject != null && hit.transform.gameObject.name != "Highlight(Clone)")
                {
                    //Calculate the hit position for a the new highlight
                    Vector3 hitPos = hit.point - Camera.main.transform.position;
                    hitPos.x = hitPos.x * 0.9f;
                    hitPos.y = hitPos.y * 0.9f;
                    hitPos.z = hitPos.z * 0.9f;

                    //Get the correct texture coordinates on the video texture
                    Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                    Vector2 coords = hit.textureCoord;
                    coords.x *= tex.width;
                    coords.y *= tex.height;

                    //Draw the connection from the selected highlight to the new highlight
                    mh.DrawLine(conObject, hitPos);

                    //Create the new highlight on the hit position
                    StartCoroutine(ShowText(mh.AddItem(hit.point, mp.GetCurrentPos(), "Single", coords, mp.GetMovieName())));

                    //Iterate through the list of all highlights
                    foreach (GameObject g in mh.GetList())
                    {
                        //Check for newly created highlight in list of all highlights
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && 
                            g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
                        {
                            //Connect the newly created  highlgight to the selected highlight
                            mh.ConnectItems(conObject, g);
                            break;
                        }
                    }

                    //Reset the selected highlight to null
                    conObject = null;
                }
                //Check if no highlight is already selected and the raycast hits a highlight
                else if (conObject == null && hit.transform.gameObject.name == "Highlight(Clone)")
                {
                    //Select the currently hit highlight
                    conObject = hit.transform.gameObject;
                }
            }
        }

        //Check if a highlight to connect is selected
        if (conObject != null && 
            Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Calculate the currently focused position
            Vector3 hitPos = hit.point - Camera.main.transform.position;
            hitPos.x = hitPos.x * 0.9f;
            hitPos.y = hitPos.y * 0.9f;
            hitPos.z = hitPos.z * 0.9f;

            //Draw connection line from selected highlight to the currently focused position
            mh.DrawLine(conObject, hitPos);
        }

        //Check if the L1-Button is pressed
        if (Input.GetButton("L1-Windows"))
        {
            conObject = null;
            //Check if the raycast hits a highlight
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit) && 
                hit.transform.gameObject.name == "Highlight(Clone)")
            {
                //Check if a highlight is already selected
                if (disObject != null)
                {
                    //Disconnect the selected highlight from the hit highlight if possible
                    mh.DisconnectItems(disObject);

                    //Reset the selected highlight to null
                    disObject = null;
                }
                //Check if no highlight is already selected
                else if (disObject == null)
                {
                    //Select the currently hit highlight
                    disObject = hit.transform.gameObject;
                }
            }
        }

        //Check if the R2-Button is pressed
        if (Input.GetButtonDown("R2-Windows"))
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
                        StartCoroutine(ShowText(mp.StartVideo()));
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

            //Check if raycast hits the media sphere
            if (stMenu.enabled == false && opened == false && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
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
                        if (g.GetComponent<HighlightMemory>().getTexPos() == coords && 
                            g.GetComponent<HighlightMemory>().getTime() == mp.GetCurrentPos())
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

        //Check if a highlight to modify is selected
        if (modObject != null && 
            Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            //Translate highlight while it is selected
            modObject.GetComponent<HighlightMemory>().setTime(mp.GetCurrentPos());
            mh.MoveItem(modObject, hit.point);
        }

        //Check if the R2-Button is not pressed anymore
        if (Input.GetButtonUp("R2-Windows"))
        {
            //Check if a highlight to modify is selected
            if (modObject != null && 
                Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                //Get the correct texture coordinates on the video texture
                Texture tex = hit.transform.gameObject.GetComponent<Renderer>().material.mainTexture;
                Vector2 coords = hit.textureCoord;
                coords.x *= tex.width;
                coords.y *= tex.height;

                //Modify all necessary parameters of the selected highlight
                mh.ModifyItem(modObject, hit.point, mp.GetCurrentPos(), modObject.GetComponent<HighlightMemory>().getType(), coords);

                //Deselect highlight
                modObject = null;
            }
        }

        //Check if the L"-Button is pressed
        if (Input.GetButton("L2-Windows"))
        {
            //Save the current state of all highlights for the selected video
            Save(mp.GetMovieName());
        }

        //Check if the up DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Windows") == 1)
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
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                //Jump to the end of the video
                mp.JumpToPos((int)mp.GetMovieLength().TotalSeconds - 3);
            }
        }

        //Check if the down DPad-Button is pressed
        if (Input.GetAxisRaw("DPad-Vertical-Windows") == -1)
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
                        if (index < 0)
                        {
                            index += mp.GetMovieList().Count;
                        }

                        //Select the new selected video as ne active video
                        mp.SetMovieName(mp.GetMovieListMovie(index).Substring(0, mp.GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the new video
                StartCoroutine(ShowText(mp.StartVideo()));
            }
            else
            {
                //Rewind to the start of the video
                StartCoroutine(ShowText(mp.Rewind()));
            }
        }

        //Check if the right DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") > 0)
        {
            //Set the playback speed to double
            mp.SetPlaybackSpeed(1);

            StartCoroutine(ShowText("Faster"));
        }

        //Check if the left DPad-Button is pressed
        if (Input.GetAxis("DPad-Horizontal-Windows") < 0)
        {
            //Set the playback speed to half
            mp.SetPlaybackSpeed(2);

            StartCoroutine(ShowText("Slower"));
        }

        //Check if the vertical DPad-Buttons are not pressed anymore
        if (Input.GetAxis("DPad-Horizontal-Windows") == 0)
        {
            //Set the playback speed to normal
            mp.SetPlaybackSpeed(0);
        }
#endif
    }

    void CreateEDL(String video)
    {
        //Show the user it started the converting process
        StartCoroutine(ShowText("EDL for " + video + " is creating..."));

        //Create edl file path + name
        String filePath = edlPath + "/" + video + ".edl";

        //Check if directory is not created
        if (!Directory.Exists(filePath))
        {
            //Create the directory
            Directory.CreateDirectory(filePath);
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

        int mode = 0;   //0 -> Audio/Video    | 1 -> Video    | 2 -> Audio

        TimeSpan prevTime = TimeSpan.Zero;

        //List of all already in the edit decision list used highlights
        List<GameObject> usedHl = new List<GameObject>();

        //Variables of the current highlight and the last highlight of the possible chain
        GameObject highlight = null;
        GameObject last = null;

        //Variables of the beginning and the end of the highlight in the source (the video)
        TimeSpan srcIN;
        TimeSpan srcOUT;

        //Variables of the beginning and the end of the highlight in the record (the edit decision list)
        TimeSpan recIN;
        TimeSpan recOUT;

        //Iterate through all highlights
        for (int lineCnt = 1; lineCnt <= mh.GetList().Count; lineCnt++)
        {
            //Set highlight variable
            highlight = mh.GetItem(lineCnt - 1);

            //Set transition variable
            int trans = GetTransitionNumber(highlight);

            //Check if there are already highlights which are in previous line in the edit decision list
            if (usedHl.Count != 0)
            {
                //Check if highlight has no next highlight
                if (highlight.GetComponent<HighlightMemory>().getNext() == null)
                {
                    //Set the srcIN 5 seconds before first chain highlight and srcOUT 5 seconds after last chain highlight
                    srcIN = highlight.GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(hlRange));
                    srcOUT = highlight.GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(hlRange));

                    //Set the time interval of the single/chain highlight
                    TimeSpan interval = srcOUT.Subtract(srcIN);

                    //Set recIN at the previous time and recOUT at the previous time combined with the interval
                    recIN = prevTime;
                    recOUT = prevTime.Add(interval);

                    //Set the previous time variable to the last time record output
                    prevTime = recOUT;
                }
                //Check if highlight has a next highlight
                else
                {
                    //Set the last variable (recursiv)
                    last = GetLastChainItem(highlight.GetComponent<HighlightMemory>().getNext());

                    //Set the srcIN 5 seconds before first chain highlight and srcOUT 5 seconds after last chain highlight
                    srcIN = highlight.GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(hlRange));
                    srcOUT = last.GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(hlRange));

                    //Set the ignore variable
                    bool ignore = false;

                    //Iterate through all already in the edl creation used highlights
                    foreach (GameObject g in usedHl)
                    {
                        //Check if the last variable is already part of the already used highlights
                        if (last == g)
                        {
                            ignore = true;
                            break;
                        }
                    }

                    //Check if this highlight needs to be ignored
                    if (ignore)
                    {
                        //Set both recIN and recOUT to zero to show that this highlight needs to be ignored
                        recIN = TimeSpan.Zero;
                        recOUT = TimeSpan.Zero;
                    }
                    //Check if this highlight needs to be considered
                    else
                    {
                        //Set the time interval of the single/chain highlight
                        TimeSpan interval = srcOUT.Subtract(srcIN);

                        //Set recIN at the previous time and recOUT at the previous time combined with the interval
                        recIN = prevTime;
                        recOUT = prevTime.Add(interval);

                        //Set the previous time variable to the last time record output
                        prevTime = recOUT;
                    }
                }
            }
            //Check if no highlights are already used
            else
            {
                //Check if highlight has no next highlight
                if (highlight.GetComponent<HighlightMemory>().getNext() == null)
                {
                    //Set the srcIN 5 seconds before first chain highlight and srcOUT 5 seconds after last chain highlight
                    srcIN = highlight.GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(hlRange));
                    srcOUT = highlight.GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(hlRange));

                    //Set the time interval of the single/chain highlight
                    TimeSpan interval = srcOUT.Subtract(srcIN);

                    //Set recIN at the previous time and recOUT at the previous time combined with the interval
                    recIN = prevTime;
                    recOUT = prevTime.Add(interval);

                    //Set the previous time variable to the last time record output
                    prevTime = recOUT;
                }
                //Check if highlight has a next highlight
                else
                {
                    //Set the last variable (recursiv)
                    last = GetLastChainItem(highlight.GetComponent<HighlightMemory>().getNext());

                    //Set srcIN 5 seconds before first chain highlight and srcOUT 5 seconds after last chain highlight
                    srcIN = highlight.GetComponent<HighlightMemory>().getTime().Subtract(TimeSpan.FromSeconds(hlRange));
                    srcOUT = last.GetComponent<HighlightMemory>().getTime().Add(TimeSpan.FromSeconds(hlRange));

                    //Set recIN at the beginning and recOUT at srcOUT
                    recIN = TimeSpan.Zero;
                    recOUT = srcOUT;

                    //Set the previous time variable to the last time record output
                    prevTime = recOUT;
                }
            }

            //Check if both recIN and recOUT are zero (no time interval present)
            if (recIN == TimeSpan.Zero && recOUT == TimeSpan.Zero)
            {
                Debug.Log("This highlight is already used in the edl");
            }
            //Check if there is an time interval
            else
            {
                //Convert the given parameters to a complete edl line
                sw.WriteLine(ec.ConvertEdlLine(lineCnt, mode, trans, srcIN, srcOUT, recIN, recOUT));
                sw.WriteLine("* FROM CLIP NAME: " + video.ToUpper() + ".MP4");
                sw.WriteLine();
            }
        }

        //Close the streamwriter
        sw.Close();

        //Show the user it started the converting process
        StartCoroutine(ShowText("EDL for " + video + " is created"));
    }

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

    GameObject GetLastChainItem (GameObject g)
    {
        GameObject output = null;

        //Check if this highlight has a next highlight
        if (g.GetComponent<HighlightMemory>().getNext() != null)
        {
            //Start GetLastChainItem again with the next highlight (recursive)
            output = GetLastChainItem (g.GetComponent<HighlightMemory>().getNext());
        }
        //Check if this highlight has no next highlight
        else
        {
            //Set this highlight as the output highlight
            output = g;
        }

        return output;
    }

    //Save the current state of all highlights of the currently active video
    void Save(String video)
    {
        //Show the user it started the save process
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

        //Serialize the file and close the filestream
        bf.Serialize(fs, data);
        fs.Close();

        //Show the user it finished the save process
        StartCoroutine(ShowText(video + " is saved"));
    }

    //Load the previously saved highlights for the active video
    void Load(String video)
    {
        //Determine the location and name of the save file
        String filePath = savePath + video + ".hl";

        //Clear old list from highlights
        mh.ClearList();

        //Check if there is already a save file
        if (File.Exists(filePath))
        {
            //Show the user it started the load process
            StartCoroutine(ShowText(video + " is loading..."));

            //Create Formatter and open file path + name
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Open(filePath, FileMode.Open);

            //Deserialize the file and close the filestream
            SaveData data = (SaveData)bf.Deserialize(fs);
            fs.Close();

            //Start the creation of all saved highlights
            CreateHighlights(data, video);

            //Show the user it finished the load process
            StartCoroutine(ShowText(video + " is loaded"));
        }
    }

    //Translates all highlights each to a string from their parameter information
    void CreateIDList(SaveData data)
    {
        localId[] localIdList = new localId[mh.GetList().Count];
        String nextLocalId = "";

        //Iterate through the list of all highlights
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

        //Iterate through the list of all created ids without the next and previous ids
        for (int i = 0; i < localIdList.Length; i++)
        {
            //Check if the highlight has a next highlight
            if (localIdList[i].g.GetComponent<HighlightMemory>().getNext() != null)
            {
                //Iterate through the list of all created ids without the next and previous ids
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

    //Create highlights from a opened save file
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

            //Get the world position parameter as Vector3
            Vector3 pos;
            pos = ParseToVector3(strPos);

            //Spawn the higlight from its parameters
            mh.AddItem(pos, time, strType, texPos, video);

            //Add the newly created highlight to the list of possible next highlights
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

    //Parses a String to a Vector2
    private Vector2 ParseToVector2(String val)
    {
        //Get rid of both brackets
        String str = val.Substring(1, val.Length - 2);

        //Split vector in its both coordinates x and y as strings
        string[] sArray = str.Split(',');

        //Convert both coordinate strings to float and return the vector
        return new Vector2(float.Parse(sArray[0]), float.Parse(sArray[1]));
    }

    //Parses a String to a Vector3
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
    IEnumerator ShowText(String text)
    {
        //Start showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = text;

        //Wait for a given time (Shows text during this period)
        yield return new WaitForSecondsRealtime(showTime);

        //Stop showing the input text on the VRMenu
        vrMenu.GetComponent<Text>().text = "";
    }
}
