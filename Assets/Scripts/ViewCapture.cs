using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System.Threading.Tasks;
using System;
#if !UNITY_EDITOR
using Windows.Storage;
#endif
using UnityEngine.Windows.Speech;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

public class ViewCapture : MonoBehaviour//, IInputClickHandler
{
    AudioSource audioData;

    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    public Material material;
    //public Material cgDepthMaterial;
    //public Material cgColorMaterial;
    public Material sceneDepthMaterial;
    public Material webcamMaterial;
    public int SpatialMapLayer = 9;

    private WebCamTexture webcamTexture;
    private RenderTexture sceneDepthTexture;
    //private RenderTexture cgDepthTexture;
    //private RenderTexture cgColorTexture;

    private Camera thisCamera;
    private Camera copyCamera;
    private GameObject copyCameraGameObject;

    private bool shouldCapture = false;
    private bool isWritingFile = false;
    int frameCounter = 0;
    int maxFrame = 20;
    int fileCounter = 0;
    int maxFileCounter = 300;
    string sceneDepthFilename = "scenedepth";
    string webcamFilename = "webcam";

    byte[] sceneDepthData;
    //byte[] cgDepthData;
    //byte[] cgColorData;
    byte[] webcamData;
    byte[] cameraPoseData;

    string folder;
    string timeStamp;

    GCHandle sceneDepthHandle;
    GCHandle webcamHandle;
    IntPtr sceneDepthPtr;
   // IntPtr cgDepthPtr;
    //IntPtr cgColorPtr;
    IntPtr webcamPtr;

    Color32[] sceneDepthColor32;
    //Color32[] cgDepthColor32;
    //Color32[] cgColorColor32;
    Color32[] webcamColor32;

    byte[] sceneDepthColorBytes;
    byte[] webcamColorBytes;

    MemoryStream sceneDepthMs;
    //MemoryStream cgDepthMs;
    //MemoryStream cgColorMs;
    MemoryStream webcamMs;

    Texture2D screenGrab;
    Rect screenRect;

#if UNITY_EDITOR
    private bool isCapturing = false;
    private bool isSaved = false;
#endif


#if !UNITY_EDITOR
    StorageFolder storageFolder;
    StorageFile storageFile;
#endif

