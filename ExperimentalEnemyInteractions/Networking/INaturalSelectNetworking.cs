using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using GameNetcodeStuff;
using BepInEx;
using Unity;

namespace NaturalSelection.Networking;

public interface INaturalSelectNetworking
{
    EnemyAI OwningEnemy();
    public delegate void NaturalNetworkDelegate();
}
