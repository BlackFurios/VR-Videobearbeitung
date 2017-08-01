/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.3 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculus.com/licenses/LICENSE-3.3

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;					//required for Coroutines
using System.Collections.Generic;           //required for Lists
using System.Runtime.InteropServices;		//required for DllImport
using System;								//required for IntPtr
using System.IO;                            //required for File
using UnityEngine.Video;                    //required for VideoPlayer
using UnityEngine.UI;                       //required for Canvas

/************************************************************************************
Usage:

	Place a simple textured quad surface with the correct aspect ratio in your scene.

	Add the MediaPlayer.cs script to the surface object.

	Supply the name of the media file to play:
	This sample assumes the media file is placed in "Assets/StreamingAssets", ie
	"ProjectName/Assets/StreamingAssets/MovieName.mp4".

	On Desktop, Unity MovieTexture functionality is used. Note: the media file
	is loaded at runtime, and therefore expected to be converted to Ogg Theora
	beforehand.

Implementation:

	In the MediaPlayer Awake() call, GetNativeTexturePtr() is called on 
	renderer.material.mainTexture.
	
	When the MediaSurface plugin gets the initialization event on the render thread, 
	it creates a new Android SurfaceTexture and Surface object in preparation 
	for receiving media. 

	When the game wants to start the video playing, it calls the StartVideoPlayerOnTextureId()
	script call, which creates an Android MediaPlayer java object, issues a 
	native plugin call to tell the native code to set up the target texture to
	render the video to and return the Android Surface object to pass to MediaPlayer,
	then sets up the media stream and starts it.
	
	Every frame, the SurfaceTexture object is checked for updates.  If there 
	is one, the target texId is re-created at the correct dimensions and format
	if it is the first frame, then the video image is rendered to it and mipmapped.  
	The following frame, instead of Unity drawing the image that was placed 
	on the surface in the Unity editor, it will draw the current video frame.

************************************************************************************/

public class MediaPlayer : MonoBehaviour
{
    private Canvas              vrMenu;                                 //Instance of the VRMenu object

    private Control             cr;                                     //

    private bool                timeShown = false;                      //Is currently a text shown
    private int                 showTime = 1;                           //How long texts should be shown in seconds

    private List<videoList>     movieList = new List<videoList>();      //List of all available videos with their paths

    private string              movieName = string.Empty;               //Name of the video which is currently played
    private bool                videoPaused = false;                    //Is the video currently paused
    private bool                videoPausedBeforeAppPause = false;      //Is the video currently paused because the app is paused

    private string              mediaFullPath = string.Empty;           //Path of video which is currently played
    private bool                startedVideo = false;                   //Is the video started

    private String              updStr;                                 //Is a valid video returned at VideStart()

    private VideoPlayer         vp;                                     //Instance of the video player script
    private AudioSource         audioEmitter = null;                    //AudioEmitter which plays the video sound

    private Renderer            mediaRenderer = null;                   //Renderer for the played video

    private enum MediaSurfaceEventType
    {
        Initialize = 0,
        Shutdown = 1,
        Update = 2,
        Max_EventType
    };

    public class videoList
    {
        public String movie;
        public String path;

        public videoList(String video, String videoPath)
        {
            movie = video;
            path = videoPath;
        }
    };

    /// <summary>
    /// Initialization of the movie surface
    /// </summary>
    public void Awake()
    {
        //
        cr = GetComponent<Control>();

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

        Debug.Log("MovieSample Awake");

        //Sets the Renderer script
        mediaRenderer = GetComponent<Renderer>();

        //Sets the Video Player script
        vp = GetComponent<VideoPlayer>();

        //Sets the Audio Source script
        audioEmitter = GetComponent<AudioSource>();

        //Check if the renderer material is null or no texture to display the video exists
        if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null)
        {
            Debug.LogError("No material for movie surface");
        }

