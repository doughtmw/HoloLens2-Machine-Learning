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
        // Send a request to the echo server.
        //string request = "Hello, World!";
        //using (Stream outputStream = socket.OutputStream.AsStreamForWrite())
        //{
        //    using (var streamWriter = new StreamWriter(outputStream))
        //    {
        //       await streamWriter.WriteLineAsync(request);
        //        await streamWriter.FlushAsync();
        //   }
        //}

        attemptReceiveSpatialAnchor();

    }

    private async void attemptReceiveSpatialAnchor()
    {

        if(_Connected)
            try
            {
                using (Stream inputStream = socket.InputStream.AsStreamForRead())
                {
                    using (var streamReader = new StreamReader(inputStream))
                    {
                        var bytes = default(byte[]);
                        using (var memstream = new MemoryStream())
                        {
                            await streamReader.BaseStream.CopyToAsync(memstream);
                            bytes = memstream.ToArray();
                            //_input = bytes.ToString();
                            Debug.Log(System.Text.Encoding.ASCII.GetString(bytes));
                        }

                    }
                }
            }
            catch (Exception exception)
            {
                throw;
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
            sendDataToServer(client, data);
            Debug.Log(receiveDataFromServer(client));
            }
            else
            {
            Debug.Log("Connection Failed after timeout.");
            }

    }

    public bool sendDataToServer(TcpClient client, Byte[] data){
        NetworkStream stream = client.GetStream();
        uint messageSize = (uint)data.Length;

        data = AddByteToBeginningArray(data, Convert.ToByte(messageSize));
        stream.Write(data, 0, data.Length);
        return true;
    }

    public string receiveDataFromServer(TcpClient client){

    // Buffer to store the response bytes.
    data = new Byte[256];

    // String to store the response ASCII representation.
    String responseData = String.Empty;
    NetworkStream stream = client.GetStream();
    // Read the first batch of the TcpServer response bytes.
    Int32 bytes = stream.Read(data, 0, data.Length);
    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
    return responseData;
    }

#endif


    public byte[] AddByteToBeginningArray(byte[] bArray, byte newByte)
    {
        byte[] newArray = new byte[bArray.Length + 1];
        bArray.CopyTo(newArray, 1);
        newArray[0] = newByte;
        return newArray;

    }

    
    void Update()
    {
#if !UNITY_EDITOR
        //attemptReceiveSpatialAnchor();
#else

    
#endif
    }
}
