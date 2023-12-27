using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
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

        private static Terminal terminal = null;
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchLoadResources(StartOfRound __instance)
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
            
            // patch spawnable items to include masks on all levels
            __instance.StartCoroutine(RegisterMasks());

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

        private static IEnumerator RegisterMasks()
        {
            if (Plugin.enableChangeMaskSpawnChance.Value == 0)
            {
                yield break;
            }
            
            
            // find the terminal object
            while (!terminal)
            {
                terminal = Object.FindObjectOfType<Terminal>();
                yield return new WaitForSeconds(1);
            }
            
            // change all items list comedy and tragedy scrap value according to config
            Item comedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Comedy");
            
            if (!comedyItem)
            {
                Plugin.Log.LogError("Comedy not found in allItemsList, skipping patching Comedy");
            }
            else
            {
                comedyItem.minValue = Plugin.minMaskItemBaseValue.Value;
                comedyItem.maxValue = Plugin.maxMaskItemBaseValue.Value;
            }
            
            
            Item tragedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Tragedy");
            
            if (!tragedyItem)
            {
                Plugin.Log.LogError("Tragedy not found in allItemsList, skipping patching Tragedy");
            }
            else
            {
                tragedyItem.minValue = Plugin.minMaskItemBaseValue.Value;
                tragedyItem.maxValue = Plugin.maxMaskItemBaseValue.Value;
            }

            SelectableLevel[] moonsCatalogueList = terminal.moonsCatalogueList;
            float multiplier = Plugin.maskRarityScalingMultiplier.Value;
            foreach (SelectableLevel level in moonsCatalogueList)
            {
                int rarity = Mathf.Clamp(Mathf.RoundToInt(Plugin.maskRarity.Value *
                                                          (1 + (Plugin.maskRarityScaling.Value ? multiplier : 0))), 0, 100);
                
                if (comedyItem)
                {
                    SpawnableItemWithRarity comedyWithRarity = new SpawnableItemWithRarity();
                    comedyWithRarity.spawnableItem = comedyItem;
                    comedyWithRarity.rarity = rarity;
                    
                    int levelIndex = level.spawnableScrap.FindIndex(item => item.spawnableItem.itemName == "Comedy");
                    if (levelIndex == -1 && Plugin.enableChangeMaskSpawnChance.Value == 2)
                    {
                        level.spawnableScrap.Add(comedyWithRarity);
                    }
                    else
                    {
                        level.spawnableScrap[levelIndex] = comedyWithRarity;
                    }
                }

                if (tragedyItem)
                {
                    SpawnableItemWithRarity tragedyWithRarity = new SpawnableItemWithRarity();
                    tragedyWithRarity.spawnableItem = tragedyItem;
                    tragedyWithRarity.rarity = rarity;
                    
                    int levelIndex = level.spawnableScrap.FindIndex(item => item.spawnableItem.itemName == "Tragedy");
                    if (levelIndex == -1 && Plugin.enableChangeMaskSpawnChance.Value == 2)
                    {
                        level.spawnableScrap.Add(tragedyWithRarity);
                    }
                    else
                    {
                        level.spawnableScrap[levelIndex] = tragedyWithRarity;
                    }
                }

                if (Plugin.maskRarityScaling.Value)
                {
                    multiplier *= Plugin.maskRarityScalingMultiplier.Value;
                }
            }
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
            
            minTimeToSwitchSlots = Plugin.minTimeToSwitchSlots.Value;
            maxTimeToSwitchSlots = Plugin.maxTimeToSwitchSlots.Value;
            
            minTimeToPossess = Plugin.minTimeToPossess.Value;
            maxTimeToPossess = Plugin.maxTimeToPossess.Value;
            
            minTimeToPossessPlayer = Plugin.minTimeToPossessPlayer.Value;
            maxTimeToPossessPlayer = Plugin.maxTimeToPossessPlayer.Value;
            Plugin.Log.LogInfo("Reset timers and reloaded config values");
        }
        
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void PatchUpdate()
        {
            // If player isn't initialized or dead cancel
            if (localPlayer == null || 
                localPlayer.isPlayerDead || 
                StartOfRound.Instance.inShipPhase ||
                StartOfRound.Instance.currentLevelID == 3)
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
            if (Plugin.enableMaskPossessionMechanic.Value &&
                currentHeld != null && 
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
                    
                    PossessPlayer(time);
                    nextTimeToSwitchSlot = time + 1;
                }
            }
            // Else if the player holds a mask try to switch slots to the closest mask
            else if (Plugin.enableMaskSwitchSlotMechanic.Value && 
                     timeHeldByPlayer > Plugin.timeToStartSwitchingSlots.Value && 
                     nextTimeToSwitchSlot <= 0f)
            {
                Plugin.Log.LogMessage("Player is not holding a mask, switching to closest held mask");

                int numOfSlots = localPlayer.ItemSlots.Length;
                GrabbableObject[] inventory = localPlayer.ItemSlots;
                int currIndex = localPlayer.currentItemSlot;
                bool found = false;
                bool forward = false;
                bool oneAway = false;
                for (int i = 1; i <= numOfSlots / 2; i++)
                {
                    int checkIndex = ((currIndex + i) % numOfSlots + numOfSlots) % numOfSlots;
                    Plugin.Log.LogMessage($"Checking slot: {checkIndex}");
                    GrabbableObject item = inventory[checkIndex];
                    if (item != null && item.GetType() == typeof(HauntedMaskItem))
                    {
                        Plugin.Log.LogMessage("Success! (+1)");
                        found = true;
                        forward = true;
                        if (i == 1)
                            oneAway = true;
                        break;
                    }
                    
                    checkIndex = ((currIndex - i) % numOfSlots + numOfSlots) % numOfSlots;
                    Plugin.Log.LogMessage($"Checking slot: {checkIndex}");
                    item = inventory[checkIndex];
                    if (item != null && item.GetType() == typeof(HauntedMaskItem))
                    {
                        Plugin.Log.LogMessage("Success! (-1)");
                        found = true;
                        forward = false;
                        if (i == 1)
                            oneAway = true;
                        break;
                    }
                }

                if (!found)
                {
                    Plugin.Log.LogWarning($"No masks were found even though player has held a mask");
                    return;
                }

                if (currentHeld != null && currentHeld.isBeingUsed)
                {
                    currentHeld.ItemActivate(false, false);
                }
                
                bool currentIsTwoHanded = currentHeld != null && currentHeld.itemProperties.twoHanded;
                if (oneAway)
                    IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Disable();
                if (localPlayer.currentlyHeldObject != null && localPlayer.currentlyHeldObject.GetType() == typeof(HauntedMaskItem))
                {
                    return;
                }
                localPlayer.StartCoroutine(
                    SwitchSlot(
                        forward, 
                        currentIsTwoHanded,
                        oneAway
                        )
                    );
                
                nextTimeToSwitchSlot = Random.Range(minTimeToSwitchSlots, maxTimeToSwitchSlots);
                minTimeToSwitchSlots -= (minTimeToSwitchSlots > minSwitchingSlotTime ? deltaTimeToSwitchSlots : 0f);
                maxTimeToSwitchSlots -= (maxTimeToSwitchSlots > minSwitchingSlotTime ? deltaTimeToSwitchSlots : 0f);
                Plugin.Log.LogInfo("Next switch in " + nextTimeToSwitchSlot + " seconds");
            }
        }
        
        private static void PossessPlayer(float time)
        {
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Disable();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").Disable();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard").Enable();
            
            if (localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.GetType() == typeof(HauntedMaskItem))
            {
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Disable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("ActivateItem").Disable();
                ShipBuildModeManager.Instance.CancelBuildMode();
                
                localPlayer.currentlyHeldObjectServer.UseItemOnClient();
                localPlayer.currentlyHeldObjectServer.GetComponent<AudioSource>().PlayOneShot(possessionSounds[Random.Range(0, possessionSounds.Length)], localPlayer.itemAudio.volume * 0.75f);
                localPlayer.StartCoroutine(DePossessPlayer(time));
            }
            else
            {
                nextTimeToSwitchSlot = 0;
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Enable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").Enable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard").Enable();
            }
        }
        
        private static IEnumerator DePossessPlayer(float time)
        {
            yield return new WaitForSeconds(time);
            ShipBuildModeManager.Instance.CancelBuildMode();
            if (localPlayer.currentlyHeldObjectServer != null)
            {
                localPlayer.currentlyHeldObjectServer.UseItemOnClient(buttonDown: false);
            }
            yield return new WaitForEndOfFrame();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("ActivateItem").Enable();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").Enable();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Enable();
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard").Enable();
        }

        private static IEnumerator SwitchSlot(bool forward, bool currentIsTwoHanded, bool oneAway)
        {
            if (currentIsTwoHanded)
            {
                if (Plugin.twoHandedItemBehaviour.Value)
                {
                    GrabbableObject current = localPlayer.currentlyHeldObject;
                    yield return localPlayer.StartCoroutine(localPlayer.waitToEndOfFrameToDiscard());
                    current.EnableItemMeshes(true);
                    yield return new WaitForEndOfFrame();
                    if (Random.Range(0f, 1f) < 0.25f)
                        localPlayer.itemAudio.PlayOneShot(slotSwitchSounds[Random.Range(0, slotSwitchSounds.Length)], localPlayer.itemAudio.volume * 0.25f);
                    localPlayer.SwitchToItemSlot(localPlayer.NextItemSlot(forward));
                    localPlayer.SwitchItemSlotsServerRpc(forward);
                }
            }
            else
            {
                if (Random.Range(0f, 1f) < 0.25f)
                    localPlayer.itemAudio.PlayOneShot(slotSwitchSounds[Random.Range(0, slotSwitchSounds.Length)], localPlayer.itemAudio.volume * 0.25f);
                yield return new WaitForEndOfFrame();
                localPlayer.SwitchToItemSlot(localPlayer.NextItemSlot(forward));
                localPlayer.SwitchItemSlotsServerRpc(forward);
            }

            if (oneAway)
            {
                yield return new WaitForSeconds(0.5f);
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").Enable();
            }
        }
    }
}