using Steamworks;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;


namespace DestinyCustomBlocks
{
    // Base class for custom block types' network behaviour.
    public class CustomBlock_Network : MonoBehaviour_Network
    {
        public virtual void OnBlockPlaced()
        {
            NetworkIDManager.AddNetworkID(this);
        }

        protected override void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this);
            base.OnDestroy();
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1 && remoteID != ComponentManager<Raft_Network>.Value.LocalSteamID)
            {
                if (!CustomBlocks.IgnoreFlagMessages && !(Raft_Network.IsHost && CustomBlocks.PreventChanges))
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        var block = this.GetComponent<ICustomBlock>();
                        if (block != null)
                        {
                            // This is how we handle resending to everyone else if
                            // the user is the host. Otherwise, we don't send
                            // anything for the update.
                            block.SetSendUpdates(Raft_Network.IsHost);
                            block.SetImageData(data);
                            block.SetSendUpdates(true);
                        }
                    }
                }
                return false;
            }
            return base.Deserialize(msg, remoteID);
        }

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomBlocks.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), this, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }

        /*
        private IEnumerator SendMessage(Message_Animal_AnimTriggers msg)
        {
            Packet_Single packet = new PacketSingle(EP2PSend.k_EP2PSendReliable, msg);
            BinaryFormatter bin = new BinaryFormatter();
            MemoryStream mem = new MemorySteam();
            bin.Serialize(mem, packet);

            Raft_Network.Message_FragmentedPacket[] messages;
        }
        */
    }



    public class CustomSail_Network : Sail//, IRaycastable
    {
        public virtual void OnBlockPlaced()
        {
            // Need to access the lower sail OnBlockPlace, but it is private.
            typeof(Sail).GetMethod("OnBlockPlaced", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
            NetworkIDManager.AddNetworkID(this);
        }

        protected override void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this);
            base.OnDestroy();
        }

        public override RGD Serialize_Save()
        {
            // Override so that the block class handles the save.
            return null;
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1 && remoteID != ComponentManager<Raft_Network>.Value.LocalSteamID)
            {
                if (!CustomBlocks.IgnoreFlagMessages && !(Raft_Network.IsHost && CustomBlocks.PreventChanges))
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        var block = this.GetComponent<ICustomBlock>();
                        if (block != null)
                        {
                            // This is how we handle resending to everyone else if
                            // the user is the host. Otherwise, we don't send
                            // anything for the update.
                            block.SetSendUpdates(Raft_Network.IsHost);
                            block.SetImageData(data);
                            block.SetSendUpdates(true);
                        }
                    }
                }
                return false;
            }
            return base.Deserialize(msg, remoteID);
        }

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomBlocks.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), this, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }
    }



    public class CustomInteractableOC_Network : Placeable_Interactable_OpenClose
    {
        protected override void BlockPlaced()
        {
            base.BlockPlaced();
            var block = this.GetComponent<Block_CustomBlock_Interactable>();
            this.GetComponentInChildren<RaycastInteractable>()?.AddRaycastables(new IRaycastable[] { block });
        }

        public override RGD Serialize_Save()
        {
            // Override so that the block class handles the save.
            return null;
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1 && remoteID != ComponentManager<Raft_Network>.Value.LocalSteamID)
            {
                if (!CustomBlocks.IgnoreFlagMessages && !(Raft_Network.IsHost && CustomBlocks.PreventChanges))
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        var block = this.GetComponent<ICustomBlock>();
                        if (block != null)
                        {
                            // This is how we handle resending to everyone else if
                            // the user is the host. Otherwise, we don't send
                            // anything for the update.
                            block.SetSendUpdates(Raft_Network.IsHost);
                            block.SetImageData(data);
                            block.SetSendUpdates(true);
                        }
                    }
                }
                return false;
            }
            return base.Deserialize(msg, remoteID);
        }

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomBlocks.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }
    }
}
