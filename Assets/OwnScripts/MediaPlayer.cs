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
using System.Collections;					// required for Coroutines
using System.Collections.Generic;           // required for Lists
using System.Runtime.InteropServices;		// required for DllImport
using System;								// required for IntPtr
using System.IO;                            // required for File
using UnityEngine.Video;

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
    private List<videoList>     movieList = new List<videoList>();      //List of all available videos with their paths

    private string              movieName = string.Empty;               //Name of the video which is currently played
    private bool                videoPaused = false;                    //Is the video currently paused
    private bool                videoPausedBeforeAppPause = false;      //Is the video currently paused because the app is paused

    private string              mediaFullPath = string.Empty;           //Path of video which is currently played
    private bool                startedVideo = false;                   //Is the video started

#if (UNITY_ANDROID && !UNITY_EDITOR)
	private Texture2D           nativeTexture = null;                   //Instance of texture
	private IntPtr	            nativeTexId = IntPtr.Zero;              //Pointer for TextureID
	private int		            textureWidth = 2880;                    //Hardcoded width of video player
	private int 	            textureHeight = 1440;                   //Hardcoded height of video player
	private AndroidJavaObject   mediaPlayer = null;                     //Instance of AndroidMediaPlayer
    private AndroidJavaObject   playerParams = null;                    //Instance of AndroidPlaybackParams
#else
    private VideoPlayer         vp;                                     //Instance of the video player script
    private AudioSource         audioEmitter = null;                    //AudioEmitter which plays the video sound
#endif
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
    /// The start of the numeric range used by event IDs.
    /// </summary>
    /// <description>
    /// If multiple native rundering plugins are in use, the Oculus Media Surface plugin's event IDs
    /// can be re-mapped to avoid conflicts.
    /// 
    /// Set this value so that it is higher than the highest event ID number used by your plugin.
    /// Oculus Media Surface plugin event IDs start at eventBase and end at eventBase plus the highest
    /// value in MediaSurfaceEventType.
    /// </description>
    public static int eventBase
    {
        get { return _eventBase; }
        set
        {
            _eventBase = value;
#if (UNITY_ANDROID && !UNITY_EDITOR)
			OVR_Media_Surface_SetEventBase(_eventBase);
#endif
        }
    }
    private static int _eventBase = 0;

    private static void IssuePluginEvent(MediaSurfaceEventType eventType)
    {
        GL.IssuePluginEvent((int)eventType + eventBase);
    }

    /// <summary>
    /// Initialization of the movie surface
    /// </summary>
    public void Awake()
    {
        Debug.Log("MovieSample Awake");

#if UNITY_ANDROID && !UNITY_EDITOR
		OVR_Media_Surface_Init();
#endif

        mediaRenderer = GetComponent<Renderer>();
#if !UNITY_ANDROID || UNITY_EDITOR
        vp = GetComponent<VideoPlayer>();

        audioEmitter = GetComponent<AudioSource>();
#endif

        if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null)
        {
            Debug.LogError("No material for movie surface");
        }

        if (movieName == string.Empty)
        {
            String absPath;

#if UNITY_ANDROID && !UNITY_EDITOR
            //Absolute path of the video libary on android devices
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
                movieList.Add(new videoList(str.Substring(str.LastIndexOf("/") + 1), str));
            }

            Debug.Log("Detected movies to list of available movies added");
        }
        else
        {
            StartVideo();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
		nativeTexture = Texture2D.CreateExternalTexture(textureWidth, textureHeight,
		                                                TextureFormat.RGBA32, true, false,
		                                                IntPtr.Zero);

		IssuePluginEvent(MediaSurfaceEventType.Initialize);
#endif
    }

    /// <summary>
    /// Construct the streaming asset path.
    /// Note: For Android, we need to retrieve the data from the apk.
    /// </summary>
    IEnumerator RetrieveStreamingAsset(string mediaFileName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
		string streamingMediaPath = "file://";
        string persistentPath = "";
         foreach (videoList vl in movieList)
        {
            if(vl.movie.Substring(0, vl.movie.LastIndexOf(".")) == mediaFileName)
            {
                streamingMediaPath = vl.path;
                persistentPath = vl.path;
                break;
            }
        }
        
		if (!File.Exists(persistentPath))
		{
			WWW wwwReader = new WWW(streamingMediaPath);
			yield return wwwReader;

			if (wwwReader.error != null)
			{
				Debug.LogError("wwwReader error: " + wwwReader.error);
			}

			System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
		}
		mediaFullPath = persistentPath;
#else
        string streamingMediaPath = "file://";
        foreach (videoList vl in movieList)
        {
            if (vl.movie.Substring(0, vl.movie.LastIndexOf(".")) == mediaFileName)
            {
                streamingMediaPath += vl.path;
                break;
            }
        }

        vp.url = streamingMediaPath;
        yield return vp;

        mediaFullPath = streamingMediaPath;
#endif
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
        yield return null; // delaystartedVideo 1 frame to allow MediaSurfaceInit from the render thread.

        if (!startedVideo)
        {
            Debug.Log("Mediasurface DelayedStartVideo");

            startedVideo = true;
#if (UNITY_ANDROID && !UNITY_EDITOR)
			mediaPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, mediaFullPath);
			mediaRenderer.material.mainTexture = nativeTexture;

            playerParams = CreateMediaPlayerParams();
#else
            vp.Prepare();

            Debug.Log("Video is playing");
#endif
        }
