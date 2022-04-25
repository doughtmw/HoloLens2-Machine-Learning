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
using System.Runtime.Serialization.Formatters.Binary;

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

public class SocketServer : MonoBehaviour
{
    private enum SystemStates
    {
        Initializing,
        CreatingAnchor,
        TransferingAnchor,
        AnchorTransfered
    }
    public int currentState;
    TrackableId myTrackableId;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool clientConnected = false;
    bool anchorFresh = false;
    int counter = 0;

    
    

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

        currentState = (int)SystemStates.Initializing;

#if !UNITY_EDITOR

        port = "15463";
        listener.ConnectionReceived += Listener_ConnectionReceived;
        listener.Control.KeepAlive = false;
        await Listener_Start();

#else

#endif


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
        clientSocket = args.Socket;
        clientConnected = true;
    }

#else

#endif


    async void Update()
    {
        MemoryStream memoryStream = new MemoryStream();
#if !UNITY_EDITOR
        counter += 1;

        if (counter >= 60 && clientConnected)
        {
            counter = 0;
            if(currentState != 1)
            {
                memoryStream = await tryAddLocalAnchor();
                while (memoryStream == null)
                {
                    memoryStream = await tryAddLocalAnchor();
                }
                anchorFresh = true;
            }

            if(currentState != 2 && anchorFresh)
            {
                await trysendAnchor(memoryStream);
            }

            memoryStream.Close();
        }


#else

#endif
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

#if !UNITY_EDITOR
    async Task<MemoryStream> tryAddLocalAnchor()
    {
        MemoryStream memoryStream;
        //Debug.Log("Creating Export Anchor Batch in Socket Stream");
        try
        {
            if(myAnchorTransferBatch.AnchorNames.Count > 1)
            {
                myAnchorTransferBatch.RemoveAnchor("ParentAnchor");
            }
            myTrackableId = GameObject.Find("AnchorParent").GetComponent<ARAnchor>().trackableId;
            myAnchorTransferBatch.AddAnchor(myTrackableId, "ParentAnchor");
            memoryStream = (MemoryStream)await XRAnchorTransferBatch.ExportAsync(myAnchorTransferBatch);


            return memoryStream;
        }
        catch(Exception e)
        {
            return null;
        }


    }


    async Task<bool> trysendAnchor(MemoryStream memoryStream)
    {
        currentState = (int)SystemStates.TransferingAnchor;
        try
        {
                long streamLength = memoryStream.Length;
                int bytesSent = 0;
                int bufferSize = 8192;
                byte[] dataBuffer = new byte[bufferSize];
                byte[] byteAnchorStreamTemp = memoryStream.ToArray();
                int clientAnchorReceived = 0;

                //memoryStream.Close();
                byte[] lengthBytes = BitConverter.GetBytes(byteAnchorStreamTemp.Length);

                using (Stream dataWriter = clientSocket.OutputStream.AsStreamForWrite())
                {
                    using (Stream dataReader = clientSocket.InputStream.AsStreamForRead())
                    {

                        await Task.WhenAll(dataWriter.WriteAsync(lengthBytes, 0, lengthBytes.Length), dataWriter.FlushAsync());

                        await Task.WhenAll(dataWriter.WriteAsync(byteAnchorStreamTemp, 0, byteAnchorStreamTemp.Length), dataWriter.FlushAsync());

                    }
                }
            return true;
    
        }
        catch (Exception e)
        {
            return false;
            throw;
        }
        finally
        {
            Debug.Log("Anchor sent to client");
            //Client Stream will hang unless socket connection is closed.  Wait given milliseconds for the client buffer to be read into it's memory
            //await Task.Delay(5000);
            currentState = (int)SystemStates.AnchorTransfered;
            //clientSocket.Dispose();
        }

    }
#endif

}
