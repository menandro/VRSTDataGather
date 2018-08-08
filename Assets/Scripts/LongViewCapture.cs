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

public class LongViewCapture : MonoBehaviour//, IInputClickHandler
{
    SocketClient dataReceiver = new SocketClient();
    private string ip_addr = "192.168.1.2";
    private string port = "13600";

    AudioSource audioData;

#if !UNITY_EDITOR
    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();
#endif

    // Materials
    public Material material;
    public Material cgDepthMaterial;
    public Material cgColorMaterial;
    public Material sceneDepthMaterial;
    public Material webcamMaterial;
    public int SpatialMapLayer = 9;

    private RenderTexture cgDepthTexture;
    private RenderTexture cgColorTexture;
    private WebCamTexture webcamTexture;
    private RenderTexture sceneDepthTexture;

    private Camera thisCamera;
    private Camera copyCamera;
    private GameObject copyCameraGameObject;

    private bool shouldCapture = false;
    private bool shouldSend = false;
    private bool shouldWrite = false;
    private bool isWritingFile = false;
    int frameCounter = 0;
    int maxFrame = 60;
    int fileCounter = 0;
    int maxFileCounter = 300;
    string sceneDepthFilename = "scenedepth";
    string webcamFilename = "webcam";
    string cgDepthFilename = "cgdepth";
    string cgColorFilename = "cgcolor";

    byte[] sceneDepthData;
    byte[] cgDepthData;
    byte[] cgColorData;
    byte[] webcamData;
    byte[] cameraPoseData;
    byte[] depthBytes;
    byte[] rgbBytes;

    string folder;
    string timeStamp;

    // For writing to file and sending data
    GCHandle handle;
    IntPtr colorPtr;
    Texture2D screenGrab;
    Rect screenRect;
    Color32[] colors;
    byte[] colorBytes;

    MemoryStream depthMs;
    MemoryStream positionMs;
    MemoryStream webcamMs;

    int msCounter = 0;
    List<MemoryStream> depthMsList;
    List<MemoryStream> webcamMsList;
    int maxMs = 3;

#if UNITY_EDITOR
    private bool isCapturing = false;
    private bool isSaved = false;
#endif

#if !UNITY_EDITOR
    StorageFolder storageFolder;
    StorageFile storageFile;
#endif

    Transform tf;

    // Use this for initialization
    void Start()
    {
        positionMs = new MemoryStream();
        depthMs = new MemoryStream();
        webcamMs = new MemoryStream();

        depthMsList = new List<MemoryStream>();
        webcamMsList = new List<MemoryStream>();

        for (int i = 0; i < maxMs; i++)
        {
            MemoryStream dms = new MemoryStream(15000000);
            depthMsList.Add(dms);

            MemoryStream wms = new MemoryStream(50000000);
            webcamMsList.Add(wms);
        }

        tf = this.transform;

        TryConnect();

        audioData = GetComponent<AudioSource>();

#if !UNITY_EDITOR
        keywords.Add("capture", () => { StartCapture(); });
        keywords.Add("send", () => { StartSend(); });
        keywords.Add("stop", () => { StopSend(); });
        keywords.Add("write", () => { StartWrite(); });
        keywords.Add("brown", () => { SaveToFile(); });

        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
#endif


#if UNITY_EDITOR
        folder = Application.persistentDataPath;
#endif
#if !UNITY_EDITOR
        storageFolder = KnownFolders.CameraRoll;
#endif

        DateTime time = DateTime.Now;
        timeStamp = string.Format("{0:D2}{1:D2}{2:D2}{3:D2}", time.Day, time.Hour, time.Minute, time.Second);
        Debug.Log(timeStamp);
        DebugToServer.Log.Send(timeStamp);

        webcamTexture = new WebCamTexture(896, 504);
        webcamTexture.Play();
        DebugToServer.Log.Send("Camera started");

        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();

        Debug.Log("Screen size: " + Screen.width.ToString() + "x" + Screen.height.ToString());
        sceneDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        cgDepthTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.Depth);
        cgColorTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        cgDepthMaterial.SetTexture("_DepthTex", cgDepthTexture);
        cgColorMaterial.SetTexture("_ColorTex", cgColorTexture);
        //material.SetTexture("_WebcamTex", webcamTexture);
        sceneDepthMaterial.SetTexture("_DepthTex", sceneDepthTexture);
        webcamMaterial.SetTexture("_WebcamTex", webcamTexture);

        colors = new Color32[Screen.width * Screen.height];
        colorBytes = new byte[Screen.width * Screen.height * 4];
        screenGrab = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
        handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
        colorPtr = handle.AddrOfPinnedObject();

