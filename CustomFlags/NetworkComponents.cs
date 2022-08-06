using Steamworks;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;


namespace DestinyCustomFlags
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
            CustomFlags.DebugLog($"Received Message: {msg}");
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1)
            {
                if (!CustomFlags.IgnoreFlagMessages && !(Raft_Network.IsHost && CustomFlags.PreventChanges))
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        this.GetComponent<Block_CustomBlock_Base>().ImageData = data;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            return base.Deserialize(msg, remoteID);
        }

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomFlags.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.All, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }
    }



    public class CustomSail_Network : Sail, IRaycastable
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
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1)
            {
                if (!CustomFlags.IgnoreFlagMessages)
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        this.GetComponent<Block_CustomBlock_Base>().ImageData = data;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            return base.Deserialize(msg, remoteID);
        }

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomFlags.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.All, EP2PSend.k_EP2PSendReliable, (NetworkChannel)17732);
            }
        }
    }
}
