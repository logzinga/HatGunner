using NobleConnect.NetCodeForGameObjects;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;

namespace NobleConnect.Examples.NetCodeForGameObjects
{
    public class ExampleNetCodeNetworkHUD : MonoBehaviour
    {
        public TMPro.TMP_InputField hostAddressField;
        public TMPro.TMP_InputField hostPortField;
        public TMPro.TMP_InputField hostAddressText;
        public TMPro.TMP_InputField hostPortText;
        public TMPro.TMP_Text connectionStatusText;

        public GameObject startPanel;
        public GameObject clientPanel;
        public GameObject clientConnectedPanel;
        public GameObject hostPanel;

        NobleUnityTransport transport;

        private void Start()
        {
            transport = (NobleUnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            transport.OnServerPreparedCallback += OnServerPrepared;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (transport)
            {
                transport.OnServerPreparedCallback -= OnServerPrepared;
            }
        }

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();

            startPanel.SetActive(false);
            hostPanel.SetActive(true);

            hostAddressText.text = "Initializing..";
            hostPortText.text = "Initializing..";
        }

        private void OnServerPrepared(string relayAddress, ushort relayPort)
        {
            hostAddressText.text = relayAddress;
            hostPortText.text = relayPort.ToString();
        }

        public void ShowClientPanel()
        {
            startPanel.SetActive(false);
            clientPanel.SetActive(true);


            hostAddressField.text = "";
            hostPortField.text = "";
            connectionStatusText.text = "";
        }

        public void StartClient()
        {
            transport.ConnectionData.Address = hostAddressField.text;
            transport.ConnectionData.Port = ushort.Parse(hostPortField.text);

            NetworkManager.Singleton.StartClient();

            clientPanel.SetActive(false);
            clientConnectedPanel.SetActive(true);

            connectionStatusText.text = "Connecting...";
        }

        private void OnClientConnected(ulong clientID)
        {
            connectionStatusText.text = "Connected via " + transport.ConnectionType.ToString();
        }

        private void OnClientDisconnected(ulong obj)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                clientConnectedPanel.SetActive(false);
                startPanel.SetActive(true);
            }
        }

        public void StopHost()
        {
            NetworkManager.Singleton.Shutdown();

            hostPanel.SetActive(false);
            startPanel.SetActive(true);
        }

        public void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();

            clientConnectedPanel.SetActive(false);
            startPanel.SetActive(true);
        }
    }
}