# README
[
[**Website**](https://noblewhale.com)
|
[**Dashboard**](https://noblewhale.com/dashboard)
|
[**Docs**](https://noblewhale.com/docs)
|
[**FAQ**](https://noblewhale.com/faq)
|
[**Asset Store**](https://assetstore.unity.com/packages/tools/network/noble-connect-140535)
]

Adds relays and punchthrough to UNet or Mirror.

Guarantees your players can connect while reducing latency and saving you money by connecting players directly whenever possible.
Your players won't need to worry about forwarding ports or fiddling with router settings, they can just sit back, relax, and enjoy your beautiful game.

Supports Windows, Linux, OSX, Android, and iOS.
Supports:
 * UNet
 * Mirror with KCP, LiteNetLib, or Ignorance transports
 * NetCode for GameObjects with UnityTransport

*Note: Web builds are not supported.*

# How to Use
In order to use the Noble Connect relay and punchthrough services you will need to sign up for an account. You can do this on our 
website or through the Unity Engine at Window->Noble Connect->Setup. It is free to sign up but your CCU and bandwidth will be limited. 
In order to raise the limits you will either need to purchase the Starter Pack or one of the monthly plans.

## Step 1 - Set up
	1. You can access the setup window at any time by going to Window->Noble Connect->Setup.
	2. Enter your email address to sign up, or enter your Game ID if you already have an account.
		* You can get your Game ID any time from the dashboard at https://noblewhale.com/dashboard
	3. Import the package for your networking system:
		* If you are using Mirror, import the "Mirror Noble Connect.unitypackage"
			* Make sure you have the correct version of Mirror imported first. Usually this means the latest from the Asset Store, but you can check the description on the [Noble Connect](https://assetstore.unity.com/packages/tools/network/noble-connect-140535) page to confirm.
		* If you are using UNet, import the "UNet Noble Connect.unitypackage"
			* In 2019.1 or later you must first install the "Multiplayer HLAPI" package from the Unity Package Manager
		* If you are using NetCode for Gameobjects, import the "NetCode for GameObjects Noble Connect.unitypackage"

## Step 2 - Test it
	1. Add the "Noble Connect/[UNet or Mirror or Netcode for GameObjects]/Examples/Network Manager/Network Manager Example.unity" scene to the build settings.
	2. Build for your desired platform and run the build.
	3. Click "Host" in the build.
		* Note the IP and port that are displayed, this is the address that clients will use to connect to the host.
	4. Run the Network Manager Example scene in the editor.
	5. Click "Client" and then enter the ip and port from the host.
	6. Click "Connect" to connect to the host.
		* When the connection is complete you will see the connection type displayed on the client.

# Examples
Check out the Example scenes and scripts to see common ways to get connected. Each example includes a README file with more detailed instructions.
If you need any more information don't hesitate to contact us at nobleconnect@noblewhale.com

# What next?
Generally you should extend from the provided NobleNetworkManager.
If you prefer something a little lower level, you can also use the NobleServer and NobleClient classes directly to Listen() and Connect().
Most things will work exactly the same as you are used to if you are familiar with UNet, Mirror, or Netcode for GameObjects

**Note: For most methods that you override in NobleNetworkManager you will want to make sure to call the base method to avoid causing unexpected behaviour.**

# Notes for UNet / Mirror
The main difference is that you will use the NobleNetworkManager instead of Unity or Mirror's NetworkManager, or the NobleServer and NobleClient instead of Unity's or Mirror's NetworkServer and NetworkClient.

# Note for Netcode for GameObjects
Just make sure to use the NobleUnityTransport and you should be good to go.

# General Notes
The host will not know the address that clients should connect to until it has been assigned by the Noble Connect servers. 
You will need to override the OnServerPrepared() method or use the OnServerPreparedCallback to know when this has happened and to get the hostAddress and hostPort (collectively known as the HostEndPoint) 
that clients should use to connect to the host. This is generally when you would create a match if you're using a matchmaking system. You can also get 
the HostEndPoint address any time after it has been assigned via NobleServer.HostEndPoint or NobleNetworkManager.HostEndPoint or NobleUnityTransport.HostRelayEndPoint

# Regions
By default the closest region will be selected automatically for relays. You can also manually select the region on the NobleNetworkManager or by passing in a GeographicRegion at runtime.
You can see this in any of the example scenes.

We have servers in the following regions:
	* US_EAST - New York
	* US_WEST - California
	* EUROPE - Amsterdam
	* AUSTRALIA - Sydney
	* HONG_KONG - Hong Kong
	* ASIA_PACIFIC - Singapore
	* SOUTH_AMERICA - Brazil
	* CHINE - China

# How it Works
Punchthrough and relays work according to the [ICE](https://tools.ietf.org/html/rfc5245), [TURN](https://tools.ietf.org/html/rfc5766), and [STUN](https://tools.ietf.org/html/rfc5389) specifications.