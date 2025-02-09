using UnityEngine;
using HarmonyLib;

namespace MeasureTwice.Patches;

[HarmonyPatch]
internal class CustomRuler : MonoBehaviour
{
    public float m_timeout = 1f;
    private ZNetView m_nview;
    private static bool m_triggerOnPlaced = false;
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_timeout = MeasureTwice.Instance.TimedDestruction.Value;
        if (m_triggerOnPlaced)
        {
            ApplyScale();
            InvokeRepeating("DestroyNow", m_timeout, 1f);
        }
    }

    public static void SetTriggerOnPlaced(bool trigger)
    {
        m_triggerOnPlaced = trigger;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    private static void PlacePiecePrefix()
    {
        SetTriggerOnPlaced(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    private static void PlacePiecePostfix()
    {
        SetTriggerOnPlaced(false);
    }

    public void ApplyScale()
    {
        transform.localScale = ScaleManager.ScaleOnPlaced;
    }

    public void DestroyNow()
    {
        if (m_nview)
        {
            if (m_nview.IsValid())
            {
                if (!m_nview.HasOwner())
                {
                    m_nview.ClaimOwnership();
                }
                if (m_nview.IsOwner())
                {
                    ZNetScene.instance.Destroy(gameObject);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
