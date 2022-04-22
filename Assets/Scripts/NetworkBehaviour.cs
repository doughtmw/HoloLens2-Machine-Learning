// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using TMPro;
using System.Linq;
using System.Runtime.InteropServices;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class NetworkBehaviour : MonoBehaviour
{
    // Public fields
    public Vector2 InputFeatureSize = new Vector2(416, 416);
    public GameObject objectOutlineCube;
    public GameObject tempCamera;
    int samplingInterval = 30;
    int counter = 0;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private Camera cam;
    private PhotoCapture photoCaptureObject = null;
    List<NetworkResult> result = null;
    List<NetworkResultWithLocation> resultWithLocation = null;
    bool enablePointCloud = true;
    Vector3 currentPosition;
    Quaternion currentRotation;
    float cameraHeight;
    float cameraWidth;
    GameObject cameraGameObject;
    Camera newCamera;


#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
    byte[] frameTexture;
#endif

    #region UnityMethods

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
        IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
        //unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;
#else
        IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#endif
#endif
    }
    async void Start()
    {

        cam = Camera.main;
        cameraHeight = Camera.main.orthographicSize * 2.0f;
        cameraWidth = cameraHeight * Camera.main.aspect;

        try
        {
            // Create a new instance of the network model class
            // and asynchronously load the onnx model
            _networkModel = new NetworkModel();
#if ENABLE_WINMD_SUPPORT
            await _networkModel.InitModelAsync();
#endif
            Debug.Log("Loaded model. Starting camera...");

#if ENABLE_WINMD_SUPPORT
            // Configure camera to return frames fitting the model input size
            try
            {
                Debug.Log("Creating MediaCaptureUtility and initializing frame reader.");
                _mediaCaptureUtility = new MediaCaptureUtility();
                await _mediaCaptureUtility.InitializeMediaFrameReaderAsync(
                    (uint)InputFeatureSize.x, (uint)InputFeatureSize.y);
                Debug.Log("Camera started. Running!");

                Debug.Log("Successfully initialized frame reader.");
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to start camera: {ex.Message}. Using loaded/picked image.");

            }


        researchMode = new HL2ResearchMode();

        // Depth sensor should be initialized in only one mode
        researchMode.InitializeLongDepthSensor();
        
        researchMode.InitializeSpatialCamerasFront();
        researchMode.SetReferenceCoordinateSystem(unityWorldOrigin);
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor initialization
        researchMode.StartLongDepthSensorLoop(enablePointCloud);
        researchMode.StartSpatialCamerasFrontLoop();

#endif
        }
        catch (Exception ex)
        {
            Debug.Log("Error init:" +  ex.Message);
            Debug.Log($"Failed to start model inference: {ex}");
        }
    }

    private async void OnDestroy()
    {
        _isRunning = false;
        if (_mediaCaptureUtility != null)
        {
            await _mediaCaptureUtility.StopMediaFrameReaderAsync();
        }
    }

    async void Update()
    {
#if ENABLE_WINMD_SUPPORT
        _isRunning = true;
        counter += 1;
        int depthChannelStride = 320 * 288;
        RaycastHit hit;

        if (_isRunning && counter == samplingInterval)
        {
            counter = 0;
            //await Task.Run(async () =>
            //{
                
                if (_mediaCaptureUtility.IsCapturing)
                {
                    using (var videoFrame = _mediaCaptureUtility.GetLatestFrame())
                    {
                        currentPosition = cam.transform.position;
                        currentRotation = cam.transform.rotation;
                        cameraGameObject = new GameObject();
                        newCamera = cameraGameObject.AddComponent<Camera>();
                        newCamera.enabled = false;
                        newCamera.transform.position = currentPosition;
                        newCamera.transform.rotation = currentRotation;
                        await EvaluateFrame(videoFrame);
                        
                    }
                }

                else
                {
                    return;
                }

            if (result.Count > 0)
            {

                 if (result[0].label != "None")
                 {
                        Debug.Log("BBox x coord is: " + result[0].bbox[0] + " and BBox y coord is :" + result[0].bbox[1] + " and the width is: " + result[0].bbox[2] +
                        " and the height is: " + result[0].bbox[3]);

                        Ray ray = newCamera.ViewportPointToRay(new Vector3(result[0].bbox[0]/InputFeatureSize.x, result[0].bbox[1]/InputFeatureSize.y, 0));
   
                        if (Physics.Raycast(ray, out hit)){

                            Vector3 tempLocation = ray.origin + (ray.direction * hit.distance);
                            GameObject newObject = Instantiate(objectOutlineCube, tempLocation, Quaternion.identity);
                            newObject.transform.localScale = new Vector3(1*(result[0].bbox[2]/InputFeatureSize.x)*hit.distance, 1*(result[0].bbox[3]/InputFeatureSize.y)*hit.distance, .2F);
                            newObject.GetComponentInChildren<TextMeshPro>().SetText("Object: " + result[0].label + " Confidence: " + Math.Round((decimal)result[0].prob, 2)*100 + "%");
                            //newObject.transform.LookAt(cam.transform, newObject.transform.up);
                            Debug.Log("Created 3D Bounding Box at " + tempLocation);
                            newObject.GetComponent<BoundingBoxScript>().networkResultWithLocation = new NetworkResultWithLocation(result[0].label, result[0].bbox, result[0].prob, newObject);
                            //resultWithLocation.Add(new NetworkResultWithLocation(result[0].label, result[0].bbox, result[0].prob, newObject);
                            }

                  }
            }
            else
            {
                    GameObject.Find("OutputWindow").GetComponent<TextMeshPro>().SetText("Nothing Detected in Current Frame");
            }

            Destroy(cameraGameObject);
            //});

        }
#endif
    }
    #endregion

#if ENABLE_WINMD_SUPPORT
    private async Task EvaluateFrame(Windows.Media.VideoFrame videoFrame)
    {
        try
        {
            // Get the current network prediction from model and input frame
            result = await _networkModel.EvaluateVideoFrameAsync(videoFrame);

            // Update the UI with prediction
            //UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            //{


            //}, false);
        }
        catch (Exception ex)
        {
            Debug.Log($"Exception {ex}");
        }
    }

#endif

}