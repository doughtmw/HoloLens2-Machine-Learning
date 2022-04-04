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
using System.Threading.Tasks;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#else
using System.Net.Sockets;

#endif

[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARAnchor))]
//Able to act as a reciever 
public class SocketClient : MonoBehaviour
{
    public String _input = "Waiting";
    String message;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool anchorReceived = false;
    MemoryStream tempStream = new MemoryStream();

#if !UNITY_EDITOR
    StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();
    HostName serverHost = new HostName("192.168.0.162");
    String port = "15463";
    bool _Connected = false;


#else
    Int32 port = 15463;
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
        // Buffer to store the response bytes.
        byte[] lengthBuffer = new byte[256];
        byte[] singleByte = new byte[1];
        int bytesRead = 0;
        int totalBytes = 0;
        int bufferSize = 8192;
        double progress = 0;
        int counter = 0;
        int streamLength;
        MemoryStream tempMemStream;

        if (_Connected && !anchorReceived)
        {
            try
            {
                using (Stream dataReader = socket.InputStream.AsStreamForRead())
                {
                    
                    await dataReader.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                    streamLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] myReadBuffer = new byte[bufferSize];
                    byte[] tempByteArray = new byte[streamLength];
                    Debug.Log("Attempting to read anchor of size: " + streamLength);
                    // Incoming message may be larger than the buffer size.


                    while (totalBytes < streamLength)
                    {

                        bytesRead = await dataReader.ReadAsync(myReadBuffer, 0, bufferSize);
                        Array.Copy(myReadBuffer, 0, tempByteArray, totalBytes, bytesRead);
                        totalBytes += bytesRead;
                        counter += 1;

                        if (counter == 120)
                        {
                            progress = (Convert.ToDouble(totalBytes) / Convert.ToDouble(streamLength)) * 100;
                            Debug.Log("Recv'd " + progress + "% of expected Anchor");
                            counter = 0;
                        }


                    }

                    tempMemStream = new MemoryStream(tempByteArray);
                    
                }
                Debug.Log("Anchor Received");
                if (tempMemStream.CanRead)
                {
                    Debug.Log("Attempting to import anchor locally...");
                    myAnchorTransferBatch = await XRAnchorTransferBatch.ImportAsync(tempMemStream);
                    while (myAnchorTransferBatch == null)
                    {
                        Debug.Log("Trying Again...");
                        myAnchorTransferBatch = await XRAnchorTransferBatch.ImportAsync(tempStream);
                    }

                    if(myAnchorTransferBatch != null)
                    {
                        Debug.Log("Host Anchor Imported to Local System");
                    }
                    
                }
                else { Debug.Log("tempStream not readable"); }

                anchorReceived = true;
            }
            catch (Exception exception)
            {
                throw;
            }

            finally
            {
                
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
    var lengthBuffer = new byte[256];
    int bytesRead = 0;
    long totalBytes = 0;
    int bufferSize = 4056;
    float progress = 0;
    int counter = 0;
    NetworkStream stream = client.GetStream();
    // Read the first batch of the TcpServer response bytes.

    if(stream.CanRead){
    
    await stream.ReadAsync(lengthBuffer, 0, 256);
    int streamLength = BitConverter.ToInt32(lengthBuffer, 0);
    byte[] myReadBuffer = new byte[bufferSize];
    int streamLengthStart = streamLength;
    Debug.Log("Attempting to read anchor of size: " + streamLength);

    // Incoming message may be larger than the buffer size.

   while(totalBytes < streamLength)
    {
    
        if(counter == 120){
        progress = ((float)totalBytes/(float)streamLengthStart) * 100;
        Debug.Log("Recv'd " + progress);
        counter = 0;
        }
    
         bytesRead = await stream.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);
         //Debug.Log("Bytes Read: " + bytesRead);
         await tempStream.WriteAsync(myReadBuffer, 0, bytesRead);
         //streamLength -= bytesRead;
         totalBytes += bytesRead;
         counter += 1;
         //Debug.Log("tempStream Length: " + tempStream.Length);

    }
    Debug.Log("Anchor Received");
    myAnchorTransferBatch = await XRAnchorTransferBatch.ImportAsync(tempStream);
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
                Debug.Log(myAnchorTransferBatch.AnchorNames[0]);
                anchorReceived = false;

            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }
        }
        else
        {

        }
#else
        if (anchorReceived)
        {
            try
            {
                Debug.Log("AnchorReceived");
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
