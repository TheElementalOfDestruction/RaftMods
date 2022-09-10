using HarmonyLib;
using System;


namespace DestinyCustomBlocks
{
    [HarmonyPatch(typeof(RGD_Block), "RestoreBlock")]
    public class Patch_RestoreBlock
    {
        public static void Postfix(ref RGD_Block __instance, Block block)
        {
            // Make sure it is one of our blocks.
            if (__instance.BlockIndex >= CustomBlocks.CUSTOM_BLOCK_ID_MIN && __instance.BlockIndex <= CustomBlocks.CUSTOM_BLOCK_ID_MAX)
            {
                RGD_Storage rgd = __instance as RGD_Storage;
                if (rgd != null)
                {
                    ICustomBlock cb = block as ICustomBlock;
                    if (cb != null)
                    {
                        if (Raft_Network.IsHost || !CustomBlocks.IgnoreFlagMessages)
                        {
                            byte[] imageData = Convert.FromBase64String(rgd.slots[0].exclusiveString);
                            if (rgd.slots[0].itemAmount == 0)
                            {
                                CustomBlocks.DebugLog("Found flag with old save data. Updating to new save system.");
                                // Handle older saves with a different form of image
                                // save data.
                                if (Raft_Network.IsHost)
                                {
                                    imageData = imageData.SanitizeImage(cb.GetBlockType());
                                }
                                else
                                {
                                    // Small protection against unsafe save data
                                    // from remote host.
                                    imageData = new byte[0];
                                }
                            }
                            if (imageData != null)
                            {
                                cb.SetSendUpdates(false);
                                cb.SetImageData(imageData);
                                cb.SetSendUpdates(true);
                            }
                        }

                        // Handle the custom sail.
                        if (cb is Block_CustomSail)
                        {
                            Sail sail = block.GetComponent<Sail>();
                            if (rgd.isOpen)
                            {
                                sail?.Open();
                            }
                            sail?.SetRotation(BitConverter.ToSingle(BitConverter.GetBytes(rgd.storageObjectIndex), 0));
                        }
                        else if (cb is Block_CustomBlock_Interactable)
                        {
                            Placeable_Interactable interact = block.GetComponent<Placeable_Interactable>();
                            interact.RestoreIndex(BitConverter.ToInt32(BitConverter.GetBytes(rgd.storageObjectIndex), 0));
                        }
                    }
                }
            }
        }
    }
}