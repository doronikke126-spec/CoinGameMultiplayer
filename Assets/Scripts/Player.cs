using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public static readonly List<Player> All = new List<Player>();

    [Header("Configuracao")]
    public float MoveSpeed = 7f;
    public float ArenaLimit = 14f;
    public float ShootCooldownSeconds = 0.35f;
    public float StunSeconds = 1.5f;
    public float CoinPickupRadius = 1.3f;
    public NetworkObject BulletPrefab;
    public Renderer BodyRenderer;

    // Estado sincronizado pela rede
    [Networked] public int Score { get; set; }
    [Networked] public NetworkString<_16> Nick { get; set; }
    [Networked] public TickTimer StunTimer { get; set; }
    [Networked] private TickTimer ShootCooldown { get; set; }

    static readonly Color[] Palette =
    {
        new Color(0.25f, 0.6f, 1f),
        new Color(1f, 0.35f, 0.35f),
        new Color(0.3f, 0.9f, 0.4f),
        new Color(0.9f, 0.5f, 1f)
    };

    Material _mat;
    public Color BaseColor { get; private set; } = Color.white;

    public override void Spawned()
    {
        All.Add(this);
        int id = Object.InputAuthority.PlayerId;
        BaseColor = Palette[Mathf.Abs(id) % Palette.Length];
        if (BodyRenderer != null)
        {
            _mat = BodyRenderer.material;
            _mat.color = BaseColor;
        }
        if (Object.HasStateAuthority)
            Nick = "Jogador " + id;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        // Logica de jogo roda apenas no host (autoridade de estado)
        if (Object.HasStateAuthority)
            CollectCoins();

        if (!GetInput(out NetworkInputData input)) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State == GameState.Finished) return;
        if (!StunTimer.ExpiredOrNotRunning(Runner)) return; // atordoado nao se move

        Vector3 dir = new Vector3(input.Move.x, 0f, input.Move.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (dir.sqrMagnitude > 0.01f)
        {
            transform.position += dir * MoveSpeed * Runner.DeltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -ArenaLimit, ArenaLimit);
        p.z = Mathf.Clamp(p.z, -ArenaLimit, ArenaLimit);
        transform.position = p;

        if (input.Shoot && gm.State == GameState.Playing &&
            Object.HasStateAuthority && ShootCooldown.ExpiredOrNotRunning(Runner))
        {
            ShootCooldown = TickTimer.CreateFromSeconds(Runner, ShootCooldownSeconds);
            Runner.Spawn(BulletPrefab,
                transform.position + transform.forward * 1.1f,
                transform.rotation,
                Object.InputAuthority);
        }
    }

    void CollectCoins()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        for (int i = Coin.All.Count - 1; i >= 0; i--)
        {
            if (i >= Coin.All.Count) continue;
            var c = Coin.All[i];
            if (c == null || c.Taken) continue;

            Vector3 d = c.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < CoinPickupRadius * CoinPickupRadius)
            {
                c.Taken = true;
                Score += 1;
                Runner.Despawn(c.Object);
            }
        }
    }

    // Chamado pelo host quando uma bala acerta este jogador
    public void Stun()
    {
        StunTimer = TickTimer.CreateFromSeconds(Runner, StunSeconds);
    }

    public bool IsStunned => !StunTimer.ExpiredOrNotRunning(Runner);

    public override void Render()
    {
        if (_mat == null) return;
        _mat.color = IsStunned ? Color.gray : BaseColor;
    }
}
