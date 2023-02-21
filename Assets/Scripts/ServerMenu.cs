using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ServerMenu : MonoBehaviour
{
    public void HostGame () {
        NetworkManager.Singleton.StartHost();
    }

    public void JoinGame () {
        NetworkManager.Singleton.StartClient();
    }
}
