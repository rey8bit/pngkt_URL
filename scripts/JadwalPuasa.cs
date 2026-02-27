using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.StringLoading;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class JadwalPuasa : UdonSharpBehaviour
{
    [Header("GitHub Settings")]
    public VRCUrl jsonUrl;
    public float downloadInterval = 3600f;

    [Header("Optimization")]
    public float activeDistance = 30f;
    public float uiUpdateRate = 0.5f;

    [Header("City Settings")]
    public string cityPrefix = "Jakarta";

    [Header("Date Settings")]
    [Tooltip("Penyesuaian hari mulai puasa. 0 = Default. -1 untuk mulai 1 hari lebih awal. 1 untuk mulai 1 hari lebih lambat.")]
    [Range(-2, 2)]
    public int startDayOffset = 0;

    [Header("UI Text Displays")]
    public TextMeshProUGUI cityText;
    public TextMeshProUGUI ramadhanDayText;
    public TextMeshProUGUI imsakText;
    public TextMeshProUGUI subuhText;
    public TextMeshProUGUI maghribText;
    public TextMeshProUGUI isyaText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI timezoneText;

    private string _jsonRaw;
    private string _imsak = "--:--", _subuh = "--:--", _maghrib = "--:--", _isya = "--:--";
    private string _sahurStart = "--:--"; 
    private float _cityUtcOffset = 7f;
    private int _lastMinute = -1;
    private bool _isEid = false;
    private VRCPlayerApi _localPlayer;
    
    private string[] _availableCities;
    private int _currentCityIndex = 0;

    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
        float randomDelay = UnityEngine.Random.Range(0.1f, 3.0f);
        SendCustomEventDelayedSeconds(nameof(RefreshData), randomDelay);
        UpdateLoop();
    }

    public void UpdateLoop()
    {
        SendCustomEventDelayedSeconds(nameof(UpdateLoop), uiUpdateRate);
        if (_localPlayer == null || !_localPlayer.IsValid()) return;

        float dist = Vector3.Distance(_localPlayer.GetPosition(), transform.position);
        if (dist > activeDistance) return;

        DateTime cityTime = DateTime.UtcNow.AddHours(_cityUtcOffset);
        if (currentTimeText) currentTimeText.text = cityTime.ToString("HH:mm:ss");

        if (cityTime.Minute != _lastMinute)
        {
            _lastMinute = cityTime.Minute;
            if (!_isEid) UpdateStatus(cityTime.ToString("HH:mm"));
        }
    }

    public void RefreshData()
    {
        if (jsonUrl == null || string.IsNullOrEmpty(jsonUrl.Get())) return;
        VRCStringDownloader.LoadUrl(jsonUrl, (IUdonEventReceiver)this);
    }

    public void NextCity() { if (_availableCities == null) return; _currentCityIndex = (_currentCityIndex + 1) % _availableCities.Length; SelectCity(_availableCities[_currentCityIndex]); }
    public void PreviousCity() { if (_availableCities == null) return; _currentCityIndex--; if (_currentCityIndex < 0) _currentCityIndex = _availableCities.Length - 1; SelectCity(_availableCities[_currentCityIndex]); }

    public void SelectCity(string newPrefix)
    {
        cityPrefix = newPrefix;
        if (!string.IsNullOrEmpty(_jsonRaw)) ParseDailyData();
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        _jsonRaw = result.Result;
        string listStr = GetValueFromJson(_jsonRaw, "_cities_list");
        if (!string.IsNullOrEmpty(listStr))
        {
            _availableCities = listStr.Split(',');
            for (int i = 0; i < _availableCities.Length; i++) { if (_availableCities[i] == cityPrefix) { _currentCityIndex = i; break; } }
        }
        ParseDailyData();
        SendCustomEventDelayedSeconds(nameof(RefreshData), downloadInterval);
    }

    public override void OnStringLoadError(IVRCStringDownload result) { SendCustomEventDelayedSeconds(nameof(RefreshData), 60f); }

    private void ParseDailyData()
    {
        if (string.IsNullOrEmpty(_jsonRaw)) return;

        string cityName = GetValueFromJson(_jsonRaw, cityPrefix + "_city");
        string utcStr = GetValueFromJson(_jsonRaw, cityPrefix + "_utc");
        string startStr = GetValueFromJson(_jsonRaw, cityPrefix + "_start"); // Format: "2026-02-18"
        
        if (!float.TryParse(utcStr, out _cityUtcOffset)) _cityUtcOffset = 7f;
        if (cityText) cityText.text = "Kota: " + (string.IsNullOrEmpty(cityName) ? cityPrefix : cityName);
        
        if (timezoneText) {
            if (_cityUtcOffset == 7) timezoneText.text = "WIB";
            else if (_cityUtcOffset == 8) timezoneText.text = "WITA";
            else if (_cityUtcOffset == 9) timezoneText.text = "WIT";
            else timezoneText.text = "UTC+" + _cityUtcOffset;
        }

        // Parsing Tanggal Mulai dari JSON
        DateTime startDate = new DateTime(2026, 2, 19); // Default
        if (!string.IsNullOrEmpty(startStr)) {
            string[] s = startStr.Split('-');
            if (s.Length == 3) startDate = new DateTime(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]));
        }

        // Terapkan penyesuaian hari dari Inspector
        startDate = startDate.AddDays(startDayOffset);

        DateTime todayInCity = DateTime.UtcNow.AddHours(_cityUtcOffset).Date;
        int dayOfRamadhan = (todayInCity - startDate).Days + 1;

        if (dayOfRamadhan <= 0) {
            if (ramadhanDayText) ramadhanDayText.text = "Menunggu Ramadhan";
            _isEid = false;
        }
        else if (dayOfRamadhan > 30) { // Kembali ke 30 hari
            _isEid = true;
            ShowEidMessage();
            return;
        }
        else {
            _isEid = false;
            if (ramadhanDayText) ramadhanDayText.text = "Hari ke-" + dayOfRamadhan + " Ramadhan";
            string rowData = GetValueFromJson(_jsonRaw, cityPrefix + "_" + dayOfRamadhan);
            if (!string.IsNullOrEmpty(rowData) && rowData.Contains("|")) {
                string[] times = rowData.Split('|');
                if (times.Length >= 4) {
                    _imsak = times[0]; _subuh = times[1]; _maghrib = times[2]; _isya = times[3];
                    _sahurStart = CalculateSahurTime(_imsak);
                }
            }
        }
        UpdateUI();
        ForceUpdateStatus();
    }

    private string CalculateSahurTime(string imsakStr) {
        if (string.IsNullOrEmpty(imsakStr) || imsakStr.Length < 5) return "--:--";
        string[] parts = imsakStr.Split(':');
        int h = int.Parse(parts[0]) - 1;
        if (h < 0) h = 23;
        return (h < 10 ? "0" + h : h.ToString()) + ":" + parts[1];
    }

    private void UpdateUI() {
        if (imsakText) imsakText.text = "Imsak: " + _imsak;
        if (subuhText) subuhText.text = "Subuh: " + _subuh;
        if (maghribText) maghribText.text = "Maghrib: " + _maghrib;
        if (isyaText) isyaText.text = "Isya: " + _isya;
    }

    private void ForceUpdateStatus() { DateTime cityTime = DateTime.UtcNow.AddHours(_cityUtcOffset); _lastMinute = cityTime.Minute; UpdateStatus(cityTime.ToString("HH:mm")); }

    private void UpdateStatus(string time) {
        if (statusText == null) return;
        if (time == _maghrib) statusText.text = "<color=#FFD700>WAKTU BERBUKA!</color>";
        else if (time == _isya) statusText.text = "<color=#ADD8E6>WAKTU ISYA / TARAWIH</color>";
        else if (time == _imsak) statusText.text = "<color=#FF4500>IMSAK</color>";
        else if (IsTimeBetween(time, _sahurStart, _imsak)) statusText.text = "<color=#FFFF00>WAKTUNYA SAHUR!</color>";
        else if (IsTimeBetween(time, _subuh, _maghrib)) statusText.text = "Sedang Berpuasa";
        else statusText.text = "Waktu Istirahat / Malam";
    }

    private bool IsTimeBetween(string current, string start, string end) {
        if (start == "--:--" || end == "--:--") return false;
        return string.Compare(current, start) >= 0 && string.Compare(current, end) < 0;
    }

    private void ShowEidMessage() {
        if (ramadhanDayText) ramadhanDayText.text = "1 Syawal - Idul Fitri";
        if (imsakText) imsakText.text = "Selamat Hari Raya Idul Fitri";
        if (subuhText) subuhText.text = "Mohon Maaf Lahir & Batin";
        if (maghribText) maghribText.text = "Happy Eid Mubarak!";
        if (isyaText) isyaText.text = "";
        if (statusText) statusText.text = "<color=#FFD700>EID MUBARAK</color>";
    }

    private string GetValueFromJson(string json, string key) {
        string sKey = "\"" + key + "\"";
        int idx = json.IndexOf(sKey);
        if (idx == -1) return "";
        int startPos = json.IndexOf(":", idx);
        int start = json.IndexOf("\"", startPos + 1) + 1;
        int end = json.IndexOf("\"", start);
        if (start < 1 || end == -1) return "";
        return json.Substring(start, end - start);
    }
}
