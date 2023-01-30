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
                            switch (rgd.slots[0].itemAmount)
                            {
                                // First versions used actual image bytes. We
                                // can no longer fix this because of the 4.0.0
                                // update.
                                case 0:
                                    CustomBlocks.DebugLog("Found flag with old save data. Use a previous version of the mod to update this save.");
                                    imageData = new byte[0];
                                    break;
                                // This was used in all versions of CustomBlocks
                                // before 3.1.0. Posters with this version need
                                // to be scaled down to half size.
                                case 1:
                                    if (block is Block_CustomPoster)
                                    {
                                        imageData = imageData.FixPoster(cb.GetBlockType());
                                    }
                                    break;
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
