using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    [Tooltip("Label used in Addressables for ResearchData")]
    public string researchLabel = "Research";

    // Public runtime state
    public string ActiveResearchId
    {
        get => activeResearchId;
    }

    // Events
    public event Action<string> OnResearchCompleted;
    public event Action<string, float, float> OnResearchProgressChanged;
    public event Action<ResearchEffectEvent> OnEffectApplied;

    /// <summary>
    /// Fired once when research data assets are loaded
    /// </summary>
    public event Action OnResearchDataLoaded;

    // Public struct used to communicate effect payloads in a generic way
    public struct ResearchEffectEvent
    {
        public enum EffectType
        {
            UnlockStructure,
            UnlockRecipe,
            UnlockUnit,
        }

        public EffectType effectType;

        public string researchId;
        public string effectId;
        public string targetId;
    }

    // Internal representations
    private readonly Dictionary<string, ResearchData> researchById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> progressById = new(StringComparer.Ordinal);
    private readonly HashSet<string> completedSet = new(StringComparer.Ordinal);
    private string activeResearchId;

    private AsyncOperationHandle<IList<ResearchData>> loadHandle;
    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(LoadAllResearchDataAddressables());
    }

    private void OnDestroy()
    {
        if (IsLoaded && loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
            IsLoaded = false;
        }
        
    }

    /// <summary>
    /// Returns true if a research with the same researchId exists
    /// </summary>
    /// <param name="researchId"></param>
    public bool HasResearch(string researchId)
    {
        return researchById.ContainsKey(researchId);
    }

    /// <summary>
    /// Returns wether a research has been completed or not
    /// </summary>
    /// <param name="researchId">ID of the research to check</param>
    /// <returns>True if completed, false otherwise</returns>
    public bool HasCompleted(string researchId)
    {
        return completedSet.Contains(researchId);
    }

    /// <summary>
    /// Get current progress to complete the research
    /// </summary>
    /// <param name="researchId"></param>
    public float GetProgress(string researchId)
    {
        progressById.TryGetValue(researchId, out float progress);
        return progress;
    }

    /// <summary>
    /// Get the total required research points needed to complete the research
    /// </summary>
    /// <param name="researchId"></param>
    public float GetRequiredPoints(string researchId)
    {
        if (researchById.TryGetValue(researchId, out ResearchData researchData))
        {
            return researchData.requiredResearchPoints;
        }

        return 0f;
    }

    /// <summary>
    /// Set research with the same ID as active research
    /// </summary>
    /// <param name="researchId"></param>
    /// <returns>True if succesful</returns>
    public bool SetActiveResearch(string researchId)
    {
        if (string.IsNullOrEmpty(researchId))
        {
            activeResearchId = null;
            return true;
        }

        if (!researchById.TryGetValue(researchId, out ResearchData researchData))
        {
            Debug.LogWarning($"{name}: {nameof(researchId)} not found: {researchId}");
            return false;
        }

        if (!CheckPrerequisitesCompleted(researchData))
        {
            Debug.Log($"{name}: {researchData.Name}: prerequisites not met");
            return false;
        }

        if (completedSet.Contains(researchId))
        {
            return false;
        }

        activeResearchId = researchId;
        return true;
    }

    /// <summary>
    /// Returns true if the research can be selected
    /// </summary>
    /// <param name="researchId"></param>
    public bool CanSelectResearch(string researchId)
    {
        if (!researchById.TryGetValue(researchId, out ResearchData data))
        {
            return false;
        }

        return CheckPrerequisitesCompleted(data);
    }

    /// <summary>
    /// Add research progress to the requested research.
    /// If research completes, its effects are applied and OnResearchCompleted fired
    /// </summary>
    public void AddProgress(string researchId, float points)
    {
        if (string.IsNullOrEmpty(researchId))
        {
            return;
        }
        if (HasCompleted(researchId))
        {
            return;
        }
        if (!researchById.TryGetValue(researchId, out ResearchData researchData))
        {
            return;
        }

        progressById.TryGetValue(researchId, out float current);
        current = Math.Min(current + points, researchData.requiredResearchPoints);
        progressById[researchId] = current;

        OnResearchProgressChanged?.Invoke(researchId, current, researchData.requiredResearchPoints);

        if (current >= researchData.requiredResearchPoints)
        {
            CompleteResearch(researchData);
        }
    }

    /// <summary>
    /// Notify that a research effect was applied
    /// </summary>
    public void NotifyEffectApplied(ResearchEffectEvent effectEvent)
    {
        OnEffectApplied?.Invoke(effectEvent);
    }

    /// <summary>
    /// Check if all the prerequisites for the provided ResearchData are completed
    /// </summary>
    /// <param name="data">ResearchData to check</param>
    /// <returns>True if ResearchData has no prerequisites or they are all completed</returns>
    private bool CheckPrerequisitesCompleted(ResearchData data)
    {
        if (data.prerequisiteResearchDatas == null || data.prerequisiteResearchDatas.Count == 0)
        {
            return true;
        }

        foreach (ResearchData prereq in data.prerequisiteResearchDatas)
        {
            if (!completedSet.Contains(prereq.id))
            {
                return false;
            }
        }

        return true;
    }

    private void CompleteResearch(ResearchData researchData)
    {
        if (completedSet.Contains(researchData.id))
        {
            return;
        }

        Debug.Log($"Research {researchData.Name} completed");

        completedSet.Add(researchData.id);

        // Apply effects
        if (researchData.effects != null)
        {
            foreach (ResearchEffect effect in researchData.effects)
            {
                if (effect == null)
                {
                    continue;
                }
                effect.ApplyEffect(researchData.id);
            }
        }

        OnResearchCompleted?.Invoke(researchData.id);

        if (activeResearchId == researchData.id)
        {
            activeResearchId = null;
        }
    }

    /// <summary>
    /// Returns all existing ResearchData
    /// </summary>
    public IEnumerable<ResearchData> AllResearchData()
    {
        return researchById.Values;
    }

    /// <summary>
    /// Returns all completed Research IDs
    /// </summary>
    public IEnumerable<string> AllCompletedResearchIds()
    {
        return completedSet;
    }

    /// <summary>
    /// Try to fetch ResearchData by ID
    /// </summary>
    /// <param name="researchId"></param>
    /// <param name="researchData">Out parameter with the ResearchData</param>
    /// <returns>True if found</returns>
    public bool TryGetResearchData(string researchId, out ResearchData researchData)
    {
        if (string.IsNullOrEmpty(researchId))
        {
            researchData = null;
            return false;
        }
        return researchById.TryGetValue(researchId, out researchData);
    }

    private IEnumerator LoadAllResearchDataAddressables()
    {
        if (IsLoaded)
        {
            yield break;
        }

        researchById.Clear();

        loadHandle = Addressables.LoadAssetsAsync<ResearchData>(
            researchLabel,
            researchData => { }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            IList<ResearchData> results = loadHandle.Result;
            if (results != null)
            {
                foreach (ResearchData asset in results)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.id))
                    {
                        continue;
                    }

                    if (researchById.ContainsKey(asset.id))
                    {
                        Debug.LogWarning($"{name}: duplicate research ID {asset.id} found in addressables");
                        continue;
                    }

                    researchById.Add(asset.id, asset);
                }
            }
            IsLoaded = true;
            OnResearchDataLoaded?.Invoke();
        }
        else
        {
            Debug.LogWarning($"{name}: failed to load ResearchData addressables with label '{researchLabel}'");
            IsLoaded = false;
        }
    }
}