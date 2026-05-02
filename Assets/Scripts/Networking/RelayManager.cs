using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
  public static RelayManager Instance { get; private set; }

  private void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
  }

  // Called by host: creates a relay slot for up to maxPlayers-1 clients
  public async Task<string> CreateRelay(int maxPlayers)
  {
    try
    {
      Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
      string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

      // Tell NGO's transport to use this relay allocation
      var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
      var serverData = BuildRelayServerDataFromAllocation(allocation, "dtls");
      transport.SetRelayServerData(serverData);

      Debug.Log($"Relay created. Join code: {joinCode}");
      return joinCode;
    }
    catch (RelayServiceException e)
    {
      Debug.LogError($"Relay creation failed: {e}");
      return null;
    }
  }

  // Called by client: joins the relay using the host's join code
  public async Task JoinRelay(string joinCode)
  {
    try
    {
      JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

      var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
      var serverData = BuildRelayServerDataFromJoinAllocation(joinAllocation, "dtls");
      transport.SetRelayServerData(serverData);

      Debug.Log($"Relay joined with code: {joinCode}");
    }
    catch (RelayServiceException e)
    {
      Debug.LogError($"Relay join failed: {e}");
    }
    }

    // Helpers to build RelayServerData via host/port constructor
    static Unity.Networking.Transport.Relay.RelayServerData BuildRelayServerDataFromAllocation(Unity.Services.Relay.Models.Allocation allocation, string connectionType)
    {
      string host = null;
      ushort port = 0;
      bool isSecure = false;
      bool isWebSocket = connectionType == "ws" || connectionType == "wss";

      foreach (var endpoint in allocation.ServerEndpoints)
      {
        if (endpoint.ConnectionType == connectionType)
        {
          host = endpoint.Host;
          port = (ushort)endpoint.Port;
          isSecure = endpoint.Secure;
          break;
        }
      }

      if (host == null)
        throw new System.ArgumentException($"No server endpoint with connection type {connectionType} found on allocation");

      return new Unity.Networking.Transport.Relay.RelayServerData(host,
        port,
        allocation.AllocationIdBytes,
        allocation.ConnectionData,
        allocation.ConnectionData,
        allocation.Key,
        isSecure,
        isWebSocket);
    }

    static Unity.Networking.Transport.Relay.RelayServerData BuildRelayServerDataFromJoinAllocation(Unity.Services.Relay.Models.JoinAllocation allocation, string connectionType)
    {
      string host = null;
      ushort port = 0;
      bool isSecure = false;
      bool isWebSocket = connectionType == "ws" || connectionType == "wss";

      foreach (var endpoint in allocation.ServerEndpoints)
      {
        if (endpoint.ConnectionType == connectionType)
        {
          host = endpoint.Host;
          port = (ushort)endpoint.Port;
          isSecure = endpoint.Secure;
          break;
        }
      }

      if (host == null)
        throw new System.ArgumentException($"No server endpoint with connection type {connectionType} found on join allocation");

      return new Unity.Networking.Transport.Relay.RelayServerData(host,
        port,
        allocation.AllocationIdBytes,
        allocation.ConnectionData,
        allocation.HostConnectionData,
        allocation.Key,
        isSecure,
        isWebSocket);
    }
  }