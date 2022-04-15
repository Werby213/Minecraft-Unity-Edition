﻿using System;
using System.Collections;
using System.IO;
using Mirror;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;

public class WorldServer : NetworkBehaviour
{
    public static WorldServer instance;

    private void Start()
    {
        instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<SetBlockMessage>(SetBlockMessageHandler, false);
        NetworkServer.RegisterHandler<SavePlayerMessage>(SavePlayerMessageHandler, false);
        NetworkServer.RegisterHandler<SpawnPlayerMessage>(SpawnPlayerMessageHandler, false);
        
        StartCoroutine(SaveLoop());
    }

    public void SetBlockMessageHandler(NetworkConnectionToClient conn, SetBlockMessage message)
    {
        RpcSetBlock(message.position, message.blockType);
    }

    [ClientRpc]
    public void RpcSetBlock(Vector3Int position, BlockType blockType)
    {
        World.Instance.SetBlock(position, blockType);
    }
    
    public void SavePlayerMessageHandler(NetworkConnectionToClient conn, SavePlayerMessage message)
    {
        World.Instance.SavePlayer(message);
    }
    
    public IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);
            World.Instance.SaveWorld();
        }
    }
    
    public void SpawnPlayerMessageHandler(NetworkConnectionToClient conn, SpawnPlayerMessage message)
    {
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")))
        {
            var playerData =  JsonUtility.FromJson<WorldServer.SavePlayerMessage>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                ".minecraftUnity/saves/" + World.Instance.worldName + $"/playerdata/{message.steamId}.json")));
            
            var player = Instantiate(NetworkManager.singleton.playerPrefab, playerData.position, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
            player.GetComponent<Player>().RpcLoadPlayer(conn, playerData);
            conn.identity.AssignClientAuthority(conn);
        }
        else
        {
            var world = World.Instance;
            for (var i = world.worldHeight - 1; i >= 0; i--)
            {
                if (world.GetBlock(new Vector3Int(0, i, 0)).type != BlockType.Air)
                {
                    var player = Instantiate(NetworkManager.singleton.playerPrefab, new Vector3(world.chunkSize/2, i + 1, world.chunkSize/2), Quaternion.identity);
                    NetworkServer.AddPlayerForConnection(conn, player);
                    conn.identity.AssignClientAuthority(conn);
                    break;
                }
            }
        }
    }


    public struct SetBlockMessage : NetworkMessage
    {
        public Vector3Int position;
        public BlockType blockType;
        
        public SetBlockMessage(Vector3Int position, BlockType blockType)
        {
            this.position = position;
            this.blockType = blockType;
        }
    }
    
    [Serializable]
    public struct SavePlayerMessage : NetworkMessage
    {
        public Vector3 position;
        public Quaternion bodyRotation;
        public Quaternion headRotation;
        public PlayerInventory.Slot[] inventory;
        public ulong steamId;

        public SavePlayerMessage(Vector3 position, Quaternion bodyRotation, Quaternion headRotation, PlayerInventory.Slot[] inventory, ulong steamId)
        {
            this.position = position;
            this.bodyRotation = bodyRotation;
            this.headRotation = headRotation;
            this.inventory = inventory;
            this.steamId = steamId;
        }
    }
    
    
    public struct SpawnPlayerMessage : NetworkMessage
    {
        public SteamId steamId;
        
        public SpawnPlayerMessage(SteamId steamId)
        {
            this.steamId = steamId;
        }
    }
}