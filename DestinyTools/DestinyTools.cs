using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;


public class DestinyTools : Mod
{
    private static Block startingBlock;
    private static Block endingBlock;
    private static Raft_Network network;

    public void Start()
    {
        DestinyTools.startingBlock = null;
        DestinyTools.endingBlock = null;
        DestinyTools.network = ComponentManager<Raft_Network>.Value;
        Debug.Log("DestinyTools has been loaded.");
    }

    public void OnModUnload()
    {
        DestinyTools.StopMeasurement();

        Debug.Log("DestinyTools has been unloaded.");
    }

    // Commands added.

    [ConsoleCommand(name: "fullRevive", docs: "Revives the local player with full stats.")]
    public static string FullRevive()
    {
        RAPI.GetLocalPlayer().PlayerScript.RespawnWithoutBed(false);
        RAPI.GetLocalPlayer().Stats.stat_health.SetToMaxValue();
        RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.SetToMaxValue();
        RAPI.GetLocalPlayer().Stats.stat_hunger.Bonus.SetToMaxValue();
        RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.SetToMaxValue();
        RAPI.GetLocalPlayer().Stats.stat_thirst.Bonus.SetToMaxValue();
        RAPI.GetLocalPlayer().Stats.stat_oxygen.SetToMaxValue();

        // The bonus bars won't properly show up, so we use these to force them.
        // There is probably a better way, which I'll look into later.
        RAPI.GetLocalPlayer().Stats.stat_hunger.Consume(1000, 1000, false);
        RAPI.GetLocalPlayer().Stats.stat_thirst.Consume(1000, 1000, false);

        DestinyTools.SendRevivePackets();

        return "Player fully revived.";
    }

    [ConsoleCommand(name: "revive", docs: "Revives the local player.")]
    public static string Revive()
    {
        // Really not a complicated command given the Raft code has something
        // for this.
        RAPI.GetLocalPlayer().PlayerScript.RespawnWithoutBed(false);

        DestinyTools.SendRevivePackets();
        return "Player revived.";
    }

    [ConsoleCommand(name: "unlockAllNotes", docs: "Unlocks all notes in the current save.")]
    public static string UnlockAllNotes()
    {
        ComponentManager<NoteBook>.Value.UnlockAllNotes();
        return "All notes unlocked.";
    }

    [ConsoleCommand(name: "selectMeasurementBlock", docs: "Selects the current block for the starting measurement.")]
    public static string SelectMeasurementBlock()
    {
        // If the ending block is set but the starting block is not, then the
        // starting block was destroyed. We want to quickly shift the ending
        // block to be the starting block.
        if (DestinyTools.endingBlock && !DestinyTools.startingBlock)
        {
            DestinyTools.startingBlock = DestinyTools.endingBlock;
            DestinyTools.endingBlock = null;
        }

        (bool, Block, Vector3) found = DestinyTools.GetBlockAtCursor();
        Debug.Log("[MeasuringTape] " + (found.Item1 ? $"Found block at {found.Item3}." : "No valid block found."));

        string message = "";

        if (found.Item1)
        {
            if (DestinyTools.startingBlock && DestinyTools.endingBlock)
            {
                // If both blocks exist, remove the outline for the original
                // starting block, move the current end block into the starting
                // block variable, set the selected block
                //
                // *Only* set the outline to false if another block is not this
                // one.
                if (DestinyTools.startingBlock != DestinyTools.endingBlock && DestinyTools.startingBlock != found.Item2)
                {
                    DestinyTools.startingBlock.SetInstanceOutline(false);
                }
                DestinyTools.startingBlock = DestinyTools.endingBlock;
                DestinyTools.endingBlock = found.Item2;
            }
            else if (DestinyTools.startingBlock)
            {
                DestinyTools.endingBlock = found.Item2;
            }
            else
            {
                DestinyTools.startingBlock = found.Item2;
            }

            // Outline the selected block.
            found.Item2.SetInstanceOutline(true);

            // If the ending block is set, display the distance.
            if (DestinyTools.endingBlock)
            {
                Vector3 start = DestinyTools.startingBlock.transform.localPosition;

                Vector3 distance = start - found.Item3;
                int x = (int)Mathf.Round(Mathf.Abs(distance.x) / 1.5f);
                int y = (int)Mathf.Round(Mathf.Abs(distance.y) / 2.42f);
                int z = (int)Mathf.Round(Mathf.Abs(distance.z) / 1.5f);
                message = (x > 0 ? $"X: {x} Block{(x > 1 ? "s" : "")} " : "") +
                          (y > 0 ? $"Y: {y} Floor{(y > 1 ? "s" : "")} " : "") +
                          (z > 0 ? $"Z: {z} Block{(z > 1 ? "s" : "")}" : "");
                message = "[MeasuringTape] " + (message.Length > 0 ? $"Distance: {message}" : "Distance: 0");
                HNotify.instance.AddNotification(HNotify.NotificationType.normal, message, 5);
            }
        }

        return message;
    }

