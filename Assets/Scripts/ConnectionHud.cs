using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;
using TMPro;
using UnityEngine.SceneManagement;
using Microsoft.MixedReality.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

/// <summary>
///     Handles the creation of the Host or connection of the client, and changes schene when host is ready.
/// </summary>
/// 
public class ConnectionHud : MonoBehaviour
{

    public GameObject startClientButton;
    public GameObject startHostButton;
    public GameObject serverPrefab;
    public GameObject clientPrefab;

    void Start()
    {
        //CoreServices.DiagnosticsSystem.ShowDiagnostics = false;
    }

    void Update()
    {

    }


    public void serverButtonPressed()
    {
            var serverInstance = Instantiate(serverPrefab);
    }


    public void clientButtonPressed()
    {
        var clientInstance = Instantiate(clientPrefab);

    }




}