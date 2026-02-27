using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class VoiceZone : UdonSharpBehaviour
{
    [Tooltip("ID unik untuk zona ini (HARUS berupa angka lebih dari 0, misal: 1, 2, 3, dst). Jangan ada ID yang sama antar zona.")]
    public int zoneId = 1;

    private Collider zoneCollider;
    private VoiceZoneManager managerInstance;

    void Start()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneId == 0)
        {
            Debug.LogError($"[VoiceZone] GameObject '{gameObject.name}' memiliki Zone ID 0. Harap ganti dengan angka unik yang lebih besar dari 0.");
        }

        // Cari Manager di scene. Pastikan nama GameObject manager Anda adalah "VoiceZoneManager".
        GameObject managerObject = GameObject.Find("VoiceZoneManager");
        if (managerObject != null)
        {
            managerInstance = managerObject.GetComponent<VoiceZoneManager>();
        }

        if (managerInstance == null)
        {
            Debug.LogError($"[VoiceZone] Tidak dapat menemukan 'VoiceZoneManager' di scene. Pastikan ada GameObject dengan nama tersebut dan skrip VoiceZoneManager di dalamnya.");
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (Utilities.IsValid(player) && managerInstance != null)
        {
            managerInstance._OnPlayerEnterZone(player, zoneId);
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (Utilities.IsValid(player) && managerInstance != null)
        {
            managerInstance._OnPlayerExitZone(player, zoneId);
        }
    }

    // Fungsi helper untuk Manager
    public bool _IsPositionInside(Vector3 position)
    {
        if (zoneCollider == null) return false;
        return zoneCollider.bounds.Contains(position);
    }
}
