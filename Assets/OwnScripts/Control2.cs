using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Control2 : MonoBehaviour
{
    private ManageHighlights mh;                                //Instance of the ManageHighlights script
    private MediaPlayer mp;                                     //Instance of the MediaPlayer script

    private Dropdown list;                                      //Instance of the dropdown list object

    private Canvas vrMenu;                                      //Instance of the VRMenu object
    private Canvas hlMenu;                                      //Instance of the HighlightMenu object
    private Canvas stMenu;                                      //Instance of the StartMenu object
    private Canvas dcMenu;                                      //Instance of the DisconnectMenu object

    private RaycastHit hit;                                     //Point where the raycast hits

    private GameObject selectedObject;                          //Highlight which the user has selected
    private int selectedIndex;                                  //Dropdown index which the user has selected

    private List<String> videoList = new List<string>();        //List of currently possible movies

    private bool forwarding = false;                            //Is the video currently forwarding
    private bool reversing = false;                             //Is the video currently reversing
    private bool opened = false;                                //Is the drodown list opened
    private bool pausing = false;                               //Is the video currently paused
    private int showTime = 3;                                   //How long texts should be shown in seconds

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
        savePath = "/storage/emulated/0/Movies/VR-Videoschnitt/Saves/";
#else
        savePath = "C:/Users/" + Environment.UserName + "/Documents/VR-Videoschnitt/Saves/";
#endif

        mh = GetComponent<ManageHighlights>();
        mp = GetComponent<MediaPlayer>();

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

#else
        if (Input.GetButton("A-Windows"))
        {

        }

        if (Input.GetButton("B-Windows"))
        {

        }

        if (Input.GetButton("X-Windows"))
        {

        }

        if (Input.GetButton("Y-Windows"))
        {

        }

        if (Input.GetButton("R1-Windows"))
        {

        }

        if (Input.GetButton("L1-Windows"))
        {

        }

        if (Input.GetButton("R2-Windows"))
        {

        }

        if (Input.GetButton("L2-Windows"))
        {

        }

        if (Input.GetAxis("DPad-Horizontal-Windows") != 0)
        {

        }

        if (Input.GetAxis("DPad-Vertical-Windows") != 0)
        {

        }
#endif
    }

    void Save(String video)
    {
        //Create Formatter and save file path + name
        BinaryFormatter bf = new BinaryFormatter();
        String filePath = savePath + video + ".hl";

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
        String filePath = savePath + video + ".hl";

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
