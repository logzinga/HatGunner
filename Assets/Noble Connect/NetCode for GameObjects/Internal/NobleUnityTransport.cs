using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using NobleConnect.Ice;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Reflection;
using Unity.Networking.Transport;
using NetworkEvent = Unity.Netcode.NetworkEvent;
using System.Collections;

namespace NobleConnect.NetCodeForGameObjects
{
    /// <summary>Extends UnityTransport to use Noble Connect for punchthrough and relays</summary>
    public class NobleUnityTransport : UnityTransport
    {
        /// <summary>Some useful configuration settings like geographic region and timeouts.</summary>
        [Header("Noble Connect Settings")]
        public Config Config;

        /// <summary>You can enable this to force relay connections to be used for testing purposes.</summary>
        /// <remarks>
        /// Disables punchthrough and direct connections. Forces connections to use the relays.
        /// This is useful if you want to test your game with the unavoidable latency that is 
        /// introduced when the relay servers are used.
        /// </remarks>
        public bool ForceRelayOnly { get => Config.ForceRelayOnly; set => Config.ForceRelayOnly = value; }

        /// <summary>This is the address that clients should connect to. It is assigned by the relay server.</summary>
        /// <remarks>
        /// Note that this is not the host's actual IP address, but one assigned to the host by the relay server.
        /// When clients connect to this address, Noble Connect will find the best possible connection and use it.
        /// This means that the client may actually end up connecting to an address on the local network, or an address
        /// on the router, or an address on the relay. But you don't need to worry about any of that, it is all
        /// handled for you internally.
        /// </remarks>
        public IPEndPoint HostRelayEndPoint;

        /// <summary>You can check this on the client after they connect, it will either be Direct, Punchthrough, or Relay.</summary>
        public ConnectionType LatestConnectionType
        {
            get
            {
                if (Peer != null) return Peer.latestConnectionType;
                else return ConnectionType.NONE;
            }
        }

        /// <summary>Use this callback to be informed when something goes horribly wrong.</summary>
        /// <remarks>
        /// You should see an error in your console with more info any time this is called. Generally
        /// it will either mean you've completely lost connection to the relay server or you
        /// have exceeded your CCU or bandwidth limit.
        /// </remarks>
        public event Action<string> OnFatalErrorCallback;

        /// <summary>Use this callback to know when a Server has received their HostRelayEndPoint and is ready to receive incoming connections.</summary>
        /// <remarks>
        /// If you are using some sort matchmaking this is a good time to create a match now that you have the HostRelayEndPoint that clients will need to connect to.
        /// </remarks>
        /// <param name="hostAddress">The address of the HostRelayEndPoint the clients should use when connecting to the host.</param>
        /// <param name="hostPort">The port of the HostRelayEndPoint that clients should use when connecting to the host</param>
        public event Action<string, ushort> OnServerPreparedCallback;

        public ConnectionType ConnectionType => Peer.latestConnectionType;

        /// <summary>Keeps track of which end point each connection belongs to so that when they disconnect we can clean up.</summary>
        Dictionary<ulong, IPEndPoint> EndPointByConnection = new Dictionary<ulong, IPEndPoint>();

        /// <summary>Represents a peer (client or server) in Noble Connect. Handles creating and destroying connection routes.</summary>
        /// <remarks>
        /// This is the interface to the relay and punchthrough services. 
        /// It is used to find the best route to connect and to clean up when a client disconnects.
        /// </remarks>
        Peer Peer;

        /// <summary>This delegate allows us to call the private base.Update method</summary>
        Action BaseUpdateDelegate;

        /// <summary>This delegate allows us to call the private ParseClientId method</summary>
        Func<ulong, NetworkConnection> ParseClientIdDelegate;

        /// <summary>Used to get the remote address of connecting clients</summary>
        FieldInfo DriverField;

        private void Awake()
        {
            // Set up logging using the LogLevel from the NetworkManager
            Logger.logger = Debug.Log;
            Logger.warnLogger = Debug.LogWarning;
            Logger.errorLogger = Debug.LogError;
            switch (NetworkManager.Singleton.LogLevel)
            {
                case LogLevel.Developer: Logger.logLevel = Logger.Level.Developer; break;
                case LogLevel.Error: Logger.logLevel = Logger.Level.Error; break;
                case LogLevel.Normal: Logger.logLevel = Logger.Level.Info; break;
                case LogLevel.Nothing: Logger.logLevel = Logger.Level.Fatal; break;
            }

            // The base update method is inaccessible but we need it to be called, so use reflection
            var baseUpdateMethod = typeof(UnityTransport).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            // Creating a delegate allows for faster method calls than Invoke, and we call Update a lot, so let's do that
            BaseUpdateDelegate = (Action)Delegate.CreateDelegate(typeof(Action), this, baseUpdateMethod);

            // We need access to the private m_Driver field in order to get the remote address of clients
            DriverField = typeof(UnityTransport).GetField("m_Driver", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // We need this private method to convert the ulong network id into a NetworkConnection
            MethodInfo ParseClientIdMethod = typeof(UnityTransport).GetMethod("ParseClientId", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(ulong) }, null);
            ParseClientIdDelegate = (Func<ulong, NetworkConnection>)Delegate.CreateDelegate(typeof(Func<ulong, NetworkConnection>), ParseClientIdMethod);

            // Set up the callbacks we need
            OnFatalErrorCallback += OnFatalError;
            OnServerPreparedCallback += OnServerPrepared;
            Config.OnFatalError = OnFatalErrorCallback;

            // The Unity Transport apparently does not support ipv6, so disable it
            Config.EnableIPv6 = false;

            // Hook in to the transport level events so we can know when a client connects / disconnects
            OnTransportEvent += OnReceivedTransportEvent;
        }

