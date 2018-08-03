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

public class PostStabilizeCapture : MonoBehaviour//, IInputClickHandler
{
    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    public Material material;
    public Material cgDepthMaterial;
    public Material cgColorMaterial;
    public Material spatialDepthMaterial;
    public Material webcamMaterial;
    public int SpatialMapLayer = 9;

    private WebCamTexture webcamTexture;
    private RenderTexture spatialMapDepthTexture;
    private RenderTexture cgDepthTexture;
    private RenderTexture cgColorTexture;
    private Texture2D webcamTextureClone;

    private Texture2D screenGrab;
    private Texture2D webcamGrab;

    private Camera thisCamera;
    private Camera copyCamera;
    private GameObject copyCameraGameObject;

    private bool shouldSave = false;
    private bool isWritingFile = false;
    int fileCounter = 0;
    int maxFileCounter = 300;
    string spatialMapFilename = "spatial";
    string webcamFilename = "webcam";
    string cgDepthFilename = "cgdepth";
    string cgColorFilename = "cgcolor";
    
    byte[] spatialMapToWrite;
    byte[] cgDepthToWrite;
    byte[] cgColorToWrite;
    byte[] webcamToWrite;

    string folder;
    string timeStamp;

#if !UNITY_EDITOR
    StorageFolder storageFolder;
    StorageFile storageFile;
#endif

    // Use this for initialization
    void Start () {
        keywords.Add("record", () => { StartRecord(); });
        keywords.Add("stop", () => { StopRecord(); });

        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;


#if UNITY_EDITOR
        folder = Application.persistentDataPath;
        Debug.Log(folder);
#endif
#if !UNITY_EDITOR
        storageFolder = KnownFolders.CameraRoll;
#endif

        DateTime time = DateTime.Now;
        timeStamp = string.Format("{0:D2}{1:D2}{2:D2}{3:D2}", time.Day, time.Hour, time.Minute, time.Second);
        Debug.Log(timeStamp);

        InputManager.Instance.PushFallbackInputHandler(gameObject);
        
        webcamTexture = new WebCamTexture(896, 504);
        webcamTexture.Play();

        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();

        webcamTextureClone = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.ARGB32, false);
        spatialMapDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        cgDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        cgColorTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        screenGrab = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        webcamGrab = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.ARGB32, false);

        //material.SetTexture("_SpatialMapTex", spatialMapDepthTexture);
        cgDepthMaterial.SetTexture("_DepthTex", cgDepthTexture);
        cgColorMaterial.SetTexture("_ColorTex", cgColorTexture);
        //material.SetTexture("_WebcamTex", webcamTexture);
        spatialDepthMaterial.SetTexture("_DepthTex", spatialMapDepthTexture);
        webcamMaterial.SetTexture("_WebcamTex", webcamTextureClone);

        keywordRecognizer.Start();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shouldSave && (fileCounter < maxFileCounter))
        {
            copyCamera.CopyFrom(thisCamera);
            copyCamera.targetTexture = spatialMapDepthTexture;
            RenderTexture.active = spatialMapDepthTexture;
            copyCamera.cullingMask = 1 << SpatialMapLayer;
            copyCamera.Render();
            Graphics.Blit(source, destination, spatialDepthMaterial);
            //Save
            screenGrab.ReadPixels(new Rect(0, 0, spatialMapDepthTexture.width, spatialMapDepthTexture.height), 0, 0);
            screenGrab.Apply();
            //spatialMapToWrite = screenGrab.EncodeToPNG();

            //copyCamera.targetTexture = cgDepthTexture;
            //RenderTexture.active = cgDepthTexture;
            //copyCamera.cullingMask = (1 << 0);
            //copyCamera.cullingMask = copyCamera.cullingMask & ~(1 << SpatialMapLayer);
            //copyCamera.Render();
            //Graphics.Blit(source, destination, cgDepthMaterial);
            ////Save
            //screenGrab.ReadPixels(new Rect(0, 0, cgDepthTexture.width, cgDepthTexture.height), 0, 0);
            //screenGrab.Apply();
            //cgDepthToWrite = screenGrab.EncodeToPNG();

            //copyCamera.targetTexture = cgColorTexture;
            //RenderTexture.active = cgColorTexture;
            //copyCamera.Render();
            //Graphics.Blit(source, destination, cgColorMaterial);
            ////Save
            //screenGrab.ReadPixels(new Rect(0, 0, cgColorTexture.width, cgColorTexture.height), 0, 0);
            //screenGrab.Apply();
            //cgColorToWrite = screenGrab.EncodeToPNG();

            //Graphics.CopyTexture(webcamTexture, webcamGrab);
            Graphics.CopyTexture(webcamTexture, webcamTextureClone);
            webcamTextureClone.Apply();
            Graphics.Blit(source, destination, webcamMaterial);
            webcamGrab.ReadPixels(new Rect(0, 0, spatialMapDepthTexture.width, spatialMapDepthTexture.height), 0, 0);
            webcamGrab.Apply();
            //webcamToWrite = webcamGrab.EncodeToPNG();

            //Task.Factory.StartNew(() => SaveToFile(spatialMapToWrite, spatialMapFilename, fileCounter));
            //Task.Factory.StartNew(() => SaveToFile(webcamToWrite, webcamFilename, fileCounter));

            //Task.Factory.StartNew(() => SaveToFile(cgDepthToWrite, cgDepthFilename, fileCounter));
            //Task.Factory.StartNew(() => SaveToFile(cgColorToWrite, cgColorFilename, fileCounter));

            //System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" +
            //    spatialMapFilename + fileCounter.ToString() + ".png", spatialMapToWrite);

            //System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" +
            //    webcamFilename + fileCounter.ToString() + ".png", webcamToWrite);

            //System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" +
            //   cgDepthFilename + fileCounter.ToString() + ".png", cgDepthToWrite);

            //System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" +
            //   cgColorFilename + fileCounter.ToString() + ".png", cgColorToWrite);

            fileCounter++;
            shouldSave = false;
        }
        RenderTexture.active = null;
        Graphics.Blit(source, destination, material);
        //Graphics.Blit(source, destination, material);
    }

    private async void SaveToFile(byte[] data, string header, int counter)
    {
#if !UNITY_EDITOR
        string exportFilename = timeStamp + header + counter.ToString() + ".png";
        storageFile = await storageFolder.CreateFileAsync(exportFilename, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteBytesAsync(storageFile, data);
#endif
#if UNITY_EDITOR
        System.IO.File.WriteAllBytes(folder + "/" +
               timeStamp + header + counter.ToString() + ".png", data);
#endif
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
        shouldSave = true;
    }

    private void StopRecord()
    {
        shouldSave = false;
    }

    // Update is called once per frame
    void Update () {
#if UNITY_EDITOR
        if (fileCounter < 300)
        {
            shouldSave = true;
        }
        else
        {
            shouldSave = false;
        }
        
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
