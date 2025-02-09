using HarmonyLib;
using UnityEngine;

namespace MeasureTwice.Patches;

[HarmonyPatch]
internal static class ScaleManager
{
    private const float MinLength = 0.1f;
    private const float MaxLength = 2f;
    private const float Tolerance = 0.01f;

    private static bool SpacerBlockIsInUse = false;
    private static float LastOriginalLength = 0f;
    private static float LastTotalDelta = 0f;
    private static Vector3 LastGhostScale = Vector3.one;
    private static bool DisabledScroll = false;

    public static Vector3 ScaleOnPlaced => LastGhostScale;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    private static void UpdatePlacementPrefix(Player __instance)
    {
        if (!__instance || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (!__instance.InPlaceMode() || Hud.IsPieceSelectionVisible())
        {
            if (SpacerBlockIsInUse)
            {
                SpacerBlockIsInUse = false;
                LastOriginalLength = 0f;
                LastTotalDelta = 0f;
                LastGhostScale = Vector3.zero;
            }
            return;
        }

        if (!IsValidSelectedPiece(__instance, out CustomRuler spacerBlock))
        {
            return;
        }

        if (ShouldModifyLength())
        {
            DisabledScroll = true; // disable scroll wheel
            ZInput.instance?.m_mouseScrollDeltaAction.Disable();
            SetLength(__instance, spacerBlock, Input.mouseScrollDelta.y * MeasureTwice.Instance.ScrollSpeed.Value);
        }

        // this is constantly refreshing if FastTools is in use but turns out that bug
        // occurs when using FastTools even without TerrainTools
        RefreshGhostScale(__instance);
    }

    /// <summary>
    ///     Re-enable scroll wheel if it was disabled.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__state"></param>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    private static void UpdatePlacementPostfix(Player __instance, ref int __state)
    {
        if (DisabledScroll)
        {
            DisabledScroll = false;
            ZInput.instance?.m_mouseScrollDeltaAction.Enable();
        }
    }

    /// <summary>
    ///     Checks if selected piece is a valid target for modifying radius.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="spacerBlock"></param>
    /// <returns></returns>
    internal static bool IsValidSelectedPiece(Player player, out CustomRuler spacerBlock)
    {
        Piece piece = player.GetSelectedPiece();
        if (!piece || !piece.TryGetComponent(out spacerBlock))
        {
            spacerBlock = null;
            return false;
        }
        return spacerBlock;
    }

    /// <summary>
    ///     Checks if radius modification is enabled and being adjusted.
    /// </summary>
    /// <returns></returns>
    internal static bool ShouldModifyLength()
    {
        return Input.GetKey(MeasureTwice.Instance.LengthModifierKey.Value) && Input.mouseScrollDelta.y != 0;
    }

    public static float ModifyLength(float length, float delta)
    {
        return Mathf.Clamp(length + delta, MinLength, MaxLength);
    }

    private static void SetLength(Player player, CustomRuler spacerBlock, float delta)
    {
        if (!SpacerBlockIsInUse && spacerBlock)
        {
            SpacerBlockIsInUse = true;
            LastOriginalLength = spacerBlock.transform.localScale.x;
            LastGhostScale = spacerBlock.transform.localScale;
        }
        LastTotalDelta = Mathf.Clamp(
            LastTotalDelta + delta,
            MinLength - LastOriginalLength,
            MaxLength - LastOriginalLength
        );
        LastGhostScale.x = ModifyLength(LastOriginalLength, LastTotalDelta);
        player.Message(MessageHud.MessageType.Center, $"Spacer Length: {LastGhostScale.x:#,0.000}");
    }

    private static void RefreshGhostScale(Player player)
    {
        if (!SpacerBlockIsInUse || !player.m_placementGhost || LastOriginalLength == 0f)
        {
            return;
        }

        float diff = Vector3.Distance(player.m_placementGhost.transform.localScale, LastGhostScale);
        if (diff > Tolerance)
        {
            player.m_placementGhost.transform.localScale = LastGhostScale;
        }
    }
}
