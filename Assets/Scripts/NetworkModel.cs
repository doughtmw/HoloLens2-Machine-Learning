// Adapted from the WinML MNIST sample and Rene Schulte's repo 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST
// https://github.com/reneschulte/WinMLExperiments/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_WINMD_SUPPORT
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Storage;
using System.Diagnostics;
using Windows.Media.Capture;
#endif


public struct NetworkResult
{
    public string label;
    public List<float> bbox;
    public double prob;

    public NetworkResult(string inlabel, List<float> inbbox, double inprob)
    {
        label = inlabel;
        bbox = inbbox;
        prob = inprob;
    }
}

public struct NetworkResultWithLocation
{
    public string label;
    public List<float> bbox;
    public double prob;
    GameObject gameObject;

    public NetworkResultWithLocation(string inlabel, List<float> inbbox, double inprob, GameObject inObject)
    {
        label = inlabel;
        bbox = inbbox;
        prob = inprob;
        gameObject = inObject;
    }
}

public class DimensionsBase
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; }
    public float Width { get; set; }
}

public class BoundingBoxDimensions : DimensionsBase { }

class Comparer : IComparer<NetworkResult>
{
    public int Compare(NetworkResult x, NetworkResult y)
    {
        return y.prob.CompareTo(x.prob);
    }
}



public class NetworkModel
{
    private string[] labels = new string[]
{
    "aeroplane", "bicycle", "bird", "boat", "bottle",
    "bus", "car", "cat", "chair", "cow",
    "diningtable", "dog", "horse", "motorbike", "person",
    "pottedplant", "sheep", "sofa", "train", "tvmonitor"
};

    private float[] anchors = new float[]
{
    1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
};

    public const int ROW_COUNT = 13;
    public const int COL_COUNT = 13;
    public const int CHANNEL_COUNT = 125;
    public const int BOXES_PER_CELL = 5;
    public const int BOX_INFO_FEATURE_COUNT = 5;
    public const int CLASS_COUNT = 20;
    public const float CELL_WIDTH = 32;
    public const float CELL_HEIGHT = 32;

    private int channelStride = ROW_COUNT * COL_COUNT;

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _media_capture;
    private LearningModel _model;
    private LearningModelSession _session;
    private LearningModelBinding _binding;

#endif



#if ENABLE_WINMD_SUPPORT


    public async Task InitModelAsync()
    {
        var model_file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets//tinyyolov2-7.onnx"));
        _model = await LearningModel.LoadFromStorageFileAsync(model_file);
       // var device = new LearningModelDevice(LearningModelDeviceKind.Cpu);
        _session = new LearningModelSession(_model);
        _binding = new LearningModelBinding(_session);

    }
    public async Task<List<NetworkResult>> EvaluateVideoFrameAsync(VideoFrame inputFrame)
    {
        // Sometimes on HL RS4 the D3D surface returned is null, so simply skip those frames
        if (_model == null || inputFrame == null || (inputFrame.Direct3DSurface == null && inputFrame.SoftwareBitmap == null))
        {
            UnityEngine.Debug.Log("EvaluateVideoFrameAsync: No detection, null frame or model not initialized.");
            var tempResult = new NetworkResult("None", new List<float>(), 0);
           List<NetworkResult> tempResultList = new List<NetworkResult>();
            tempResultList.Add(tempResult);
            return tempResultList;
       }
        
        try{

            // Perform network model inference using the input data tensor, cache output and time operation
            var stopwatch = Stopwatch.StartNew();
            var results = await EvaluateFrame(inputFrame);
            List<NetworkResult> final_detections = (List<NetworkResult>)DrawBoxes(results.ToArray());
            stopwatch.Stop();

        return final_detections;
        }

         catch (Exception ex)
        {

             UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                UnityEngine.Debug.Log(ex.Message + ex.StackTrace);
            }, false);

            var tempResult = new NetworkResult("None - Exception Thrown", new List<float>(), 0);
            List<NetworkResult> tempResultList = new List<NetworkResult>();
            tempResultList.Add(tempResult);
            return tempResultList;
        }

    }

   private async Task<List<float>> EvaluateFrame(VideoFrame frame)
        {

            _binding.Clear();
            _binding.Bind("image", frame);

            var results = await _session.EvaluateAsync(_binding, "");

            TensorFloat result = results.Outputs["grid"] as TensorFloat;
            var shape = result.Shape;
            var data = result.GetAsVectorView();
            
            return data.ToList<float>();
        }


