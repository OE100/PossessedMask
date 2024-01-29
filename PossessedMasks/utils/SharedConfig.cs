using System.Collections;
using PossessedMasks.networking;
using UnityEngine;

namespace PossessedMasks;

public static class SharedConfig
{
    public static int ItemCount { get; set; }
    public static bool TwoHandedBehaviour { get; set; }

    public static bool LurkingMechanicEnabled { get; set; }

    internal static IEnumerator DelayedRequestConfig()
    {
        if (Utils.HostCheck)
        {
            yield return new WaitUntil(() => ModConfig.Loaded);
            ItemCount = ModConfig.NumberOfSlotsFilledToEnableDroppingMask.Value;
            TwoHandedBehaviour = ModConfig.TwoHandedItemBehaviour.Value;
            LurkingMechanicEnabled = ModConfig.EnableMaskLurkingMechanic.Value;
        }
        else
        {
            yield return new WaitUntil(() => PossessedBehaviour.Instance);
            PossessedBehaviour.Instance.RequestConfigServerRpc();

            yield return new WaitUntil(() => CrawlingBehaviour.Instance);
            CrawlingBehaviour.Instance.RequestConfigServerRpc();
        }
    }
}