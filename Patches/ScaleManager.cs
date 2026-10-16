using HarmonyLib;
using UnityEngine;

namespace MeasureTwice.Patches;

[HarmonyPatch]
internal static class ScaleManager
{
    public const float MinLength = 0.1f;
    public const float MaxLength = 10f;
    private const float Tolerance = 0.01f;

    private class RulerInfo
    {
        public float LastOriginalLength = 0f;
        public float LastTotalDelta = 0f;
        public Vector3 LastGhostScale = Vector3.one;

        public void Reset()
        {
            LastOriginalLength = 0f;
            LastTotalDelta = 0f;
            LastGhostScale = Vector3.one;   
        }
    }
    private static RulerInfo HorzRulerInfo = new ();
    private static RulerInfo VertRulerInfo = new();
    private static RulerInfo ActiveRulerInfo = new();

    private static bool SpacerBlockIsInUse = false;
    private static bool DisabledScroll = false;

    public static Vector3 GetScaleOnPlaced(bool horiztonal)
    {
        if (horiztonal)
        {
            return HorzRulerInfo.LastGhostScale;
        }
        return VertRulerInfo.LastGhostScale;
    }

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
                VertRulerInfo.Reset();
                HorzRulerInfo.Reset();
            }
            return;
        }

        if (!IsValidSelectedPiece(__instance, out CustomRuler spacerBlock))
        {
            return;
        }

        // update that spacer block is in use and cache values
        if (!SpacerBlockIsInUse && spacerBlock)
        {
            SpacerBlockIsInUse = true;
            if (spacerBlock.m_horiztonal)
            {
                HorzRulerInfo.LastOriginalLength = spacerBlock.GetLastOriginalLength();
                HorzRulerInfo.LastGhostScale = spacerBlock.transform.localScale;
            }
            else
            {
                VertRulerInfo.LastOriginalLength = spacerBlock.GetLastOriginalLength();
                VertRulerInfo.LastGhostScale = spacerBlock.transform.localScale;
            }
        }

        if (ShouldModifyLength())
        {
            DisabledScroll = true; // disable scroll wheel
            ZInput.instance?.m_mouseScrollDeltaAction.Disable();
            SetLength(__instance, spacerBlock, Input.mouseScrollDelta.y * MeasureTwice.Instance.ScrollSpeed.Value);
        }

        // this is constantly refreshing if FastTools is in use but turns out that bug
        // occurs when using FastTools even without TerrainTools
        RefreshGhostScale(__instance, spacerBlock);
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
        if (spacerBlock.m_horiztonal)
        {
            HorzRulerInfo.LastTotalDelta = Mathf.Clamp(
                HorzRulerInfo.LastTotalDelta + delta,
                MinLength - HorzRulerInfo.LastOriginalLength,
                MaxLength - HorzRulerInfo.LastOriginalLength
            );
            HorzRulerInfo.LastGhostScale.x = ModifyLength(HorzRulerInfo.LastOriginalLength, HorzRulerInfo.LastTotalDelta);
            player.Message(MessageHud.MessageType.Center, $"Ruler Length: {HorzRulerInfo.LastGhostScale.x:#,0.000}");
        }
        else
        {
            VertRulerInfo.LastTotalDelta = Mathf.Clamp(
                VertRulerInfo.LastTotalDelta + delta,
                MinLength - VertRulerInfo.LastOriginalLength,
                MaxLength - VertRulerInfo.LastOriginalLength
            );
            VertRulerInfo.LastGhostScale.y = ModifyLength(VertRulerInfo.LastOriginalLength, VertRulerInfo.LastTotalDelta);
            player.Message(MessageHud.MessageType.Center, $"Ruler Length: {VertRulerInfo.LastGhostScale.y:#,0.000}");
        }        
    }

    private static void RefreshGhostScale(Player player, CustomRuler spacerBlock)
    {
        if (!SpacerBlockIsInUse || !player.m_placementGhost)
        {
            return;
        }
     
        if (spacerBlock.m_horiztonal && HorzRulerInfo.LastOriginalLength > 0.0f) 
        {
            ActiveRulerInfo = HorzRulerInfo;
        }
        else if (VertRulerInfo.LastOriginalLength > 0.0f)
        {
            ActiveRulerInfo = VertRulerInfo;
        }
        else
        {
            return;
        }

        float diff = Vector3.Distance(player.m_placementGhost.transform.localScale, ActiveRulerInfo.LastGhostScale);
        if (diff > Tolerance)
        {
            player.m_placementGhost.transform.localScale = ActiveRulerInfo.LastGhostScale;
        }
    }
}