#if (UNITY_ANDROID && !UNITY_EDITOR)
        try
		{
            mediaPlayer.Call("reset");
			mediaPlayer.Call("setDataSource", mediaFullPath);
			mediaPlayer.Call("prepare");
			mediaPlayer.Call("setLooping", true);
			mediaPlayer.Call("start");
		}
		catch (Exception e)
		{
			Debug.Log("Failed to start mediaPlayer with message " + e.Message);
		}
#else

#endif
    }

    void Update()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        //Check if video player is there
        if (mediaPlayer != null) 
        {
            //Check if the video player is playing and is at the end of the video
            if (!videoPaused && GetCurrentPos() == GetMovieLength())
            {
                //Iterate through all videos
                for (int i = 0; i < GetMovieList().Count; i++)
                {
                    //Check for the current video
                    if (GetMovieListMovie(i).Substring(0, GetMovieListMovie(i).LastIndexOf(".")) == GetMovieName())
                    {
                        //Make the array a cycle (0 -> 1 -> 2 -> 0)
                        int index = (i + 1) % GetMovieList().Count;
                        if (index < 0)
                        {
                            index += GetMovieList().Count;
                        }

                        //Set the next videoas current video
                        SetMovieName(GetMovieListMovie(index).Substring(0, GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the video player with the new video
                StartVideo();
            }

            IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
            if (currTexId != nativeTexId)
            {
                nativeTexId = currTexId;
                nativeTexture.UpdateExternalTexture(currTexId);
            }

            IssuePluginEvent(MediaSurfaceEventType.Update);
        }
#else
        //Check if video player is there
        if (vp != null)
        {
            //Check if the video player is playing and is at the end of the video
            if (vp.isPlaying && GetCurrentPos() == GetMovieLength())
            {
                //Iterate through all videos
                for (int i = 0; i < GetMovieList().Count; i++)
                {
                    //Check for the current video
                    if (GetMovieListMovie(i).Substring(0, GetMovieListMovie(i).LastIndexOf(".")) == GetMovieName())
                    {
                        //Make the array a cycle (0 -> 1 -> 2 -> 0)
                        int index = (i + 1) % GetMovieList().Count;
                        if (index < 0)
                        {
                            index += GetMovieList().Count;
                        }

                        //Set the next videoas current video
                        SetMovieName(GetMovieListMovie(index).Substring(0, GetMovieListMovie(index).LastIndexOf(".")));
                        break;
                    }
                }
                //Start the video player with the new video
                StartVideo();
            }

            vp.Play();
            if (audioEmitter != null)
            {
                audioEmitter.Play();
            }
        }
#endif
    }

    public String StartVideo()
    {
        //Unpauses the media player before starting the video
        SetPaused(false);

        //Starts the video
        StartCoroutine(RetrieveStreamingAsset(movieName));
        return movieName;
    }

    public TimeSpan GetMovieLength()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        return TimeSpan.FromMilliseconds(mediaPlayer.Call<int>("getDuration"));
#else
        return TimeSpan.FromSeconds(vp.frameCount / vp.frameRate);
#endif
    }

    public TimeSpan GetCurrentPos()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        return TimeSpan.FromMilliseconds(mediaPlayer.Call<int>("getCurrentPosition"));
#elif (UNITY_STANDALONE_WIN || UNITY_EDITOR)
        return TimeSpan.FromSeconds(vp.time);
#endif
    }

    public void JumpToPos(int pos)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        if (mediaPlayer != null)
        {
            try
			{
				mediaPlayer.Call("seekTo", pos);
			}
			catch (Exception e)
			{
				Debug.Log("Failed to stop mediaPlayer with message " + e.Message);
			}
        }
#else
        if (vp != null)
        {
            vp.time = pos;
        }
#endif
    }

    public void SetMovieName(String val)
    {
        movieName = val;
    }

    public String GetMovieName()
    {
        return movieName;
    }

    public List<videoList> GetMovieList()
    {
        return movieList;
    }

    public String GetMovieListMovie(int index)
    {
        return movieList[index].movie;
    }

    public void SetPlaybackSpeed(int mode)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        if (mediaPlayer != null && playerParams != null)
        {
            //Set video playback to normal (mode 0)
            if (mode == 0)
            {
                playerParams.Call("setSpeed", 1);
                mediaPlayer.Call("setPlaybackParams", playerParams);
            }

            //Set video playback to fast (mode 1)
            if (mode == 1)
            {
                playerParams.Call("setSpeed", 2);
                mediaPlayer.Call("setPlaybackParams", playerParams);
            }

            //Set video playback to slow (mode 2)
            if (mode == 2)
            {
                playerParams.Call("setSpeed", 0.5f);
                mediaPlayer.Call("setPlaybackParams", playerParams);
            }
        }
