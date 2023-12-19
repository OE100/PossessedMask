using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatch
    {
        private static PlayerControllerB localPlayer = null;
        private static Dictionary<HauntedMaskItem, float> timeHeldByPlayer = new Dictionary<HauntedMaskItem, float>();
        private static float nextTimeToSwitchSlot = 0f;

        private static AudioClip[] possessionSounds = new AudioClip[3];
        private static AudioClip[] slotSwitchSounds = new AudioClip[2];
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchLoadResources()
        {
            AssetBundle ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(Assembly.GetExecutingAssembly().GetManifestResourceNames()[0]));
            if (ab == null)
            {
                Plugin.Log.LogError("Failed to load asset bundle");
                return;
            }
            Plugin.Log.LogInfo("Asset bundle loaded");
            possessionSounds[0] = ab.LoadAsset<AudioClip>("possession1");
            possessionSounds[1] = ab.LoadAsset<AudioClip>("possession2");
            possessionSounds[2] = ab.LoadAsset<AudioClip>("possession3");
            slotSwitchSounds[0] = ab.LoadAsset<AudioClip>("slot1");
            slotSwitchSounds[1] = ab.LoadAsset<AudioClip>("slot2");
        }
        
        [HarmonyPatch("openingDoorsSequence")]
        [HarmonyPostfix]
        private static void PatchOpeningDoorsSequence(StartOfRound __instance)
        {
            // set local player
            localPlayer = __instance.localPlayerController;
            // clear dictionary
            timeHeldByPlayer.Clear();
            nextTimeToSwitchSlot = 0f;
            Plugin.Log.LogInfo("timeHeldByPlayer cleared and timers reset");
        }
        
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void PatchUpdate()
        {
            // If player isn't initialized or dead cancel
            if (localPlayer == null || 
                localPlayer.isPlayerDead || 
                StartOfRound.Instance.inShipPhase)
            {
                return;
            }
            // Reduce time until switch or possession
            nextTimeToSwitchSlot -= Time.deltaTime;
            
            // Check player inventory for masks
            int longestHeldIndex = -1;
            float longestHeldTime = 0;
            
            for (int ind = 0; ind < localPlayer.ItemSlots.Length; ind++)
            {
                var currentItem = localPlayer.ItemSlots[ind];
                if (currentItem == null ||
                    currentItem.GetType() != typeof(HauntedMaskItem)) continue;
                // If found mask add to it's count and check if it's the longest in inventory or currently held
                float time;
                if (timeHeldByPlayer.ContainsKey((HauntedMaskItem)currentItem))
                {
                    time = timeHeldByPlayer[(HauntedMaskItem)currentItem] += Time.deltaTime;
                }
                else
                {
                    time = timeHeldByPlayer[(HauntedMaskItem)currentItem] = Time.deltaTime;
                }

                if (!(longestHeldTime < time)) continue;
                
                longestHeldIndex = ind;
                longestHeldTime = time;
            }
            // If a mask is the currently held item try to possess player
            var currentHeld = localPlayer.ItemSlots[localPlayer.currentItemSlot];
            if (currentHeld != null && 
                currentHeld.GetType() == typeof(HauntedMaskItem))
            {
                if (timeHeldByPlayer[(HauntedMaskItem)currentHeld] > 30f)
                {
                    float time = Random.Range(0.5f, 5f);
                    Plugin.Log.LogInfo("Player has held mask for " + timeHeldByPlayer[(HauntedMaskItem)currentHeld] + " seconds, possessing for " + time + " seconds");
                    timeHeldByPlayer[(HauntedMaskItem)currentHeld] = -time;
                    IEnumerator possessPlayerDelayed = PossessPlayer((HauntedMaskItem)currentHeld, time);
                    localPlayer.StartCoroutine(possessPlayerDelayed);
                }
            }
            // Else if the player holds a mask try to switch slots to the mask held for the longest time
            else if (longestHeldIndex != -1 && nextTimeToSwitchSlot <= 0f)
            {
                Plugin.Log.LogInfo("Player is not holding a mask, switching to longest held mask");
                int numOfSlots = localPlayer.ItemSlots.Length;
                int halfOfSlots = numOfSlots / 2;
                int currentIndex = localPlayer.currentItemSlot;
                int switchToIndex;
                if (currentIndex < longestHeldIndex)
                {
                    if ((currentIndex - longestHeldIndex) % numOfSlots >= halfOfSlots)
                        switchToIndex = localPlayer.NextItemSlot(true);
                    else
                        switchToIndex = localPlayer.NextItemSlot(true);
                }
                else
                {
                    if ((currentIndex - longestHeldIndex) % numOfSlots < halfOfSlots)
                        switchToIndex = localPlayer.NextItemSlot(true);
                    else
                        switchToIndex = localPlayer.NextItemSlot(true);
                }

                Plugin.Log.LogInfo("Switching to inventory slot " + switchToIndex);
                if (localPlayer.ItemSlots[localPlayer.currentItemSlot].itemProperties.twoHanded)
                    localPlayer.DiscardHeldObject();
                if (Random.Range(0f, 1f) < 0.25f)
                    localPlayer.itemAudio.PlayOneShot(slotSwitchSounds[Random.Range(0, slotSwitchSounds.Length)], localPlayer.itemAudio.volume * 0.25f);
                localPlayer.SwitchToItemSlot(switchToIndex);

                nextTimeToSwitchSlot = Random.Range(7f, 15f);
                Plugin.Log.LogInfo("Next switch in " + nextTimeToSwitchSlot + " seconds");
            }
        }

        private static IEnumerator PossessPlayer(HauntedMaskItem item, float time)
        {
            item.ItemActivate(true);
            item.GetComponent<AudioSource>().PlayOneShot(possessionSounds[Random.Range(0, possessionSounds.Length)], localPlayer.itemAudio.volume * 0.75f);
            yield return new WaitForSeconds(time);
            item.ItemActivate(false, false);
            item.CancelAttachToPlayerOnLocalClient();
        }
    }
}