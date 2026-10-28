using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float Speed = 16f;
    public float Lifetime = 2f;
    public float HitRadius = 1f;
    public int PointsPerHit = 2;

    [Networked] private TickTimer Life { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Life = TickTimer.CreateFromSeconds(Runner, Lifetime);
    }

    public override void FixedUpdateNetwork()
    {
        // Apenas o host simula a bala; os clientes recebem a posicao via NetworkTransform
        if (!Object.HasStateAuthority) return;

        if (Life.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        transform.position += transform.forward * Speed * Runner.DeltaTime;

        for (int i = 0; i < Player.All.Count; i++)
        {
            Player target = Player.All[i];
            if (target == null || !target.Object.IsValid) continue;
            if (target.Object.InputAuthority == Object.InputAuthority) continue; // nao acerta o dono
            if (target.IsStunned) continue;

            Vector3 d = target.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= HitRadius * HitRadius)
            {
                target.Stun();

                for (int j = 0; j < Player.All.Count; j++)
                {
                    if (Player.All[j].Object.InputAuthority == Object.InputAuthority)
                    {
                        Player.All[j].Score += PointsPerHit;
                        break;
                    }
                }

                Runner.Despawn(Object);
                return;
            }
        }
    }
}
