using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    public static readonly List<Coin> All = new List<Coin>();

    public Transform Visual;
    [HideInInspector] public bool Taken;

    public override void Spawned()
    {
        Taken = false;
        All.Add(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    void Update()
    {
        if (Visual != null)
            Visual.Rotate(Vector3.up, 180f * Time.deltaTime, Space.World);
    }
}
