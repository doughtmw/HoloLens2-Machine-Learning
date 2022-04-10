// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Storage;
using System.Diagnostics;
#endif 


public struct NetworkResult
{
    public NetworkResult(string pred, float prob, long time)
    {
        PredictionLabel = pred;
        PredictionProbability = prob;
        PredictionTime = time;
        //PredictionBbox = boundingbox;
}

    public string PredictionLabel { get; }
    public float PredictionProbability { get; }
    public long PredictionTime { get; }
    //public List<float> PredictionBbox { get; }
}

public class NetworkModel
{
    public string OnnxFileName = "model.onnx";
    public string LabelsFileName = "Labels.json";
    public float DetectionThreshold = 0.5f;
    public float IOU_threshold = 0.45f;

    private List<string> _labels = new List<string>();
    private NetworkResult _networkResult;

#if ENABLE_WINMD_SUPPORT
    private CustomNetworkModel _customNetworkModel;
    private CustomNetworkInput _customNetworkInput = new CustomNetworkInput();
    private CustomNetworkOutput _customNetworkOutput = new CustomNetworkOutput();
#endif 

    /// <summary>
    /// Asyncrhonously load the onnx model from Visual Studio assets folder 
    /// </summary>
    /// <returns></returns>
    public async Task LoadModelAsync()
    {
        try
        {
            // Parse imagenet labels from label json file
            // https://github.com/reneschulte/WinMLExperiments/
            var labelsTextAsset = Resources.Load(LabelsFileName) as TextAsset;
            using (var streamReader = new StringReader(labelsTextAsset.text))
            {
                string line = "";
                char[] charToTrim = { '\"', ' ' };
                while (streamReader.Peek() >= 0)
                {
                    line = streamReader.ReadLine();
                    line.Trim(charToTrim);
                    var indexAndLabel = line.Split(':');
                    if (indexAndLabel.Count() == 2)
                    {
                        _labels.Add(indexAndLabel[1]);
                    }
                }
            }

#if ENABLE_WINMD_SUPPORT
            // Load onnx model from Visual studio assets folder, build VS project in Unity
            // then add the onnx model to the Assets folder in visual studio solution
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/model.onnx"));
            _customNetworkModel = await CustomNetworkModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            UnityEngine.Debug.Log("LoadModelAsync: Onnx model loaded successfully.");
#endif 
        }

        catch
        {
#if ENABLE_WINMD_SUPPORT
            _customNetworkModel = null;
            UnityEngine.Debug.Log("LoadModelAsync: Onnx model failed to load.");
#endif 
            throw;
        }
    }

#if ENABLE_WINMD_SUPPORT
    public async Task<NetworkResult> EvaluateVideoFrameAsync(
        VideoFrame inputFrame)
    {
        // Sometimes on HL RS4 the D3D surface returned is null, so simply skip those frames
        if (_customNetworkModel == null || inputFrame == null || (inputFrame.Direct3DSurface == null && inputFrame.SoftwareBitmap == null))
        {
            UnityEngine.Debug.Log("EvaluateVideoFrameAsync: No detection, null frame or model not initialized.");
            return new NetworkResult("None", 0f, 0); ;
        }

        // Cache the input video frame to network input
        _customNetworkInput.features = ImageFeatureValue.CreateFromVideoFrame(inputFrame);

        // Perform network model inference using the input data tensor, cache output and time operation
        var stopwatch = Stopwatch.StartNew();
        _customNetworkOutput = await _customNetworkModel.EvaluateAsync(_customNetworkInput);
        stopwatch.Stop();

        // Convert prediction to datatype
        var outVec = _customNetworkOutput.prediction.GetAsVectorView().ToList();

        // LINQ query to check for highest probability digit
        if (outVec.Max() > DetectionThreshold)
        {
            // Get the index of max probability value
            var maxProb = outVec.Max();
            var maxIndex = outVec.IndexOf(maxProb);

            UnityEngine.Debug.Log($"EvaluateVideoFrameAsync: Prediction [{_labels[maxIndex]}] time: [{stopwatch.ElapsedMilliseconds} ms]");

            // Return the detections
            return new NetworkResult(_labels[maxIndex], maxProb, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            return new NetworkResult("No prediction exceeded probability threshold.", 0f, stopwatch.ElapsedMilliseconds); ;

        }
    }
#endif
}