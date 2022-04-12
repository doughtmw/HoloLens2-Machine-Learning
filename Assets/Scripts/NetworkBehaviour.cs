// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkBehaviour : MonoBehaviour
{
    // Public fields
    //public float ProbabilityThreshold = 0.5f;
    public Vector2 InputFeatureSize = new Vector2(416, 416);
    public GameObject objectOutlineCube;
    static public List<GameObject> objectList;

    // Private fields
    private NetworkModel _networkModel;
    private MediaCaptureUtility _mediaCaptureUtility;
    private bool _isRunning = false;
    private Camera cam;

    #region UnityMethods
    async void Start()
    {

        cam = Camera.main;
        objectList = new List<GameObject>();
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

            // Run processing loop in separate parallel Task, get the latest frame
            // and asynchronously evaluate
            Debug.Log("Begin performing inference in frame grab loop.");

            _isRunning = true;
            await Task.Run(async () =>
            {
                while (_isRunning)
                {
                    if (_mediaCaptureUtility.IsCapturing)
                    {
                        using (var videoFrame = _mediaCaptureUtility.GetLatestFrame())
                        {
                            Debug.Log("Evaluating Frame....");
                            await EvaluateFrame(videoFrame);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            });
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

    void Update()
    {

    }
    #endregion

#if ENABLE_WINMD_SUPPORT
    private async Task EvaluateFrame(Windows.Media.VideoFrame videoFrame)
    {
        try
        {
            // Get the current network prediction from model and input frame
            var result = await _networkModel.EvaluateVideoFrameAsync(videoFrame);

            // Update the UI with prediction
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                if (result.Count > 0)
                {

                    if (result[0].label != "None")
                    {
                        //foreach (GameObject tempObject in objectList)
                        //{
                        //    Destroy(tempObject);
                        //}
                        GameObject newObject = Instantiate(objectOutlineCube, cam.ScreenToWorldPoint(new Vector3(result[0].bbox[0] / cam.pixelWidth, result[0].bbox[1] / cam.pixelHeight, cam.nearClipPlane)), Quaternion.identity);
                        newObject.GetComponentInChildren<TextMeshPro>().SetText(result[0].label);
                        //objectList.Add(newObject);
                    }
                }
                else
                {
                    GameObject.Find("OutputWindow").GetComponent<TextMeshPro>().SetText("Nothing Detected in Current Frame");
                }

            }, false);
        }
        catch (Exception ex)
        {
            Debug.Log($"Exception {ex}");
        }
    }

#endif
}