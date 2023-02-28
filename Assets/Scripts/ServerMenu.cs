using NobleConnect.NetCodeForGameObjects;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;

public class ServerMenu : MonoBehaviour
{
    public void HostGame () {
        NetworkManager.Singleton.StartHost();
    }

    public void JoinGame () {
        NetworkManager.Singleton.StartClient();
    }
}