    // Use this for initialization
    void Start()
    {
        audioData = GetComponent<AudioSource>();
        keywords.Add("record", () => { StartRecord(); });
        keywords.Add("stop", () => { StopRecord(); });

        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;


#if UNITY_EDITOR
        folder = Application.persistentDataPath;
#endif
#if !UNITY_EDITOR
        storageFolder = KnownFolders.CameraRoll;
#endif

        DateTime time = DateTime.Now;
        timeStamp = string.Format("{0:D2}{1:D2}{2:D2}{3:D2}", time.Day, time.Hour, time.Minute, time.Second);
        Debug.Log(timeStamp);

        webcamTexture = new WebCamTexture(896, 504);
        webcamTexture.Play();

        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();
        
        sceneDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        //cgDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        //cgColorTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        //cgDepthMaterial.SetTexture("_DepthTex", cgDepthTexture);
        //cgColorMaterial.SetTexture("_ColorTex", cgColorTexture);
        //material.SetTexture("_WebcamTex", webcamTexture);
        sceneDepthMaterial.SetTexture("_DepthTex", sceneDepthTexture);
        webcamMaterial.SetTexture("_WebcamTex", webcamTexture);

        sceneDepthColor32 = new Color32[Screen.width * Screen.height];
        //cgDepthColor32 = new Color32[Screen.width * Screen.height];
        //cgColorColor32 = new Color32[Screen.width * Screen.height];
        webcamColor32 = new Color32[Screen.width * Screen.height];

        sceneDepthColorBytes = new byte[Screen.width * Screen.height * 4];
        webcamColorBytes = new byte[Screen.width * Screen.height * 4];

        sceneDepthMs = new MemoryStream();
        //cgDepthMs = new MemoryStream();
        //cgColorMs = new MemoryStream();
        webcamMs = new MemoryStream();

        screenGrab = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        screenRect = new Rect(0, 0, Screen.width, Screen.height);

        sceneDepthHandle = GCHandle.Alloc(sceneDepthColor32, GCHandleType.Pinned);
        sceneDepthPtr = sceneDepthHandle.AddrOfPinnedObject();

        webcamHandle = GCHandle.Alloc(webcamColor32, GCHandleType.Pinned);
        webcamPtr = webcamHandle.AddrOfPinnedObject();

        keywordRecognizer.Start();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shouldCapture && (fileCounter < maxFileCounter) && (frameCounter < maxFrame) && (webcamTexture.isPlaying))
        {
            copyCamera.CopyFrom(thisCamera);
            copyCamera.targetTexture = sceneDepthTexture;
            RenderTexture.active = sceneDepthTexture;
            copyCamera.cullingMask = 1 << SpatialMapLayer;
            copyCamera.Render();
            Graphics.Blit(source, destination, sceneDepthMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            sceneDepthColor32 = screenGrab.GetPixels32();
            AddToStream(sceneDepthMs, sceneDepthPtr, sceneDepthColorBytes);
            //AddSceneDepthToStream();

            //copyCamera.targetTexture = cgDepthTexture;
            //RenderTexture.active = cgDepthTexture;
            //copyCamera.cullingMask = (1 << 0);
            //copyCamera.cullingMask = copyCamera.cullingMask & ~(1 << SpatialMapLayer);
            //copyCamera.Render();
            //Graphics.Blit(source, destination, cgDepthMaterial);
            //screenGrab.ReadPixels(screenRect, 0, 0);
            //cgDepthColor32 = screenGrab.GetPixels32();
            //AddToStream(cgDepthMs, cgDepthColor32);

            //copyCamera.targetTexture = cgColorTexture;
            //RenderTexture.active = cgColorTexture;
            //copyCamera.Render();
            //Graphics.Blit(source, destination, cgColorMaterial);
            //screenGrab.ReadPixels(screenRect, 0, 0);
            //cgColorColor32 = screenGrab.GetPixels32();
            //AddToStream(cgColorMs, cgColorColor32);

            // Add frames to Data
            Graphics.Blit(source, destination, webcamMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            webcamColor32 = screenGrab.GetPixels32();
            //Debug.Log(webcamColor32.Length.ToString());
            //Debug.Log(webcamColorBytes.Length.ToString());
            //Debug.Log(frameCounter.ToString());
            AddToStream(webcamMs, webcamPtr, webcamColorBytes);
            //AddWebcamToStream();

            frameCounter++;
        }
        RenderTexture.active = null;
        Graphics.Blit(source, destination, material);
    }

    private void AddSceneDepthToStream()
    {
        Marshal.Copy(sceneDepthPtr, sceneDepthColorBytes, 0, Screen.width * Screen.height * 4);
        Debug.Log(sceneDepthColorBytes.Length.ToString());
        sceneDepthMs.Write(sceneDepthColorBytes, 0, sceneDepthColorBytes.Length);
    }

    private void AddWebcamToStream()
    {
        Marshal.Copy(webcamPtr, webcamColorBytes, 0, Screen.width * Screen.height * 4);
        Debug.Log(webcamColorBytes.Length.ToString());
        webcamMs.Write(webcamColorBytes, 0, webcamColorBytes.Length);
    }

    private void AddToStream(MemoryStream ms, IntPtr colorPtr, byte[] colorBytes)
    {
        DebugToServer.Log.Send("Added to stream: " + frameCounter.ToString());
        Marshal.Copy(colorPtr, colorBytes, 0, Screen.width * Screen.height * 4);
        ms.Write(colorBytes, 0, colorBytes.Length);
    }

    private async void SaveToFile(byte[] data, string header, int counter)
    {
        string exportFilename = timeStamp + header + counter.ToString() + ".dat";
#if !UNITY_EDITOR
        storageFile = await storageFolder.CreateFileAsync(exportFilename, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(storageFile, data);
#endif
#if UNITY_EDITOR
        System.IO.File.WriteAllBytes(folder + "/" + exportFilename, data);
#endif
        audioData.Play(0);
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;
        // if the keyword recognized is in our dictionary, call that Action.
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }

    private void StartRecord()
    {
        shouldCapture = true;
    }

    private void StopRecord()
    {
        shouldCapture = false;
        //Write to file
        SaveToFile(sceneDepthMs.ToArray(), sceneDepthFilename, frameCounter);
        SaveToFile(webcamMs.ToArray(), webcamFilename, frameCounter);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if ((frameCounter < 20) && (!isSaved) && (!isCapturing))
        {
            Debug.Log("Recording");
            StartRecord();
            isCapturing = true;
        }
        else if ((frameCounter == 20) && (isCapturing))
        {
            Debug.Log("Stopped");
            shouldCapture = false;
            isCapturing = false;
        }

        if ((frameCounter == 20) && (!isSaved) && (!isCapturing))
        {
            StopRecord();
            isSaved = true;
            Debug.Log("Saved");
        }
#endif
    }

    void OnApplicationQuit()
    {
        webcamTexture.Stop();
        sceneDepthHandle.Free();
        webcamHandle.Free();
#if !UNITY_EDITOR
        webcamMs.Dispose();
        sceneDepthMs.Dispose();
#endif
    }

    //// Airtap detection
    //public void OnInputClicked(InputClickedEventData eventData)
    //{
    //    Debug.Log("Clicked");
    //    ToggleShouldSave();
    //}

    //private void ToggleShouldSave()
    //{
    //    shouldSave = !shouldSave;
    //}
}
