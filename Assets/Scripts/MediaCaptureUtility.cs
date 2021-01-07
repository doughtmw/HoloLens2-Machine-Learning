// Script taken directly from Rene Schulte's repo: https://github.com/reneschulte/WinMLExperiments/blob/master/HoloVision20/Assets/Scripts/MediaCapturer.cs

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Media;
using Windows.Media.Capture;
using Windows.Storage.Streams;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Devices;
using Windows.Graphics.Imaging;
using Windows.Devices.Enumeration;
#endif // ENABLE_WINMD_SUPPORT

public class MediaCaptureUtility
{
    public bool IsCapturing { get; set; }

#if ENABLE_WINMD_SUPPORT
    private MediaCapture _mediaCapture;
    private MediaFrameReader _mediaFrameReader;
    private VideoFrame _videoFrame;

    /// <summary>
    /// Method to start capturing camera frames at desired resolution.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public async Task InitializeMediaFrameReaderAsync(uint width = 224, uint height = 224)
    {
        // Check state of media capture object 
        if (_mediaCapture == null || _mediaCapture.CameraStreamState == CameraStreamState.Shutdown || _mediaCapture.CameraStreamState == CameraStreamState.NotStreaming)
        {
            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
            }

            // Find right camera settings and prefer back camera
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            Debug.Log($"InitializeMediaFrameReaderAsync: allCameras: {allCameras}");

            var selectedCamera = allCameras.FirstOrDefault(c => c.EnclosureLocation?.Panel == Panel.Back) ?? allCameras.FirstOrDefault();
            Debug.Log($"InitializeMediaFrameReaderAsync: selectedCamera: {selectedCamera}");


            if (selectedCamera != null)
            {
                settings.VideoDeviceId = selectedCamera.Id;
                Debug.Log($"InitializeMediaFrameReaderAsync: settings.VideoDeviceId: {settings.VideoDeviceId}");

            }

            // Init capturer and Frame reader
            _mediaCapture = new MediaCapture();
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media capture object.");

            await _mediaCapture.InitializeAsync(settings);
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully initialized media capture object.");

            var frameSource = _mediaCapture.FrameSources.Where(source => source.Value.Info.SourceKind == MediaFrameSourceKind.Color).First();
            Debug.Log($"InitializeMediaFrameReaderAsync: frameSource: {frameSource}.");

            // Convert the pixel formats
            var subtype = MediaEncodingSubtypes.Bgra8;

            // The overloads of CreateFrameReaderAsync with the format arguments will actually make a copy in FrameArrived
            BitmapSize outputSize = new BitmapSize { Width = width, Height = height };
            _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource.Value, subtype, outputSize);
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully created media frame reader.");

            _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            await _mediaFrameReader.StartAsync();
            Debug.Log("InitializeMediaFrameReaderAsync: Successfully started media frame reader.");

            IsCapturing = true;
        }
    }

    /// <summary>
    /// Retrieve the latest video frame from the media frame reader
    /// </summary>
    /// <returns>VideoFrame object with current frame's software bitmap</returns>
    public VideoFrame GetLatestFrame()
    {
        // The overloads of CreateFrameReaderAsync with the format arguments will actually return a copy so we don't have to copy again
        var mediaFrameReference = _mediaFrameReader.TryAcquireLatestFrame();
        var videoFrame = mediaFrameReference?.VideoMediaFrame?.GetVideoFrame();
        Debug.Log("GetLatestFrame: Successfully retrieved video frame.");
        return videoFrame;
    }
#endif

    /// <summary>
    /// Asynchronously stop media capture and dispose of resources
    /// </summary>
    /// <returns></returns>
    public async Task StopMediaFrameReaderAsync()
    {
#if ENABLE_WINMD_SUPPORT
        if (_mediaCapture != null && _mediaCapture.CameraStreamState != CameraStreamState.Shutdown)
        {
            await _mediaFrameReader.StopAsync();
            _mediaFrameReader.Dispose();
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
        IsCapturing = false;
#endif
    }
}