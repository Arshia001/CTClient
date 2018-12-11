using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    const float Accel = -240;
    const float InitSpeed = 1440;
    const float SpinTime = 6;


    public bool IsMultiplier;
    public AudioClip StartClip, LoopClip, EndClip;
    AudioSource StartAS, LoopAS, EndAS;

    Dictionary<int, List<Transform>> EntriesByID = new Dictionary<int, List<Transform>>();

    float? TargetRotation;
    float RotationStartTime;
    bool IsSpinning;
    bool IsSlowingDown;
    public event System.Action OnFinishedEvent;

    void Start()
    {
        int Index = 0;

        if (IsMultiplier)
            foreach (var KV in TransientData.Instance.SpinMultipliers.OrderBy(kv => kv.Value.Multiplier))
            {
                for (int i = 0; i < KV.Value.Chance; ++i)
                {
                    var T = transform.Find(Index++.ToString());
                    if (T == null)
                    {
                        Debug.LogError("Less spinner containers than config entries");
                        return;
                    }
                    T.GetComponent<TextMeshProUGUI>().text = $"{KV.Value.Multiplier}X";

                    List<Transform> L;
                    if (!EntriesByID.TryGetValue(KV.Key, out L))
                    {
                        L = new List<Transform>();
                        EntriesByID[KV.Key] = L;
                    }
                    L.Add(T);
                }
            }
        else
            foreach (var KV in TransientData.Instance.SpinRewards.OrderBy(kv => (kv.Value.RewardType == CurrencyType.Gem ? 100000 : 0) + kv.Value.Count))
            {
                for (int i = 0; i < KV.Value.Chance; ++i)
                {
                    var T = transform.Find(Index++.ToString());
                    if (T == null)
                    {
                        Debug.LogError("Less spinner containers than config entries");
                        return;
                    }
                    T.GetComponent<TextMeshProUGUI>().text = $"{KV.Value.Count}{(KV.Value.RewardType == CurrencyType.Gem ? "<sprite=0>" : "<sprite=1>")}";

                    List<Transform> L;
                    if (!EntriesByID.TryGetValue(KV.Key, out L))
                    {
                        L = new List<Transform>();
                        EntriesByID[KV.Key] = L;
                    }
                    L.Add(T);
                }
            }

        StartAS = gameObject.AddComponent<AudioSource>();
        StartAS.clip = StartClip;
        StartAS.bypassEffects = StartAS.bypassListenerEffects = StartAS.bypassReverbZones = true;
        StartAS.loop = false;
        LoopAS = gameObject.AddComponent<AudioSource>();
        LoopAS.clip = LoopClip;
        LoopAS.bypassEffects = LoopAS.bypassListenerEffects = LoopAS.bypassReverbZones = true;
        LoopAS.loop = true;
        EndAS = gameObject.AddComponent<AudioSource>();
        EndAS.clip = EndClip;
        EndAS.bypassEffects = EndAS.bypassListenerEffects = EndAS.bypassReverbZones = true;
        EndAS.loop = false;
    }

    public void StartSpinning()
    {
        IsSpinning = true;
        TargetRotation = null;

        StartAS.Play();
        LoopAS.PlayDelayed(StartClip.length);
    }

    public void SpinTo(int ConfigID)
    {
        var L = EntriesByID[ConfigID];

        var T = L[Random.Range(0, L.Count)];

        var RandomRange = 180.0f / transform.childCount * 0.8f;
        TargetRotation = -T.localRotation.eulerAngles.z + Random.Range(-RandomRange, RandomRange);
        RotationStartTime = Time.time;
        IsSpinning = true;
        IsSlowingDown = false;
    }

    void OnEnable()
    {
        StartAS?.Stop();
        LoopAS?.Stop();
        EndAS?.Stop();

        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!IsSpinning)
            return;

        if (TargetRotation.HasValue && IsSlowingDown)
        {
            var T = Time.time - RotationStartTime;
            if (T > SpinTime)
            {
                T = SpinTime;
                IsSpinning = false;
                OnFinishedEvent?.Invoke();
            }

            transform.localRotation = Quaternion.Euler(0, 0, 0.5f * Accel * T * T + InitSpeed * T + TargetRotation.Value);
        }
        else if (TargetRotation.HasValue && Time.time - RotationStartTime > 1.0f)
        {
            transform.localRotation = Quaternion.Euler(0, 0, TargetRotation.Value);
            RotationStartTime = Time.time;
            IsSlowingDown = true;

            StartAS.Stop();
            LoopAS.Stop();
            EndAS.Play();
        }
        else
        {
            transform.Rotate(0, 0, InitSpeed * Time.deltaTime);
        }
    }
}
