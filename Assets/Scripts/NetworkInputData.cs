using Fusion;
using UnityEngine;

// Dados de input enviados por cada jogador a cada tick de rede.
public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkBool Shoot;
}