#endif

    private float ComputeIOU(NetworkResult DRa, NetworkResult DRb)
    {
        float ay1 = DRa.bbox[0];
        float ax1 = DRa.bbox[1];
        float ay2 = DRa.bbox[2];
        float ax2 = DRa.bbox[3];
        float by1 = DRb.bbox[0];
        float bx1 = DRb.bbox[1];
        float by2 = DRb.bbox[2];
        float bx2 = DRb.bbox[3];

        UnityEngine.Debug.Assert(ay1 < ay2);
        UnityEngine.Debug.Assert(ax1 < ax2);
        UnityEngine.Debug.Assert(by1 < by2);
        UnityEngine.Debug.Assert(bx1 < bx2);

        // determine the coordinates of the intersection rectangle
        float x_left = Math.Max(ax1, bx1);
        float y_top = Math.Max(ay1, by1);
        float x_right = Math.Min(ax2, bx2);
        float y_bottom = Math.Min(ay2, by2);

        if (x_right < x_left || y_bottom < y_top)
            return 0;
        float intersection_area = (x_right - x_left) * (y_bottom - y_top);
        float bb1_area = (ax2 - ax1) * (ay2 - ay1);
        float bb2_area = (bx2 - bx1) * (by2 - by1);
        float iou = intersection_area / (bb1_area + bb2_area - intersection_area);

        UnityEngine.Debug.Assert(iou >= 0 && iou <= 1);
        return iou;
    }

    private List<NetworkResult> NMS(IReadOnlyList<NetworkResult> detections,
        float IOU_threshold = 0.45f,
        float score_threshold = 0.3f)
    {
        List<NetworkResult> final_detections = new List<NetworkResult>();
        for (int i = 0; i < detections.Count; i++)
        {
            int j = 0;
            for (j = 0; j < final_detections.Count; j++)
            {
                if (ComputeIOU(final_detections[j], detections[i]) > IOU_threshold)
                {
                    break;
                }
            }
            if (j == final_detections.Count)
            {
                final_detections.Add(detections[i]);
            }
        }
        return final_detections;
    }

    private float Sigmoid(float value)
    {
        var k = (float)Math.Exp(value);
        return k / (1.0f + k);
    }

    private float[] Softmax(float[] values)
    {
        var maxVal = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxVal));
        var sumExp = exp.Sum();

        return exp.Select(v => (float)(v / sumExp)).ToArray();
    }

    private int GetOffset(int x, int y, int channel)
    {
        // YOLO outputs a tensor that has a shape of 125x13x13, which 
        // WinML flattens into a 1D array.  To access a specific channel 
        // for a given (x,y) cell position, we need to calculate an offset
        // into the array
        return (channel * this.channelStride) + (y * COL_COUNT) + x;
    }

    private BoundingBoxDimensions ExtractBoundingBoxDimensions(float[] modelOutput, int x, int y, int channel)
    {
        return new BoundingBoxDimensions
        {
            X = modelOutput[GetOffset(x, y, channel)],
            Y = modelOutput[GetOffset(x, y, channel + 1)],
            Width = modelOutput[GetOffset(x, y, channel + 2)],
            Height = modelOutput[GetOffset(x, y, channel + 3)]
        };
    }

    private float GetConfidence(float[] modelOutput, int x, int y, int channel)
    {
        return Sigmoid(modelOutput[GetOffset(x, y, channel + 4)]);
    }

    public float[] ExtractClasses(float[] modelOutput, int x, int y, int channel)
    {
        float[] predictedClasses = new float[CLASS_COUNT];
        int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;
        for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
        {
            predictedClasses[predictedClass] = modelOutput[GetOffset(x, y, predictedClass + predictedClassOffset)];
        }
        return Softmax(predictedClasses);
    }

    private ValueTuple<int, float> GetTopResult(float[] predictedClasses)
    {
        return predictedClasses
            .Select((predictedClass, index) => (Index: index, Value: predictedClass))
            .OrderByDescending(result => result.Value)
            .First();
    }

    class CellDimensions : DimensionsBase { }

    private CellDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions)
    {
        return new CellDimensions
        {
            X = ((float)x + Sigmoid(boxDimensions.X)) * CELL_WIDTH,
            Y = ((float)y + Sigmoid(boxDimensions.Y)) * CELL_HEIGHT,
            Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH * anchors[box * 2],
            Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT * anchors[box * 2 + 1],
        };
    }

    private List<NetworkResult> ParseResult(float[] results)
    {
 
        float threshold = 0.5f;
        List<NetworkResult> detections = new List<NetworkResult>();

        for (int row = 0; row < ROW_COUNT; row++)
        {
            for (int column = 0; column < COL_COUNT; column++)
            {
                for (int box = 0; box < BOXES_PER_CELL; box++)
                {
                    var channel = (box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT));
                    BoundingBoxDimensions boundingBoxDimensions = ExtractBoundingBoxDimensions(results, row, column, channel);
                    float confidence = GetConfidence(results, row, column, channel);
                    CellDimensions mappedBoundingBox = MapBoundingBoxToCell(row, column, box, boundingBoxDimensions);


                    if (confidence < threshold)
                        continue;

                    float[] predictedClasses = ExtractClasses(results, row, column, channel);
                    var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                    var topScore = topResultScore * confidence;

                    if (topScore < threshold)
                        continue;

                    List<float> tempBbox = new List<float>();
                    //tempBbox.Add((mappedBoundingBox.X - mappedBoundingBox.Width / 2));
                    //tempBbox.Add((mappedBoundingBox.Y - mappedBoundingBox.Height / 2));
                    tempBbox.Add(mappedBoundingBox.X);
                    tempBbox.Add(mappedBoundingBox.Y);
                    tempBbox.Add(mappedBoundingBox.Width);
                    tempBbox.Add(mappedBoundingBox.Height);

                    detections.Add(new NetworkResult()
                    {
                        label = labels[topResultIndex],
                        bbox = tempBbox,
                        prob = topScore
                    });
                }
            }
        }

        return detections;
    }

    private IReadOnlyList<NetworkResult> DrawBoxes(float[] results)
    {
        List<NetworkResult> detections = ParseResult(results);
        Comparer cp = new Comparer();
        detections.Sort(cp);
        IReadOnlyList<NetworkResult> final_detections = NMS(detections);

        for (int i = 0; i < final_detections.Count; ++i)
        {
           // int top = (int)(final_detetions[i].bbox[0] * WebCam.Height);
            int top = (int)(final_detections[i].bbox[0]);
            int left = (int)(final_detections[i].bbox[1]);
            int bottom = (int)(final_detections[i].bbox[2]);
            int right = (int)(final_detections[i].bbox[3]);

        }


        return final_detections;
    }

}