        //Check if no video is selected
        if (movieName == string.Empty)
        {
            String absPath;
#if (UNITY_ANDROID && !UNITY_EDITOR)
            //Absolute path of the video libary on windows devices
            absPath = "/storage/emulated/0/Movies/";
#else
            //Absolute path of the video libary on windows devices
            absPath = "C:/Users/" + Environment.UserName + "/Videos/";
#endif

            //Get all mp4-files in the specific video folder
            String[] files = Directory.GetFiles(absPath, "*.mp4");

            //Add all detected files to the list of available movies
            foreach (String str in files)
            {
                //Add the currently selected video to the list of all found videos with their absolute paths
                movieList.Add(new videoList(str.Substring(str.LastIndexOf("/") + 1), str));
            }

            Debug.Log("Detected movies to list of available movies added");
        }
        else
        {
            StartVideo();
        }
    }

    /// <summary>
    /// Construct the streaming asset path.
    /// Note: For Android, we need to retrieve the data from the apk.
    /// </summary>
    IEnumerator RetrieveStreamingAsset(string mediaFileName)
    {
        string streamingMediaPath = "file://";

        //Iterate through the list of all found videos
        foreach (videoList vl in movieList)
        {
            //Check for the currently selected video in the list of all found videos
            if (vl.movie.Substring(0, vl.movie.LastIndexOf(".")) == mediaFileName)
            {
                //Make the found path the play path for the player
                streamingMediaPath += vl.path;
                break;
            }
        }

        //Set the path to be the url for the VideoPlayer script
        vp.url = streamingMediaPath;
        yield return vp;

        mediaFullPath = streamingMediaPath;

        Debug.Log("Movie FullPath: " + mediaFullPath);

        // Video must start only after mediaFullPath is filled in
        Debug.Log("MovieSample Start");
        StartCoroutine(DelayedStartVideo());
    }

    /// <summary>
    /// Auto-starts video playback
    /// </summary>
    IEnumerator DelayedStartVideo()
    {
        //DelaystartedVideo 1 frame to allow MediaSurfaceInit from the render thread
        yield return null;

        //Check if the player already started playing
        if (!startedVideo)
        {
            Debug.Log("Mediasurface DelayedStartVideo");

            startedVideo = true;

            //Prepare the VideoPlayer script
            vp.Prepare();

            Debug.Log("Video is playing");
        }
    }

    void Update()
    {
        if (vp != null && !videoPaused)
        {
            if (GetCurrentPos() != TimeSpan.Zero && GetCurrentPos() == GetMovieLength())
            {
                for (int i = 0; i < GetMovieList().Count; i++)
                {
                    if (GetMovieListMovie(i).Substring(0, GetMovieListMovie(i).LastIndexOf(".")) == GetMovieName())
                    {
                        //Make the array a cycle (0 -> 1 -> 2 -> 0)
                        int index = (i + 1) % GetMovieList().Count;

                        if (index < 0)
                        {
                            index += GetMovieList().Count;
                        }

                        SetMovieName(GetMovieListMovie(index).Substring(0, GetMovieListMovie(index).LastIndexOf(".")));

                        Debug.Log("Start next video");
                        break;
                    }
                }

                updStr = StartVideo();

                if (updStr != null)
                {
                    Debug.LogError("No valid video found");
                }
                else
                {
                    cr.Load(updStr);

                    StartCoroutine(ShowTextForTime(updStr));
                }
            }
            vp.Play();

            if (audioEmitter != null)
            {
                audioEmitter.Play();
            }
        }
    }

    //Starts the player with a given video
    public String StartVideo()
    {
        Debug.Log("Start video!!!");

        //Unpauses the media player before starting the video
        SetPaused(false);

        //Starts the video
        StartCoroutine(RetrieveStreamingAsset(movieName));
        return movieName;
    }

    //Returns to length of the currently active video
    public TimeSpan GetMovieLength()
    {
        //Check if the video player is already loaded
        if (vp != null && GetMovieName() != String.Empty)
        {
            return TimeSpan.FromSeconds(vp.frameCount / vp.frameRate);
        }
        else
        {
            return TimeSpan.Zero;
        }
    }

    //Returns the current time position in the currently active video (Android -> Milliseconds | Windows -> Seconds)
    public TimeSpan GetCurrentPos()
    {
        return TimeSpan.FromSeconds(vp.time);
    }

    //Jumps to a specific time position in the currently active video
    public void JumpToPos(int pos)
    {
        //Check if the VideoPlayer is null
        if (vp != null)
        {
            //Jump to the given time position in the VideoPlayer
            vp.time = pos;
        }
    }

    //Sets the currently active video
    public void SetMovieName(String val)
    {
        movieName = val;
    }

    //Returns the currently active video
    public String GetMovieName()
    {
        return movieName;
    }

    //Returns the list of all found videos
    public List<videoList> GetMovieList()
    {
        return movieList;
    }

    //Returns a certain video from the list of all found videos
    public String GetMovieListMovie(int index)
    {
        return movieList[index].movie;
    }

    //Sets the playback speed of the currently played video
    public void SetPlaybackSpeed(int mode)
    {
        //Check if the VideoPlayer is null
        if (vp != null)
        {
            //Set video playback to normal (mode 0)
            if (mode == 0)
            {
                vp.playbackSpeed = 1;
            }

            //Set video playback to fast (mode 1)
            if (mode == 1)
            {
                vp.playbackSpeed = 2;
            }

            //Set video playback to slow (mode 2)
            if (mode == 2)
            {
                vp.playbackSpeed = 0.5f;
            }
        }
    }

    //Rewind the the currently active video to its beginning
    public String Rewind()
    {
        //Check if the VideoPlayer is null
        if (vp != null)
        {
            //Stops the VideoPlayer
            vp.Stop();

            //Check if the audio source script is null
            if (audioEmitter != null)
            {
                //Stops the audio source script
                audioEmitter.Stop();
            }
            return "Rewinded video";
        }
        return "No player active";
    }

    //Toggles the pause state of the currently active video
    public void SetPaused(bool wasPaused)
    {
        Debug.Log("SetPaused: " + wasPaused);

        //Check if the VideoPlayer is null
        if (vp != null)
        {
            //Set the pausing variable to the new state
            videoPaused = wasPaused;

            //Check if the video player is already paused
            if (videoPaused)
            {
                //Pauses the VideoPlayer
                vp.Pause();

                //Check if the audio source script is null
                if (audioEmitter != null)
                {
                    //Pauses the audio source script
                    audioEmitter.Pause();
                }
            }
            else
            {
                vp.Play();
                if (audioEmitter != null)
                {
                    audioEmitter.Play();
                }
            }
        }
    }

    /// <summary>
    /// Pauses video playback when the app loses or gains focus
    /// </summary>
    void OnApplicationPause(bool appWasPaused)
    {
        Debug.Log("OnApplicationPause: " + appWasPaused);

        //Check if the application lost focus in the platform
        if (appWasPaused)
        {
            videoPausedBeforeAppPause = videoPaused;
        }

        // Pause/unpause the video only if it had been playing prior to app pause
        if (!videoPausedBeforeAppPause)
        {
            SetPaused(appWasPaused);
        }
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
}