    [ConsoleCommand(name: "stopMeasurement", docs: "Clears the start and end points of the measurements.")]
    public static string StopMeasurement()
    {
        if (DestinyTools.startingBlock)
        {
            DestinyTools.startingBlock.SetInstanceOutline(false);
            DestinyTools.startingBlock = null;
        }
        if (DestinyTools.endingBlock)
        {
            DestinyTools.endingBlock.SetInstanceOutline(false);
            DestinyTools.endingBlock = null;
        }


        return "[MeasuringTape] Measurement stopped.";
    }

    [ConsoleCommand(name: "defeatVarunaBoss", docs: "Progresses the Varuna point boss fight automatically until the shark is dead.")]
    public static void DefeatVarunaBoss()
    {
        float killValue = 999999f;


        // First get rid of the pillars.
        Array.ForEach(FindObjectsOfType<QuestInteractable_VarunaPillar>(), x =>
            {
                while (x.currentObjectStateIndex < x.objectStateMaxIndex)
                {
                    x.Interact(null, true);
                }
            });

        // Next destroy all the walls.
        Array.ForEach(FindObjectsOfType<QuestInteractable_VarunaBoss_Wall>(), x => x.Interact(null, true));

        // Finally, kill the shark.
        var sm = FindObjectOfType<AI_StateMachine_Boss_Varuna>();
        if (sm)
        {
            // Based on the decompiled game code.
            sm.networkBehaviour.networkEntity.IsInvurnerable = false;
            ComponentManager<Network_Host>.Value.DamageEntity(sm.networkBehaviour.networkEntity, sm.networkBehaviour.transform, 999999f, Vector3.zero, Vector3.up, EntityType.Player, null);
        }
    }

    // Returns a tuple of whether the block was found and valid, the block, and
    // it's position.
    private static ValueTuple<bool, Block, Vector3> GetBlockAtCursor()
    {
        RaycastHit r;
        if (!Helper.HitAtCursor(out r, 100f, (LayerMask)1024, QueryTriggerInteraction.UseGlobal))
        {
            return (false, (Block)null, new Vector3(0, 0, 0));
        }
        Block block = r.transform.GetComponentInParent<Block>();
        if (!block || !block.IsWalkable())
        {
            return (false, (Block)null, new Vector3(0, 0, 0));
        }

        return (true, block, block.transform.localPosition);
    }

    /*
     * Syncronizes reviving with other players.
     */
    private static void SendRevivePackets()
    {
        var pn = Traverse.Create(RAPI.GetLocalPlayer().PlayerScript).Field("playerNetwork").GetValue<Network_Player>();
        var messageRevStart = new Message_NetworkBehaviour(Messages.Respawn_Start, pn);
        var messageRevCompl = new Message_NetworkBehaviour(Messages.Respawn_Complete, pn);
        if (Raft_Network.IsHost)
        {
            DestinyTools.network.RPC(messageRevStart, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            DestinyTools.network.RPC(messageRevCompl, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
        else
        {
            pn.SendP2P(messageRevStart, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            pn.SendP2P(messageRevCompl, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
    }
}
