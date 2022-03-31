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
using Windows.Storage;
using Windows.System;
using Windows.Storage.Streams;

#else
using System.Net.Sockets;
using System.Threading.Tasks;

#endif




[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARAnchor))]
public class SocketServer : MonoBehaviour
{
    public String _input = "Waiting";
    private StreamWriter writer;
    private StreamReader reader;
    TrackableId myTrackableId;
    XRAnchorTransferBatch myAnchorTransferBatch = new XRAnchorTransferBatch();
    bool localAnchorAdded = false;
    bool localBatchReady = false;
    bool localAnchorStreamCreated = false;

    Stream tempStream;
    MemoryStream memoryStream = new MemoryStream();
    Int64 bytesSent;

#if !UNITY_EDITOR
    StreamSocketListener listener = new StreamSocketListener();
    String port;
    String message;
    Byte[] data;
 
    StreamSocket clientSocket = new StreamSocket();
#else
    TcpListener server=null;
    bool _Active;
    System.Net.Sockets.NetworkStream stream;
    TcpClient client = null;
#endif

    //define filePath
    #region Constants to modify
    private const string SessionFolderRoot = "SavedAnchorBatchs";
    #endregion

    #region private members
    private string m_sessionPath;
    private string m_filePath;
    #endregion

    // Use this for initialization
    async void Start()
    {



#if !UNITY_EDITOR
        
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

       tryAddLocalAnchor();

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
            clientSocket = args.Socket;
            //None of the Debug Messages here will show on the main thread.
            if (localAnchorStreamCreated)
            {
            trySendSpatialAnchorToClient(clientSocket);
            }
            else
            {
                Debug.Log("Connection Received, but no anchor batch ready to send");
                writer.Write("-1");
            }
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


        if (localBatchReady)
        {
            Debug.Log("Exported Anchor Batch in Socket Stream");
            Debug.Log("The size of the written stream is: " + tempStream.Length);
            localBatchReady = false;
        }


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

    async void tryAddLocalAnchor()
    {

        Debug.Log("Creating Export Anchor Batch in Socket Stream");

        while (tempStream == null)
        {
            myTrackableId = this.GetComponent<ARAnchor>().trackableId;
            myAnchorTransferBatch.AddAnchor(myTrackableId, "HostPosition");
            tempStream = await XRAnchorTransferBatch.ExportAsync(myAnchorTransferBatch);
        }

        //await tempStream.CopyToAsync(memoryStream);
        if (tempStream != null)
        {
            localAnchorStreamCreated = true;
            Debug.Log("Anchor Copied To Local Stream");
            //MakeNewSession();
            //CreateNewAnchorFile(tempStream);
            //Debug.Log("Local Stream Written To Disk");

        }

    }

    #if !UNITY_EDITOR
    async void trySendSpatialAnchorToClient(StreamSocket socket)
    {


        tempStream.Position = 0;
        
        //await tempStream.CopyToAsync(socket.OutputStream.AsStreamForWrite(16384));
        
        /* try
        {
            await socket.OutputStream.AsStreamForWrite().FlushAsync();
            Debug.Log("Anchor Sent to Client");
            localBatchReady = true;
        }
        catch (Exception exception)
        {
            throw exception;
        }

        socket.OutputStream.AsStreamForWrite().Close();*/

        await tempStream.CopyToAsync(socket.OutputStream.AsStreamForWrite());
        DataWriter dataWriter = new DataWriter(socket.OutputStream);
        await dataWriter.FlushAsync();

    }
#endif

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

    async void MakeNewSession()
    {
        string rootPath = "";
#if WINDOWS_UWP
            StorageFolder sessionParentFolder = await KnownFolders.PicturesLibrary
                .CreateFolderAsync(SessionFolderRoot,
                CreationCollisionOption.OpenIfExists);
            rootPath = sessionParentFolder.Path;
#else
        rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SessionFolderRoot);
        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
#endif
        Directory.CreateDirectory(rootPath);
        UnityEngine.Debug.Log("Writing anchor data to " + rootPath);
    }

    async void CreateNewAnchorFile(Stream tempStream)
    {
        string rootPath = "";
        var filename = "CurrentAnchorBatch.dat";
        m_filePath = Path.Combine(rootPath, filename);

        using (var fileWriter = new FileStream(m_filePath, FileMode.OpenOrCreate))
        {
            /*while (tempStream.Position < tempStream.Length)
            {
                byte[] byteArray = new byte[] { (byte)tempStream.ReadByte() };
                await fileWriter.WriteAsync(byteArray, 0, 1);
            }*/
            tempStream.Position = 0;
            await tempStream.CopyToAsync(fileWriter);
            fileWriter.Close();
            Debug.Log("Achor written to file");
        }

    }

}