        depthBytes = new byte[(Screen.width / 2) * (Screen.height / 2)];
        rgbBytes = new byte[(Screen.width / 2) * (Screen.height / 2) * 3];

#if !UNITY_EDITOR
        keywordRecognizer.Start();
#endif
        Debug.Log("Initialized.");
    }

    public async void TryConnect()
    {
        await dataReceiver.Connect(ip_addr, port);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        copyCamera.CopyFrom(thisCamera);

        if (shouldCapture)
        {
            copyCamera.targetTexture = sceneDepthTexture;
            RenderTexture.active = sceneDepthTexture;
            copyCamera.cullingMask = 1 << SpatialMapLayer;
            copyCamera.Render();
            Graphics.Blit(source, destination, sceneDepthMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SaveData(sceneDepthFilename, frameCounter);

            copyCamera.targetTexture = cgDepthTexture;
            RenderTexture.active = cgDepthTexture;
            copyCamera.cullingMask = (1 << 0);
            copyCamera.cullingMask = copyCamera.cullingMask & ~(1 << SpatialMapLayer);
            copyCamera.Render();
            Graphics.Blit(source, destination, cgDepthMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SaveData(cgDepthFilename, frameCounter);

            copyCamera.targetTexture = cgColorTexture;
            RenderTexture.active = cgColorTexture;
            copyCamera.Render();
            Graphics.Blit(source, destination, cgColorMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SaveData(cgColorFilename, frameCounter);

            Graphics.Blit(source, destination, webcamMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SaveData(webcamFilename, frameCounter);

            shouldCapture = false;
            frameCounter++;
        }

        if (shouldSend && (fileCounter < maxFileCounter) && (frameCounter < maxFrame) && (webcamTexture.isPlaying))
        {
            SendPositionData();

            copyCamera.CopyFrom(thisCamera);
            copyCamera.targetTexture = sceneDepthTexture;
            RenderTexture.active = sceneDepthTexture;
            copyCamera.cullingMask = 1 << SpatialMapLayer;
            copyCamera.Render();
            Graphics.Blit(source, destination, sceneDepthMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SendDepthData();

            Graphics.Blit(source, destination, webcamMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            SendRgbData();

            frameCounter++;
        }

        if (shouldWrite && (webcamTexture.isPlaying))
        {
            AddPositionData();

            copyCamera.CopyFrom(thisCamera);
            copyCamera.targetTexture = sceneDepthTexture;
            RenderTexture.active = sceneDepthTexture;
            copyCamera.cullingMask = 1 << SpatialMapLayer;
            copyCamera.Render();
            Graphics.Blit(source, destination, sceneDepthMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            AddDepthData();

            Graphics.Blit(source, destination, webcamMaterial);
            screenGrab.ReadPixels(screenRect, 0, 0);
            colors = screenGrab.GetPixels32();
            AddRgbData();

            frameCounter++;
            if (frameCounter >= maxFrame)
            {
                msCounter++;
                frameCounter = 0;
            }
            if (msCounter >= maxMs)
            {
                shouldWrite = false;
                audioData.Play(0);
            }
        }
        //RenderTexture.active = null;
        Graphics.Blit(source, destination, material);
    }

    private void AddPositionData()
    {
        positionMs.Write(BitConverter.GetBytes(tf.position.x), 0, 4);
        positionMs.Write(BitConverter.GetBytes(tf.position.y), 0, 4);
        positionMs.Write(BitConverter.GetBytes(tf.position.z), 0, 4);

        positionMs.Write(BitConverter.GetBytes(tf.rotation.w), 0, 4);
        positionMs.Write(BitConverter.GetBytes(tf.rotation.x), 0, 4);
        positionMs.Write(BitConverter.GetBytes(tf.rotation.y), 0, 4);
        positionMs.Write(BitConverter.GetBytes(tf.rotation.z), 0, 4);
    }

    private void AddDepthData()
    {
        for (int j = 0; j < Screen.height; j += 2)
        {
            for (int i = 0; i < Screen.width; i += 2)
            {
                depthMsList[msCounter].WriteByte(colors[j * Screen.width + i].r);
            }
        }
    }

    private void AddRgbData()
    {
        for (int j = 0; j < Screen.height; j += 2)
        {
            for (int i = 0; i < Screen.width; i += 2)
            {
                webcamMsList[msCounter].WriteByte(colors[j * Screen.width + i].r);
                webcamMsList[msCounter].WriteByte(colors[j * Screen.width + i].g);
                webcamMsList[msCounter].WriteByte(colors[j * Screen.width + i].b);
            }
        }
    }

    private void SendData()
    {
        Marshal.Copy(colorPtr, colorBytes, 0, Screen.width * Screen.height * 4);
        dataReceiver.SendBytes(colorBytes);
    }

    private void SendPositionData()
    {
        //float x, float y, float z, quaternion w, x, y, z
        //Debug.Log(tf.rotation.x);
        dataReceiver.SendFloat(tf.position.x);
        dataReceiver.SendFloat(tf.position.y);
        dataReceiver.SendFloat(tf.position.z);
        dataReceiver.SendFloat(tf.rotation.w);
        dataReceiver.SendFloat(tf.rotation.x);
        dataReceiver.SendFloat(tf.rotation.y);
        dataReceiver.SendFloat(tf.rotation.z);
    }


    private void SendDepthData()
    {

        using (MemoryStream ms = new MemoryStream())
        {
            for (int j = 0; j < Screen.height; j += 2)
            {
                for (int i = 0; i < Screen.width; i += 2)
                {
                    ms.WriteByte(colors[j * Screen.width + i].r);
                    //dataReceiver.SendByte(colors[j * Screen.width + i].r);
                }
            }
            dataReceiver.SendBytes(ms.ToArray());
        }//Marshal.Copy(colorPtr, colorBytes, 0, Screen.width * Screen.height * 4);

    }
    //dataReceiver.SendBytes(colorBytes);

    private void SendRgbData()
    {
        //Marshal.Copy(colorPtr, colorBytes, 0, Screen.width * Screen.height * 4);
        using (MemoryStream ms = new MemoryStream())
        {
            for (int j = 0; j < Screen.height; j += 2)
            {
                for (int i = 0; i < Screen.width; i += 2)
                {
                    ms.WriteByte(colors[j * Screen.width + i].r);
                    ms.WriteByte(colors[j * Screen.width + i].g);
                    ms.WriteByte(colors[j * Screen.width + i].b);
                    //dataReceiver.SendByte(colors[j * Screen.width + i].r);
                    //dataReceiver.SendByte(colors[j * Screen.width + i].g);
                    //dataReceiver.SendByte(colors[j * Screen.width + i].b);
                }
            }
            dataReceiver.SendBytes(ms.ToArray());
        }

        //dataReceiver.SendBytes(colorBytes);
    }

    private void SaveData(string header, int counter)
    {
        DebugToServer.Log.Send("Saving Data");
        MemoryStream ms = new MemoryStream();
        Marshal.Copy(colorPtr, colorBytes, 0, Screen.width * Screen.height * 4);
        ms.Write(colorBytes, 0, colorBytes.Length);
        WriteToFile(ms.ToArray(), header, counter);
        ms.Dispose();
    }

    private async void WriteToFile(byte[] data, string header, int counter)
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

#if !UNITY_EDITOR
    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;
        // if the keyword recognized is in our dictionary, call that Action.
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
#endif

    private void StartWrite()
    {
        Debug.Log("Started writing.");
        shouldWrite = true;
        audioData.Play(0);
    }

    private void StartCapture()
    {
        Debug.Log("Screen shot.");
        shouldCapture = true;
    }

    private void StartSend()
    {
        Debug.Log("Started sending.");
        shouldSend = true;
    }

    private void StopSend()
    {
        Debug.Log("Stopped sending/writing.");
        shouldSend = false;
        shouldWrite = false;
    }

    private void SaveToFile()
    {
        DateTime time = DateTime.Now;
        timeStamp = string.Format("{0:D2}{1:D2}{2:D2}{3:D2}", time.Day, time.Hour, time.Minute, time.Second);
        WriteToFile(positionMs.ToArray(), "position", maxFrame * msCounter + frameCounter);

        int cnt = 0;
        foreach (var dms in depthMsList)
        {
            WriteToFile(dms.ToArray(), sceneDepthFilename, cnt);
            cnt++;
        }

        cnt = 0;
        foreach (var wms in webcamMsList)
        {
            WriteToFile(wms.ToArray(), webcamFilename, cnt);
            cnt++;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.C))
        {
            StartCapture();
        }
        if (Input.GetKeyUp(KeyCode.G))
        {
            StartSend();
        }
        if (Input.GetKeyUp(KeyCode.V))
        {
            StopSend();
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            StartWrite();
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            SaveToFile();
        }
#endif
    }

    void OnApplicationQuit()
    {
        webcamTexture.Stop();
        handle.Free();
        dataReceiver.Disconnect();
#if !UNITY_EDITOR
#endif
    }
}
