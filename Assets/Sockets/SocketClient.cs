using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Microsoft.MixedReality.OpenXR;
using TMPro;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.MixedReality.OpenXR.ARFoundation;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#else
using System.Net.Sockets;
using System.Threading.Tasks;
#endif

[RequireComponent(typeof(ARAnchorManager))]
//Able to act as a reciever 
public class SocketClient : MonoBehaviour
{
    public String _input = "Waiting";
    String message;
    Byte[] data = System.Text.Encoding.ASCII.GetBytes("Hello from the Client");
    private StreamWriter writer;
    private StreamReader reader;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    long streamLength;
    bool anchorReceived = false;

#if !UNITY_EDITOR
    StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();
    HostName serverHost = new HostName("192.168.0.162");
    String port = "9999";
    bool _Connected = false;


#else
    Int32 port = 9999;
    bool _Active;
    string server = "192.168.0.162";
    TcpClient client = new TcpClient();
#endif

    // Use this for initialization
    void Start()
    {
#if !UNITY_EDITOR
        socket.Control.KeepAlive = false;
        Client_Start();

#else

    try
    {
      StartClient();
    }
    catch(SocketException e)
    {
      Debug.Log("SocketException: " +  e.ToString());
    }
#endif
    }


#if !UNITY_EDITOR
    private async void Client_Start()
    {
        Debug.Log("Client Started");

        try
        {
            await socket.ConnectAsync(serverHost, port);
            _Connected = true;

        }
        catch (Exception exception)
        {
            // If this is an unknown status it means that the error is fatal and retry will likely fail.
            if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
            {
                throw;
                Debug.Log("Connection Attempt failed, trying again");
                Client_Start();
            }
        }

        if (_Connected)
        {
            Debug.Log("Connected to Server");
            attemptReceiveSpatialAnchor();
        }


    }

    private async void attemptReceiveSpatialAnchor()
    {

        if (_Connected && !anchorReceived)
        {
            try
            {
                using (Stream inputStream = socket.InputStream.AsStreamForRead(16384))
                {
                        //await inputStream.FlushAsync();
                        myAnchorTransferBatch = await XRAnchorTransferBatch.ImportAsync(inputStream);
                        
                        anchorReceived = true;
                }

            }
            catch (Exception exception)
            {
                throw;
            }
        }
           
        else
        {
            Debug.Log("No Server Connection Yet");
        }


    }

#else
    public async void StartClient()
    {
            var connectionTask = client.ConnectAsync(server, port).ContinueWith(task => {
            return task.IsFaulted ? null : client;
            }, TaskContinuationOptions.ExecuteSynchronously);

            var timeoutTask = Task.Delay(5000)
            .ContinueWith<TcpClient>(task => null, TaskContinuationOptions.ExecuteSynchronously);

            var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();

            resultTask.Wait();
            var resultTcpClient = resultTask.Result;

            // Or shorter by using `await`:
            // var resultTcpClient = await resultTask;

            if (resultTcpClient != null)
            {
            receiveDataFromServer(client);
            }
            else
            {
            Debug.Log("Connection Failed after timeout.");
            }

    }


    private async void receiveDataFromServer(TcpClient client){

    // Buffer to store the response bytes.
    data = new Byte[256];
    var receiveBuffer = new byte[2048];
    int bytesRead = 0;
    NetworkStream stream = client.GetStream();
    char[] inputChar = new char[256];
    // Read the first batch of the TcpServer response bytes.

    if(stream.CanRead){
    byte[] myReadBuffer = new byte[1024];
    StringBuilder myCompleteMessage = new StringBuilder();

    // Incoming message may be larger than the buffer size.
    /*do{
         await stream.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);

         myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, 32));
    }
    while(stream.DataAvailable);

    Debug.Log(myCompleteMessage.Length);*/
    StreamReader test = new StreamReader(stream);
    var testString = await test.ReadToEndAsync();
    Debug.Log(testString.Length);
    anchorReceived = true;
    }
    }


#endif


    void Update()
    {
#if !UNITY_EDITOR
        if (anchorReceived)
        {
            try
            {
                //Debug.Log("Received from server " + Encoding.ASCII.GetString(memstream.ToArray()));
                Debug.Log(myAnchorTransferBatch.AnchorNames[0]);

            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }
            //attemptReceiveSpatialAnchor();
        }
        else
        {

        }
#else
        if (anchorReceived)
        {
            try
            {
                //Debug.Log("Received from server " + Encoding.ASCII.GetString(memstream.ToArray()));
                //Debug.Log(myAnchorTransferBatch.AnchorNames[0]);

            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }
            //attemptReceiveSpatialAnchor();
        }
        else
        {

        }
    
#endif
    }
}