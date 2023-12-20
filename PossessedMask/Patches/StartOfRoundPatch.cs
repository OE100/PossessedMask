using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatch
    {
        private static PlayerControllerB localPlayer = null;
        private static float timeHeldByPlayer = 0f;
        private static float nextTimeToSwitchSlot = 0f;
        private static float timeUntilPossession = 0f;
        
        
        private static AudioClip[] possessionSounds = new AudioClip[3];
        private static AudioClip[] slotSwitchSounds = new AudioClip[2];

        private static float minTimeToSwitchSlots;
        private static float maxTimeToSwitchSlots;
        private static float deltaTimeToSwitchSlots;
        private static float minSwitchingSlotTime;
        
        private static float minTimeToPossess;
        private static float maxTimeToPossess;
        private static float deltaTimeToPossess;
        private static float minPossessingTime;
        
        private static float minTimeToPossessPlayer;
        private static float maxTimeToPossessPlayer;
        private static float deltaTimeToPossessPlayer;
        private static float maxPossessingPlayerTime;
        
        
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
            
            // config loading
            minTimeToSwitchSlots = Plugin.minTimeToSwitchSlots.Value;
            maxTimeToSwitchSlots = Plugin.maxTimeToSwitchSlots.Value;
            deltaTimeToSwitchSlots = Plugin.deltaTimeToSwitchSlots.Value;
            minSwitchingSlotTime = Plugin.minSwitchingSlotTime.Value;
            
            minTimeToPossess = Plugin.minTimeToPossess.Value;
            maxTimeToPossess = Plugin.maxTimeToPossess.Value;
            deltaTimeToPossess = Plugin.deltaTimeToPossess.Value;
            minPossessingTime = Plugin.minPossessingTime.Value;
            
            minTimeToPossessPlayer = Plugin.minTimeToPossessPlayer.Value;
            maxTimeToPossessPlayer = Plugin.maxTimeToPossessPlayer.Value;
            deltaTimeToPossessPlayer = Plugin.deltaTimeToPossessPlayer.Value;
            maxPossessingPlayerTime = Plugin.maxPossessingPlayerTime.Value;
        }
        
        [HarmonyPatch("openingDoorsSequence")]
        [HarmonyPostfix]
        private static void PatchOpeningDoorsSequence(StartOfRound __instance)
        {
            // set local player
            localPlayer = __instance.localPlayerController;
            // clear dictionary
            timeUntilPossession = Plugin.timeToStartPossession.Value;
            timeHeldByPlayer = 0f;
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
            bool hasMask = false;
            
            for (int ind = 0; ind < localPlayer.ItemSlots.Length; ind++)
            {
                var currentItem = localPlayer.ItemSlots[ind];
                if (currentItem == null ||
                    currentItem.GetType() != typeof(HauntedMaskItem)) continue;
                // If found mask add to it's count and check if it's the longest in inventory or currently held
                hasMask = true;
                break;
            }
            if (hasMask)
            {
                timeHeldByPlayer += Time.deltaTime;
                timeUntilPossession -= Time.deltaTime;
            }
            else
            {
                return;
            }
            // If a mask is the currently held item try to possess player
            var currentHeld = localPlayer.ItemSlots[localPlayer.currentItemSlot];
            if (currentHeld != null && 
                currentHeld.GetType() == typeof(HauntedMaskItem))
            {
                if (timeUntilPossession < 0f)
                {
                    float time = Random.Range(minTimeToPossessPlayer, maxTimeToPossessPlayer);
                    minTimeToPossessPlayer += (minTimeToPossessPlayer < maxPossessingPlayerTime ? deltaTimeToPossessPlayer : 0f);
                    maxTimeToPossessPlayer += (maxTimeToPossessPlayer < maxPossessingPlayerTime ? deltaTimeToPossessPlayer : 0f);
                    
                    Plugin.Log.LogInfo($"Player has held mask for enough time, possessing for {time} seconds");
                    
                    timeUntilPossession = Random.Range(minTimeToPossess, maxTimeToPossess);
                    minTimeToPossess -= (minTimeToPossess > minPossessingTime ? deltaTimeToPossess : 0f);
                    maxTimeToPossess -= (maxTimeToPossess > minPossessingTime ? deltaTimeToPossess : 0f);
                    
                    IEnumerator possessPlayerDelayed = PossessPlayer((HauntedMaskItem)currentHeld, time);
                    localPlayer.StartCoroutine(possessPlayerDelayed);
                }
            }
            // Else if the player holds a mask try to switch slots to the closest mask
            else if (timeHeldByPlayer > Plugin.timeToStartSwitchingSlots.Value && 
                     nextTimeToSwitchSlot <= 0f)
            {
                Plugin.Log.LogMessage("Player is not holding a mask, switching to closest held mask");

                int numOfSlots = localPlayer.ItemSlots.Length;
                GrabbableObject[] inventory = localPlayer.ItemSlots;
                int currIndex = localPlayer.currentItemSlot;
                int switchToIndex = -1;
                for (int i = 1; i <= numOfSlots / 2; i++)
                {
                    int checkIndex = ((currIndex + i) % numOfSlots + numOfSlots) % numOfSlots;
                    Plugin.Log.LogMessage($"Checking slot: {checkIndex}");
                    GrabbableObject item = inventory[checkIndex];
                    if (item != null && item.GetType() == typeof(HauntedMaskItem))
                    {
                        Plugin.Log.LogMessage("Success! (+1)");
                        switchToIndex = ((currIndex + 1) % numOfSlots + numOfSlots) % numOfSlots;
                        break;
                    }
                    
                    checkIndex = ((currIndex - i) % numOfSlots + numOfSlots) % numOfSlots;
                    Plugin.Log.LogMessage($"Checking slot: {checkIndex}");
                    item = inventory[checkIndex];
                    if (item != null && item.GetType() == typeof(HauntedMaskItem))
                    {
                        Plugin.Log.LogMessage("Success! (-1)");
                        switchToIndex = ((currIndex - 1) % numOfSlots + numOfSlots) % numOfSlots;
                        break;
                    }
                }

                Plugin.Log.LogMessage($"Switching to inventory slot {switchToIndex}, current item is two handed: {currentHeld?.itemProperties.twoHanded}");
                bool currentIsTwoHanded = currentHeld != null && currentHeld.itemProperties.twoHanded;
                localPlayer.StartCoroutine(
                    SwitchSlot(
                        switchToIndex, 
                        currentIsTwoHanded
                        )
                    );
                
                nextTimeToSwitchSlot = Random.Range(minTimeToSwitchSlots, maxTimeToSwitchSlots);
                minTimeToSwitchSlots -= (minTimeToSwitchSlots > minSwitchingSlotTime ? deltaTimeToSwitchSlots : 0f);
                maxTimeToSwitchSlots -= (maxTimeToSwitchSlots > minSwitchingSlotTime ? deltaTimeToSwitchSlots : 0f);
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

        private static IEnumerator SwitchSlot(int slot, bool currentIsTwoHanded)
        {
            if (Plugin.twoHandedItemBehaviour.Value)
            {
                if (currentIsTwoHanded)
                    yield return localPlayer.StartCoroutine(localPlayer.waitToEndOfFrameToDiscard());
                if (Random.Range(0f, 1f) < 0.25f)
                    localPlayer.itemAudio.PlayOneShot(slotSwitchSounds[Random.Range(0, slotSwitchSounds.Length)], localPlayer.itemAudio.volume * 0.25f);
                localPlayer.SwitchToItemSlot(slot);
            }
        }
    }
}