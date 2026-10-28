using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

// Cria/entra em sessoes, faz o spawn dos jogadores e envia o input para a rede.
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkObject PlayerPrefab;
    public NetworkObject GameManagerPrefab;
    public GameObject MenuPanel;
    public InputField RoomInput;
    public Button JoinButton;
    public Text StatusText;

    GameObject _runnerGo;
    NetworkRunner _runner;
    bool _busy;
    bool _gmSpawned;
    int _spawnCounter;
    readonly Dictionary<PlayerRef, NetworkObject> _players = new Dictionary<PlayerRef, NetworkObject>();

    void Awake()
    {
        Application.runInBackground = true;
        if (JoinButton != null) JoinButton.onClick.AddListener(JoinRoom);
    }

    async void JoinRoom()
    {
        if (_busy) return;
        _busy = true;

        string room = (RoomInput == null || string.IsNullOrWhiteSpace(RoomInput.text))
            ? "Sala1" : RoomInput.text.Trim();
        StatusText.text = "Conectando a sala '" + room + "'...";

        _runnerGo = new GameObject("NetworkRunner");
        _runner = _runnerGo.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        var sceneManager = _runnerGo.AddComponent<NetworkSceneManagerDefault>();

        _gmSpawned = false;
        _spawnCounter = 0;
        _players.Clear();

        var result = await _runner.StartGame(new StartGameArgs
        {
            // Se a sala nao existe, vira Host; se existe, entra como Client.
            GameMode = GameMode.AutoHostOrClient,
            SessionName = room,
            SceneManager = sceneManager,
            PlayerCount = 4
        });

        if (result.Ok)
        {
            MenuPanel.SetActive(false);
        }
        else
        {
            StatusText.text = "Falha: " + result.ShutdownReason;
            Destroy(_runnerGo);
            _busy = false;
        }
    }

    // ---------- Callbacks ----------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return; // so o host faz spawn

        if (!_gmSpawned)
        {
            _gmSpawned = true;
            runner.Spawn(GameManagerPrefab, Vector3.zero, Quaternion.identity);
        }

        Vector3 pos = Quaternion.Euler(0f, _spawnCounter * 90f, 0f) * Vector3.forward * 8f;
        pos.y = 1f;
        _spawnCounter++;

        NetworkObject obj = runner.Spawn(PlayerPrefab, pos, Quaternion.identity, player);
        _players[player] = obj;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_players.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _players.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        Vector2 move = Vector2.zero;
        bool shoot = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            shoot = kb.spaceKey.isPressed;
        }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) shoot = true;
#else
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        shoot = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
#endif

        data.Move = move;
        data.Shoot = shoot;
        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _players.Clear();
        if (_runnerGo != null) Destroy(_runnerGo, 0.5f);
        _busy = false;
        if (MenuPanel != null) MenuPanel.SetActive(true);
        if (StatusText != null) StatusText.text = "Desconectado: " + shutdownReason;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
