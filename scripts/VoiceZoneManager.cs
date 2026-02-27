using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// PENTING: Hanya boleh ada SATU GameObject dengan skrip ini di seluruh scene Anda.
/// Disarankan untuk menamakannya "VoiceZoneManager".
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class VoiceZoneManager : UdonSharpBehaviour
{
    [Header("Pengaturan Suara Global")]
    [Tooltip("Pengaturan ini berlaku untuk semua zona yang dikelola oleh Manager ini.")]
    public float gainInside = 15f;
    public float distanceNearInside = 0f;
    public float distanceFarInside = 25f;
    public bool lowpassInside = false;
    [Space]
    public float gainOutside = 10f;
    public float distanceNearOutside = 4f;
    public float distanceFarOutside = 8f;
    public bool lowpassOutside = true;

    [Header("Pengaturan Transisi")]
    [Range(1f, 10f)]
    public float transitionSpeed = 3.0f;

    #region Constants and State
    private const float DEFAULT_GAIN = 15f;
    private const float DEFAULT_NEAR = 0f;
    private const float DEFAULT_FAR = 25f;
    private const bool DEFAULT_LOWPASS = false;
    private const float LERP_THRESHOLD = 0.05f;

    private VRCPlayerApi[] trackedPlayers;
    private int[] playerZoneIds;
    private int playerCount;
    private bool isDirty = true;
    private VoiceZone[] allZones;
    private int localPlayerIndex = -1;
    #endregion

    void Start()
    {
        // Cari semua zona dengan mencari anak dari GameObject bernama "AllVoiceZones".
        GameObject zonesParent = GameObject.Find("AllVoiceZones");
        if (zonesParent != null)
        {
            // GetComponentsInChildren didukung oleh UdonSharp untuk tipe custom.
            allZones = zonesParent.GetComponentsInChildren<VoiceZone>(true); 
        }

        if (allZones == null || allZones.Length == 0)
        {
            Debug.LogWarning("[VoiceZoneManager] Tidak menemukan GameObject 'AllVoiceZones' atau tidak ada anak VoiceZone di dalamnya. Sistem akan tetap bekerja, namun tidak akan ada zona aktif.");
        }
        
        SendCustomEventDelayedSeconds(nameof(_InitializePlayerList), 2.5f);
    }
    
    public void _InitializePlayerList()
    {
        playerCount = VRCPlayerApi.GetPlayerCount();
        if (playerCount > 0)
        {
            trackedPlayers = new VRCPlayerApi[playerCount];
            playerZoneIds = new int[playerCount];
            VRCPlayerApi.GetPlayers(trackedPlayers);

            for (int i = 0; i < playerCount; i++)
            {
                var player = trackedPlayers[i];
                if (Utilities.IsValid(player))
                {
                    if (player.isLocal) localPlayerIndex = i;
                    playerZoneIds[i] = GetZoneIdForPosition(player.GetPosition());
                }
            }
        }
        isDirty = true;
    }
    
    public void _OnPlayerEnterZone(VRCPlayerApi player, int zoneId)
    {
        int index = GetPlayerIndex(player);
        if (index != -1) playerZoneIds[index] = zoneId;
        isDirty = true;
    }

    public void _OnPlayerExitZone(VRCPlayerApi player, int currentZoneId)
    {
        int index = GetPlayerIndex(player);
        if (index != -1 && playerZoneIds[index] == currentZoneId)
        {
            playerZoneIds[index] = 0;
        }
        isDirty = true;
    }
    
    #region Player Tracking Helpers
    private int GetPlayerIndex(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return -1;
        for (int i = 0; i < playerCount; i++)
        {
            if (Utilities.IsValid(trackedPlayers[i]) && trackedPlayers[i].playerId == player.playerId) return i;
        }
        return -1;
    }

    private int GetZoneIdForPosition(Vector3 position)
    {
        if (allZones == null) return 0;
        foreach (var zone in allZones)
        {
            if (zone != null && zone._IsPositionInside(position))
            {
                return zone.zoneId;
            }
        }
        return 0;
    }
    #endregion

    #region VRChat Events
    public override void OnPlayerJoined(VRCPlayerApi player) => SendCustomEventDelayedSeconds(nameof(_InitializePlayerList), 1.5f);
    public override void OnPlayerLeft(VRCPlayerApi player) => SendCustomEventDelayedSeconds(nameof(_InitializePlayerList), 1.5f);
    #endregion

    void Update()
    {
        if (!isDirty) return;
        if (trackedPlayers == null || playerCount == 0 || localPlayerIndex == -1) return;

        int transitionsPending = 0;
        int localPlayerZoneId = playerZoneIds[localPlayerIndex];

        for (int i = 0; i < playerCount; i++)
        {
            VRCPlayerApi targetPlayer = trackedPlayers[i];
            if (i == localPlayerIndex || !Utilities.IsValid(targetPlayer)) continue;

            float targetGain, targetNear, targetFar;
            bool targetLowpass;
            int targetPlayerZoneId = playerZoneIds[i];

            bool inSameZone = localPlayerZoneId == targetPlayerZoneId;

            targetGain      = inSameZone ? gainInside : gainOutside;
            targetNear      = inSameZone ? distanceNearInside : distanceNearOutside;
            targetFar       = inSameZone ? distanceFarInside : distanceFarOutside;
            targetLowpass   = inSameZone ? lowpassInside : lowpassOutside;

            if (localPlayerZoneId == 0 && targetPlayerZoneId == 0)
            {
                targetGain = DEFAULT_GAIN;
                targetNear = DEFAULT_NEAR;
                targetFar = DEFAULT_FAR;
                targetLowpass = DEFAULT_LOWPASS;
            }

            float currentGain = targetPlayer.GetVoiceGain();
            float currentFar = targetPlayer.GetVoiceDistanceFar();
            
            if (Mathf.Abs(currentGain - targetGain) > LERP_THRESHOLD || Mathf.Abs(currentFar - targetFar) > LERP_THRESHOLD)
            {
                transitionsPending++;
                
                float newGain = Mathf.Lerp(currentGain, targetGain, Time.deltaTime * transitionSpeed);
                float newNear = Mathf.Lerp(targetPlayer.GetVoiceDistanceNear(), targetNear, Time.deltaTime * transitionSpeed);
                float newFar = Mathf.Lerp(currentFar, targetFar, Time.deltaTime * transitionSpeed);

                targetPlayer.SetVoiceGain(newGain);
                targetPlayer.SetVoiceDistanceNear(newNear);
                targetPlayer.SetVoiceDistanceFar(newFar);
            }
            
            if (targetPlayer.GetVoiceLowpass() != targetLowpass)
            {
                targetPlayer.SetVoiceLowpass(targetLowpass);
            }
        }

        if (transitionsPending == 0) isDirty = false;
    }
}