#else
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
#endif
    }

    public void Rewind()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        if (mediaPlayer != null)
        {
            try
			{
				mediaPlayer.Call("seekTo", 0);
			}
			catch (Exception e)
			{
				Debug.Log("Failed to stop mediaPlayer with message " + e.Message);
			}
        }
#else
        if (vp != null)
        {
            vp.Stop();
            if (audioEmitter != null)
            {
                audioEmitter.Stop();
            }
        }
#endif
    }

    public void SetPaused(bool wasPaused)
    {
        Debug.Log("SetPaused: " + wasPaused);
#if (UNITY_ANDROID && !UNITY_EDITOR)
		if (mediaPlayer != null)
		{
			videoPaused = wasPaused;
			try
			{
				mediaPlayer.Call((videoPaused) ? "pause" : "start");
			}
			catch (Exception e)
			{
				Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
			}
		}
#else
        if (vp != null)
        {
            videoPaused = wasPaused;
            if (videoPaused)
            {
                vp.Pause();

                if (audioEmitter != null)
                {
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
#endif
    }

    /// <summary>
    /// Pauses video playback when the app loses or gains focus
    /// </summary>
    void OnApplicationPause(bool appWasPaused)
    {
        Debug.Log("OnApplicationPause: " + appWasPaused);
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

    private void OnDestroy()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("Shutting down video");
		// This will trigger the shutdown on the render thread
		IssuePluginEvent(MediaSurfaceEventType.Shutdown);
        mediaPlayer.Call("stop");
        mediaPlayer.Call("release");
        mediaPlayer = null;
#endif
    }

#if (UNITY_ANDROID && !UNITY_EDITOR)
	/// <summary>
	/// Set up the video player with the movie surface texture id.
	/// </summary>
	AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath)
	{
		Debug.Log("MoviePlayer: StartVideoPlayerOnTextureId");

		OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);

		IntPtr androidSurface = OVR_Media_Surface_GetObject();
		AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");

		// Can't use AndroidJavaObject.Call() with a jobject, must use low level interface
		//mediaPlayer.Call("setSurface", androidSurface);
		IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(),"setSurface","(Landroid/view/Surface;)V");
		jvalue[] parms = new jvalue[1];
		parms[0] = new jvalue();
		parms[0].l = androidSurface;
		AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parms);

		try
		{
			mediaPlayer.Call("setDataSource", mediaPath);
			mediaPlayer.Call("prepare");
			mediaPlayer.Call("setLooping", false);
			mediaPlayer.Call("start");
		}
		catch (Exception e)
		{
			Debug.Log("Failed to start mediaPlayer with message " + e.Message);
		}

		return mediaPlayer;
	}

    AndroidJavaObject CreateMediaPlayerParams()
    {
        Debug.Log("PlayerParams: CreateMediaPlayerParams");
        
        if (mediaPlayer.Call<AndroidJavaObject>("getPlaybackParams") != null)
        {
            AndroidJavaObject playerParams = mediaPlayer.Call<AndroidJavaObject>("getPlaybackParams");
        }
        else
        {
            AndroidJavaObject playerParams = new AndroidJavaObject("android/media/PlaybackParams");

            playerParams.Call("setAudioFallbackMode", 0);
            playerParams.Call("setPitch", 1);
            playerParams.Call("setSpeed", 1);
        }

        return playerParams;
    }
#endif

#if (UNITY_ANDROID && !UNITY_EDITOR)
	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_Init();

	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_SetEventBase(int eventBase);

	// This function returns an Android Surface object that is
	// bound to a SurfaceTexture object on an independent OpenGL texture id.
	// Each frame, before the TimeWarp processing, the SurfaceTexture is checked
	// for updates, and if one is present, the contents of the SurfaceTexture
	// will be copied over to the provided surfaceTexId and mipmaps will be 
	// generated so normal Unity rendering can use it.
	[DllImport("OculusMediaSurface")]
	private static extern IntPtr OVR_Media_Surface_GetObject();

	[DllImport("OculusMediaSurface")]
	private static extern IntPtr OVR_Media_Surface_GetNativeTexture();

	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_SetTextureParms(int texWidth, int texHeight);
#endif
}
