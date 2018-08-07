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


public class SimpleCapture : MonoBehaviour {
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
    private bool isWritingFile = false;
    int frameCounter = 0;
    int maxFrame = 200;
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
    void Start () {
        tf = this.transform;

        thisCamera = this.gameObject.GetComponent<Camera>();
        copyCameraGameObject = new GameObject("Depth Renderer Camera");
        copyCamera = copyCameraGameObject.AddComponent<Camera>();
        


        Debug.Log("Initialized.");
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
