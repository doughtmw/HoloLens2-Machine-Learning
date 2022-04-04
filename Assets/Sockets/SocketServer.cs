using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.WindowsMR;
using TMPro;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.MixedReality.OpenXR.ARFoundation;
using Microsoft.MixedReality.OpenXR;
using System.Threading.Tasks;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Perception.Spatial;
using Windows.Storage;
using Windows.System;
using Windows.Storage.Streams;

#else
using System.Net.Sockets;


#endif




[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARAnchor))]
public class SocketServer : MonoBehaviour
{
    public String _input = "Waiting";
    TrackableId myTrackableId;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool localAnchorAdded = false;
    bool localBatchReady = false;
    bool localAnchorStreamCreated = false;

    MemoryStream memoryStream;
    

#if !UNITY_EDITOR
    StreamSocketListener listener = new StreamSocketListener();
    String port;
    String message;
    StreamSocket clientSocket = new StreamSocket();
#else
    TcpListener server=null;
    bool _Active;
    System.Net.Sockets.NetworkStream stream;
    TcpClient client = null;
#endif

    // Use this for initialization
    async void Start()
    {



#if !UNITY_EDITOR
        
        port = "15463";
        listener.ConnectionReceived += Listener_ConnectionReceived;
        listener.Control.KeepAlive = false;
        await Listener_Start();

#else

#endif
        int success = await tryAddLocalAnchor();

    }


#if !UNITY_EDITOR
        private async Task<bool> Listener_Start()
    {
        Debug.Log("Listener started");
        try
        {
            await listener.BindServiceNameAsync(port);
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }

        Debug.Log("Listening");

        return true;
    }

    private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        try
        {
            clientSocket = args.Socket;
            //None of the Debug Messages here will show on the main thread.
            if (localAnchorStreamCreated)
            {
                long streamLength = memoryStream.Length;
                int bytesSent;
                int bufferSize = 4056;
                byte[] dataBuffer = new byte[bufferSize];
                byte[] byteAnchorStreamTemp = memoryStream.ToArray();
                byte[] finalByteArray = AddByteToArray(byteAnchorStreamTemp, byteAnchorStreamTemp[0]);
                memoryStream.Close();
                byte[] lengthBytes = BitConverter.GetBytes(byteAnchorStreamTemp.Length);

                using (Stream dataWriter = clientSocket.OutputStream.AsStreamForWrite())
                {

                    dataWriter.Write(lengthBytes, 0, lengthBytes.Length);
                    dataWriter.Flush();

                    await dataWriter.WriteAsync(byteAnchorStreamTemp, 0, byteAnchorStreamTemp.Length);
                    await dataWriter.FlushAsync();
                }



                localBatchReady = true;

            }
            else
            {
                Debug.Log("Connection Received, but no anchor batch ready to send");
                //clientSocket.WriteAsync(-1);
            }
        }

        catch(Exception e)
        {
            throw;
        }
        finally
        {
            Debug.Log("Anchor sent to client");
            clientSocket.Dispose();
        }

    }


#else

#endif

    void Update()
    {
#if !UNITY_EDITOR


        if (localBatchReady)
        {
            Debug.Log("Client Connected! - Exporting Host Anchor");
            //Debug.Log("The size of the written stream is: " + memoryStream.Length);
            localBatchReady = false;
        }


#else

#endif
    }

    public byte[] AddByteToArray(byte[] bArray, byte newByte)
    {
        byte[] newArray = new byte[bArray.Length + 1];
        bArray.CopyTo(newArray, 1);
        newArray[0] = newByte;
        return newArray;
    }

    public void StopExchange()
    {

#if UNITY_EDITOR


#else

        listener.Dispose();


            listener = null;

#endif

    }

    public void OnDestroy()
    {
        StopExchange();
    }

    async Task<int> tryAddLocalAnchor()
    {

        Debug.Log("Creating Export Anchor Batch in Socket Stream");

        while (memoryStream == null)
        {
            myTrackableId = this.GetComponent<ARAnchor>().trackableId;
            myAnchorTransferBatch.AddAnchor(myTrackableId, "HostPosition");
            memoryStream = (MemoryStream) await XRAnchorTransferBatch.ExportAsync(myAnchorTransferBatch);  
        }
        if (memoryStream != null)
        {
            Debug.Log("Anchor written to disk of size: " + memoryStream.Length);
            localAnchorStreamCreated = true;
            Debug.Log("Anchor Copied To Local Stream");

        }

        return 1;

    }

    XRAnchorSubsystem CreateXRSubsystem()
    {
        // Get all available plane subsystems
        var descriptors = new List<XRAnchorSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);

        // Find one that supports boundary vertices
        foreach (var descriptor in descriptors)
        {
            if (descriptor.supportsTrackableAttachments)
            {
                //Debug.Log("We got here!");
                // Create this plane subsystem
                return descriptor.Create();
            }
        }


        return null;
    }


}
