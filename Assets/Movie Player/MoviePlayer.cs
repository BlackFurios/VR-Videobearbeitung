using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;
using UnityEngine.UI;

public class MoviePlayer : MonoBehaviour
{
    private string              mediaPath                   = string.Empty;
    public string               movieName                   = string.Empty;

    public bool                 moviePaused                 = false;
    private bool                movieStarted                = false;
    private bool                videoPausedBeforeAppPause   = false;

    private Texture2D           nativeTexture               = null;
    private IntPtr              nativeTexId                 = IntPtr.Zero;
    private int                 textureWidth                = 2880;
    private int                 textureHeight               = 1440;
    private AndroidJavaObject   mediaPlayer                 = null;

    private Renderer            mediaRenderer               = null;
    private Canvas              menu                        = null;

    private int                 textTime                    = 1;

    private enum MediaSurfaceEventType
    {
        Initialize = 0,
        Shutdown = 1,
        Update = 2,
        Max_EventType
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
		    OVR_Media_Surface_SetEventBase(_eventBase);
        }
    }
    private static int _eventBase = 0;

    private static void IssuePluginEvent(MediaSurfaceEventType eventType)
    {
        GL.IssuePluginEvent((int)eventType + eventBase);
    }

    // Use this for initialization
    void Awake ()
    {
        Debug.Log("MovieSample Awake");

        OVR_Media_Surface_Init();

        mediaRenderer = GetComponent<Renderer>();
        menu = FindObjectOfType<Canvas>();

        if (menu != null)
        {
            Debug.Log("VRMenu successfully loaded.");
        }
        else
        {
            Debug.Log("No canvas provided.");
        }
        if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null)
        {
            Debug.LogError("No material for movie surface");
        }

        if (movieName != string.Empty)
        {
            Start();
            //StartCoroutine(RetrieveStreamingAsset(movieName));
        }
        else
        {
            Debug.LogError("No media file name provided");
        }
    }

    public void Start()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        StartCoroutine(RetrieveStreamingAsset(movieName));
#else

#endif
    }

    // Update is called once per frame
    void Update ()
    {
        if (!moviePaused)
        {
            IntPtr currTexId = OVR_Media_Surface_GetNativeTexture();
            if (currTexId != nativeTexId)
            {
                nativeTexId = currTexId;
                nativeTexture.UpdateExternalTexture(currTexId);
            }

            IssuePluginEvent(MediaSurfaceEventType.Update);
        }
    }

    public void CurrentPos()
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(mediaPlayer.Call<int>("getCurrentPosition"));
        StartCoroutine(ShowText(ts.ToString()));
    }

    public void CompleteRewind()
    {
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
    }

    public void Pause(bool wasPaused)
    {
        if (mediaPlayer != null)
        {
            moviePaused = wasPaused;
            try
            {
                mediaPlayer.Call((moviePaused) ? "pause" : "start");
            }
            catch (Exception e)
            {
                Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
            }
        }
    }

    void OnApplicationPause(bool appWasPaused)
    {
        Debug.Log("OnApplicationPause: " + appWasPaused);
        if (appWasPaused)
        {
            videoPausedBeforeAppPause = moviePaused;
        }

        // Pause/unpause the video only if it had been playing prior to app pause
        if (!videoPausedBeforeAppPause)
        {
            Pause(appWasPaused);
        }
    }

    void OnDestroy()
    {
        Debug.Log("Shutting down video");
        // This will trigger the shutdown on the render thread
        IssuePluginEvent(MediaSurfaceEventType.Shutdown);
        mediaPlayer.Call("stop");
        mediaPlayer.Call("release");
        mediaPlayer = null;
    }

    IEnumerator ShowText(String text)
    {
        menu.GetComponent<Text>().text = text;
        yield return new WaitForSecondsRealtime(textTime);
        menu.GetComponent<Text>().text = "";
    }

    IEnumerator RetrieveStreamingAsset(string fileName)
    {
        string streamingPath = Application.streamingAssetsPath + "/" + fileName;
        string persistentPath = Application.persistentDataPath + "/" + fileName;

        if(!File.Exists(persistentPath))
        {
            WWW wwwReader = new WWW(streamingPath);
            yield return wwwReader;

            if(wwwReader.error != null)
            {
                Debug.LogError("wwwReader error: " + wwwReader.error);
            }

            System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
        }
        mediaPath = persistentPath;

        Debug.Log("Movie FullPath: " + mediaPath);
        Debug.Log("MovieSample Start");

        StartCoroutine(DelayedStartVideo());

        menu.GetComponent<Text>().text = movieName.Substring(0, movieName.LastIndexOf(".")); ;
        yield return new WaitForSecondsRealtime(textTime);
        menu.GetComponent<Text>().text = "";
    }

    IEnumerator DelayedStartVideo()
    {
        yield return null;

        if(!movieStarted)
        {
            Debug.Log("Mediasurface DelayedStartVideo");

            movieStarted = true;

            mediaPlayer = StartVideoPlayerOnTextureId(textureWidth, textureHeight, mediaPath);
            mediaRenderer.material.mainTexture = nativeTexture;
        }

        try
        {
            mediaPlayer.Call("reset");
            mediaPlayer.Call("setDataSource", mediaPath);
            mediaPlayer.Call("prepare");
            mediaPlayer.Call("setLooping", true);
            mediaPlayer.Call("start");
        }
        catch (Exception e)
        {
            Debug.Log("Failed to start mediaPlayer with message " + e.Message);
        }
    }

    /// <summary>
	/// Set up the video player with the movie surface texture id.
	/// </summary>
	AndroidJavaObject StartVideoPlayerOnTextureId(int texWidth, int texHeight, string mediaPath)
    {
        Debug.Log("MoviePlayer: StartVideoPlayerOnTextureId");

        OVR_Media_Surface_SetTextureParms(textureWidth, textureHeight);

        IntPtr androidSurface = OVR_Media_Surface_GetObject();
#if (UNITY_ANDROID && !UNITY_EDITOR)
        AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
#else
        AndroidJavaObject mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
#endif

        // Can't use AndroidJavaObject.Call() with a jobject, must use low level interface
        //mediaPlayer.Call("setSurface", androidSurface);
        IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(), "setSurface", "(Landroid/view/Surface;)V");
        jvalue[] parms = new jvalue[1];
        parms[0] = new jvalue();
        parms[0].l = androidSurface;
        AndroidJNI.CallVoidMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parms);

        try
        {
            mediaPlayer.Call("setDataSource", mediaPath);
            mediaPlayer.Call("prepare");
            mediaPlayer.Call("setLooping", true);
            mediaPlayer.Call("start");
        }
        catch (Exception e)
        {
            Debug.Log("Failed to start mediaPlayer with message " + e.Message);
        }

        return mediaPlayer;
    }

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
}
