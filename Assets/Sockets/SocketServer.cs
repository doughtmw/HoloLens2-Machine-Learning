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

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Perception.Spatial;

#else
using System.Net.Sockets;
using System.Threading.Tasks;
#endif


[RequireComponent(typeof(ARAnchorManager))]
public class SocketServer : MonoBehaviour
{
    public String _input = "Waiting";
    private StreamWriter writer;
    private StreamReader reader;
    TrackableId myTrackableId;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool localAnchorAdded = false;
    bool localBatchReady = false;
    bool batchSentToClients = false;


#if !UNITY_EDITOR
    StreamSocketListener listener;
    String port;
    String message;
    Byte[] data;
#else
    TcpListener server=null;
    bool _Active;
    System.Net.Sockets.NetworkStream stream;
    TcpClient client = null;
#endif

    // Use this for initialization
    void Start()
    {

        

#if !UNITY_EDITOR
            listener = new StreamSocketListener();
        port = "9999";
        listener.ConnectionReceived += Listener_ConnectionReceived;
        listener.Control.KeepAlive = false;

        Listener_Start();

#else

    try
    {
     // Set the TcpListener on port 13000.
     Int32 port = 9999;
     // TcpListener server = new TcpListener(port);
     server = new TcpListener(System.Net.IPAddress.Any, port);

     // Start listening for client requests.
     server.Start();
    }
    catch(SocketException e)
    {
      Debug.Log("SocketException: " + e);
    }

#endif


        var XRSubsystem = CreateXRSubsystem();
        if (XRSubsystem != null)
        {
            XRSubsystem.Start();
        }

        myTrackableId = this.GetComponent<ARAnchor>().trackableId;
        localAnchorAdded = tryAddLocalAnchor();
    }


#if !UNITY_EDITOR
    private async void Listener_Start()
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


    }

    private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {

        //None of the Debug Messages here will show on the main thread.
        _input = "Connection received";
        //Debug.Log("Connection received");
        Stream streamOut = args.Socket.OutputStream.AsStreamForWrite();
        writer = new StreamWriter(streamOut) { AutoFlush = true };

        //Stream streamIn = args.Socket.InputStream.AsStreamForRead();
        //reader = new StreamReader(streamIn);

        writer.Write("X\n");
        //Debug.Log("Sent data!");
        //string received = null;


        //received = reader.ReadLine();
        //_input= received;

    }

#else

    private void listenForClientConnection()
    {
    // Buffer for reading data
      Byte[] bytes = new Byte[256];
      String data = null;

            //Console.Write("Waiting for a connection... ");

            if (!server.Pending())
            {
                //Debug.Log("Sorry, no connection requests have arrived");
            }
            else
            {
            //Accept the pending client connection and return a TcpClient object initialized for communication.
            client = server.AcceptTcpClient();
            // Using the RemoteEndPoint property.
            Console.WriteLine("I am listening for connections on " +
            IPAddress.Parse(((IPEndPoint)server.LocalEndpoint).Address.ToString()) +
            "on port number " + ((IPEndPoint)server.LocalEndpoint).Port.ToString());

            data = null;

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while((i = stream.Read(bytes, 0, bytes.Length))!=0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", data);

                // Process the data sent by the client.
                data = data.ToUpper();

                byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Debug.Log("Sent: " + data);
            }

            }
    }
#endif

    void Update()
    {
#if !UNITY_EDITOR
        //Debug.Log(_input);
#else
    listenForClientConnection();

#endif
    }

    public void StopExchange()
    {

#if UNITY_EDITOR

    try
    {
        stream.Close();
    }
    catch{}

    try
    {
         client.Close();
    }
    catch{}
    try
    {
         writer.Close();
    }
    catch{} 
    try
    {
        reader.Close();
    }
    catch{} 


     stream = null;


#else

        listener.Dispose();
        writer.Dispose();
        reader.Dispose();

            listener = null;

#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

    bool tryAddLocalAnchor()
    {
        myTrackableId = this.GetComponent<ARAnchor>().trackableId;
        return myAnchorTransferBatch.AddAnchor(myTrackableId, "HostPosition");
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
                Debug.Log("We got here!");
                // Create this plane subsystem
                return descriptor.Create();
            }
        }


        return null;
    }
}