        public override void Initialize(NetworkManager netMan)
        {
            // Initialize the Peer
            Peer = new Peer(Config.AsIceConfig());

            base.Initialize(netMan);
        }

        /// <summary>Start a server and allocate a relay.</summary>
        /// <remarks>
        /// When the server has received a relay address the OnServerPreparedCallback will be triggered.
        /// This callback is a good place to do things like create a match in your matchmaking system of choice,
        /// or anything else that you want to do as soon as you have the host's address.
        /// </remarks>
        public override bool StartServer()
        {
            bool success = base.StartServer();
            Peer.InitializeHosting(ConnectionData.Port, OnServerPreparedCallback);
            return success;
        }

        /// <summary>Start a client and connect to ConnectionData.Address at ConnectionData.Port</summary>
        /// <remarks>
        /// ConnectionData.Address and ConnectionData.Port should be set to a host's HostRelayEndPoint before calling this method.
        /// </remarks>
        public override bool StartClient()
        {
            Peer.InitializeClient(new IPEndPoint(IPAddress.Parse(ConnectionData.Address), ConnectionData.Port), OnClientPrepared);
            return true;
        }

        /// <summary>Shut down, disconnect, and clean up</summary>
        public override void Shutdown()
        {
            base.Shutdown();
            if (Peer != null)
            {
                Peer.CleanUpEverything();
                Peer.Dispose();
                Peer = null;
            }
        }

        /// <summary>Calls the Peer's Update() method to process messages. Also calls the base Update method via reflection</summary>
        void Update()
        {
            if (Peer != null)
            {
                Peer.Update();
            }

            // Equivalent to base.Update()
            BaseUpdateDelegate();
        }

        /// <summary>Transport level events are received here. Used to handle client connect / disconnect</summary>
        /// <param name="eventType">The type of NetworkEvent that occurred</param>
        /// <param name="clientId">The network id of the client that instigated the event</param>
        /// <param name="payload">Any payload related to the event</param>
        /// <param name="receiveTime">The time that the event was triggered</param>
        private void OnReceivedTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (eventType == NetworkEvent.Connect)
                {
                    OnIncomingClientConnection(clientId);
                }
                else if (eventType == NetworkEvent.Disconnect)
                {
                    OnServerLostClient(clientId);
                }
            }
        }

        /// <summary>Keep track of the incoming client and their associated end point</summary>
        /// <remarks>
        /// We need to know the disconnecting client's EndPoint in order to clean up properly when they disconnect.
        /// Ideally we would just look this up via the transport when the disconnect happens, but by the time we get the event the transport has already
        /// purged that info, so instead we store it ourselves on connect so we can look it up on disconnect.
        /// </remarks>
        /// <param name="clientId">The network Id of the disconnecting client</param>
        void OnIncomingClientConnection(ulong clientId)
        {
            var clientEndPoint = GetClientEndPoint(clientId);
            EndPointByConnection[clientId] = clientEndPoint;
        }

        /// <summary>Clean up resources associated with the disconnecting client</summary>
        /// <remarks>
        /// This uses the end point that was associated with the clientId in OnIncomingClientConnection
        /// </remarks>
        /// <param name="clientId">The network Id of the disconnecting client</param>
        void OnServerLostClient(ulong clientId)
        {
            if (EndPointByConnection.ContainsKey(clientId))
            {
                IPEndPoint endPoint = EndPointByConnection[clientId];
                Peer.EndSession(endPoint);
                EndPointByConnection.Remove(clientId);
            }
        }

        /// <summary>Called when the client has been allocated a relay. This is where the Transport level connection starts.</summary>
        /// <param name="bridgeEndPoint">The IPv4 EndPoint to connect to</param>
        /// <param name="bridgeEndPointIPv6">The IPv6 EndPoint to connect to. Not used here.</param>
        void OnClientPrepared(IPEndPoint bridgeEndPoint, IPEndPoint bridgeEndPointIPv6)
        {
            ConnectionData.Address = bridgeEndPoint.Address.ToString();
            ConnectionData.Port = (ushort)bridgeEndPoint.Port;

            StartCoroutine(ConnectEventually());
        }

        IEnumerator ConnectEventually()
        {
            yield return new WaitForSeconds(1);
            base.StartClient();
        }


        /// <summary>Called when the server has been allocated a relay and is ready to receive incoming connections</summary>
        /// <param name="address">The host relay address that clients should connect to</param>
        /// <param name="port">The host relay port that clients should connect to</param>
        void OnServerPrepared(string address, ushort port)
        {
            HostRelayEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
        }

        /// <summary>If anythin goes horribly wrong, stop hosting / disconnect.</summary>
        /// <param name="errorMessage">The error message from Noble Connect</param>
        void OnFatalError(string errorMessage)
        {
            NetworkManager.Singleton.Shutdown();
        }

        /// <summary>Get a client's end point from their network id</summary>
        /// <param name="clientId">The network id of the client</param>
        /// <returns>The IPEndPoint of the client</returns>
        IPEndPoint GetClientEndPoint(ulong clientId)
        {
            var driver = (NetworkDriver)DriverField.GetValue(this);

            var clientNetworkConnection = ParseClientIdDelegate(clientId);
            var remoteEndPoint = driver.RemoteEndPoint(clientNetworkConnection);

            var ip = new IPAddress(remoteEndPoint.GetRawAddressBytes().ToArray());
            var port = remoteEndPoint.Port;

            return new IPEndPoint(ip, port);
        }
    }
}