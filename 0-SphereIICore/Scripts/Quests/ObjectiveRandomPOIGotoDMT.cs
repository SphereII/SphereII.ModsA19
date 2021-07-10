using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acts as a drop-in replacement for <see cref="ObjectiveRandomPOIGoto" />, but adds the ability
/// to include or exclude different POIs by name. If you specify the names of included/excluded
/// POIs, any POIs whose name contains those strings will be included or excluded.
/// </summary>
/// <example>
/// <code>
/// &lt;objective type="RandomPOIGotoDMT, Mods" value="500-800" phase="1">
///     &lt;property name="PrefabNames" value="prefabNameContainsThis1,prefabNameContainsThis2" />
///     &lt;property name="ExcludedPrefabNames" value="prefabNameContainsThis3,prefabNameContainsThis4" />
/// &lt;/objective>
/// </code>
/// </example>
class ObjectiveRandomPOIGotoDMT : ObjectiveRandomPOIGoto
{
    private string[] IncludedPrefabNames = new string[0];

    private string[] ExcludedPrefabNames = new string[0];

    private const float _MinSearchDistance = 1000f;

    public override BaseObjective Clone()
    {
        var clone = new ObjectiveRandomPOIGotoDMT
        {
            IncludedPrefabNames = this.IncludedPrefabNames.Select(s => s).ToArray(),
            ExcludedPrefabNames = this.ExcludedPrefabNames.Select(s => s).ToArray()
        };

        CopyValues(clone);

        return clone;
    }

    protected override Vector3 GetPosition(
        EntityNPC ownerNPC = null,
        EntityPlayer entityPlayer = null,
        List<Vector2> usedPoiLocations = null,
        int entityIDforQuests = -1)
    {
        // If the quest already has a position, use it (from the parent class implementation)
        if (this.OwnerQuest.GetPositionData(
            out Vector3 dummy,
            Quest.PositionDataTypes.POIPosition))
        {
            return base.GetPosition(ownerNPC, entityPlayer, usedPoiLocations, entityIDforQuests);
        }

        EntityAlive entityAlive = (ownerNPC as EntityAlive) ??
            (this.OwnerQuest.OwnerJournal.OwnerPlayer as EntityAlive);
        
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            var worldPos = new Vector2(entityAlive.position.x, entityAlive.position.z);
            var randomPoi = FindRandomPoi(worldPos, usedPoiLocations, entityIDforQuests);

            if (randomPoi != null)
            {
                var groundPosition = new Vector2(
                    (float)randomPoi.boundingBoxPosition.x + (float)randomPoi.boundingBoxSize.x / 2f,
                    (float)randomPoi.boundingBoxPosition.z + (float)randomPoi.boundingBoxSize.z / 2f);
                
                if (groundPosition.x == -0.1f && groundPosition.y == -0.1f)
                {
                    Debug.Log("ObjectiveRandomPOIGotoDMT: No POI found.");
                    return Vector3.zero;
                }

                var x = (int)groundPosition.x;
                var y = (int)GameManager.Instance.World.GetHeightAt(groundPosition.x, groundPosition.y);
                var z = (int)groundPosition.y;
                
                this.position = new Vector3((float)x, (float)y, (float)z);
                Debug.Log("ObjectiveRandomPOIGotoDMT: POI " + randomPoi.name + " found at " + this.position.ToString());
                
                if (GameManager.Instance.World.IsPositionInBounds(this.position))
                {
                    this.OwnerQuest.Position = this.position;
                    
                    this.FinalizePoint(
                        new Vector3(
                            (float)randomPoi.boundingBoxPosition.x,
                            (float)randomPoi.boundingBoxPosition.y,
                            (float)randomPoi.boundingBoxPosition.z),
                        new Vector3(
                            (float)randomPoi.boundingBoxSize.x,
                            (float)randomPoi.boundingBoxSize.y,
                            (float)randomPoi.boundingBoxSize.z));
                    
                    this.OwnerQuest.QuestPrefab = randomPoi;
                    
                    this.OwnerQuest.DataVariables.Add("POIName", base.OwnerQuest.QuestPrefab.location.Name);
                    
                    if (usedPoiLocations != null)
                    {
                        usedPoiLocations.Add(new Vector2(
                            (float)randomPoi.boundingBoxPosition.x,
                            (float)randomPoi.boundingBoxPosition.z));
                    }
                    return this.position;
                }
            }
            else
            {
                Debug.Log("ObjectiveRandomPOIGotoDMT: No random POI found.");
            }
        }
        else
        {
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                NetPackageManager.GetPackage<NetPackageQuestGotoPoint>().Setup(
                    entityAlive.entityId,
                    this.OwnerQuest.QuestTags,
                    this.OwnerQuest.QuestCode,
                    NetPackageQuestGotoPoint.QuestGotoTypes.RandomPOI,
                    this.OwnerQuest.QuestClass.DifficultyTier,
                    0, -1, 0f, 0f, 0f, -1f,
                    biomeFilterType,
                    biomeFilter),
                false);
            
            this.CurrentValue = 1;
        }
        return Vector3.zero;
    }

    private PrefabInstance FindRandomPoi(
        Vector2 worldPos,
        List<Vector2> usedPoiLocations,
        int entityIDforQuests)
    {
        var poiPrefabs = GetMatchingPrefabs(worldPos, 4000000f, usedPoiLocations, entityIDforQuests);

        if (poiPrefabs.Count < 1)
        {
            // Try again, with max search distance of -1 meaning "anywhere in the world"
            poiPrefabs = GetMatchingPrefabs(worldPos, -1f, usedPoiLocations, entityIDforQuests);
        }

        if (poiPrefabs.Count < 1)
        {
            // Nothing matches in the entire world
            return null;
        }

        int index = GameManager.Instance.World.GetGameRandom().RandomRange(poiPrefabs.Count);
        
        return poiPrefabs[index];
    }

    // Modified and refactored from DynamicPrefabDecorator.GetRandomPOINearWorldPos
    private List<PrefabInstance> GetMatchingPrefabs(
        Vector2 worldPos,
        float maxSearchDistance,
        List<Vector2> usedPoiLocations,
        int entityIDforQuests)
    {
        var questTag = base.OwnerQuest.QuestTags;
        var difficulty = base.OwnerQuest.QuestClass.DifficultyTier;
        var matchingPrefabs = new List<PrefabInstance>();

        List<PrefabInstance> poiPrefabs;
        // If difficulty tier is "none," choose from all prefabs
        if (difficulty > 0)
        {
            poiPrefabs = QuestEventManager.Current.GetPrefabsByDifficultyTier((int)difficulty);
        }
        else
        {
            poiPrefabs = GameManager.Instance.World.ChunkClusters[0].ChunkProvider
                .GetDynamicPrefabDecorator()
                .GetPOIPrefabs();
        }

        for (int i = 0; i < poiPrefabs.Count; i++)
        {
            var prefabInstance = poiPrefabs[i];

            // Note: the check for prefabInstance.prefab.bSleeperVolumes means this will only go
            // to POIs with sleeper volumes. So, this won't work for going to a random POI that
            // does not have sleepers (intentionally or not). I left this check in, only because
            // I believe it guards against POIs that have problems with sleeper volumes. [Karl]
            if (prefabInstance.prefab.bSleeperVolumes &&
                MatchesQuestTag(prefabInstance, questTag) &&
                MatchesDifficultyTier(prefabInstance, difficulty) &&
                IncludePrefab(prefabInstance) &&
                !ExcludePrefab(prefabInstance))
            {
                var poiLocation = new Vector2(
                    (float)prefabInstance.boundingBoxPosition.x,
                    (float)prefabInstance.boundingBoxPosition.z);
                
                var lockoutReason = QuestEventManager.Current.CheckForPOILockouts(
                    entityIDforQuests,
                    poiLocation);
                
                if (lockoutReason != QuestEventManager.POILockoutReasonTypes.None)
                {
                    Log.Out(
                        "Quest POI Locked Out: " +
                        prefabInstance.location.Name +
                        " for " +
                        lockoutReason.ToStringCached<QuestEventManager.POILockoutReasonTypes>());
                    
                    continue;
                }

                if (MatchesBiome(poiLocation, biomeFilterType, biomeFilter) &&
                    InSearchDistance(prefabInstance, worldPos, maxSearchDistance))
                {
                    matchingPrefabs.Add(prefabInstance);
                }
            }
        }

        // Try to find only unused POIs, but if there are none, settle for used POIs
        if (usedPoiLocations != null)
        {
            var unusedPrefabs = matchingPrefabs.FindAll(prefab =>
                !usedPoiLocations.Contains(new Vector2(
                    (float)prefab.boundingBoxPosition.x,
                    (float)prefab.boundingBoxPosition.z)));

            if (unusedPrefabs.Count > 0)
                matchingPrefabs = unusedPrefabs;
        }

        return matchingPrefabs;
    }

    private static bool MatchesQuestTag(PrefabInstance prefabInstance, QuestTags questTag)
    {
        // Match if no quest tag specified
        return questTag == QuestTags.none || prefabInstance.prefab.GetQuestTag(questTag);
    }

    private static bool MatchesDifficultyTier(PrefabInstance prefabInstance, byte difficulty)
    {
        // Match if no difficulty specified
        return difficulty == 0 || prefabInstance.prefab.DifficultyTier == difficulty;
    }

    private static bool MatchesBiome(
        Vector2 poiLocation,
        BiomeFilterTypes biomeFilterType,
        string biomeFilter)
    {
        // Technically we don't need this check, but it bypasses the remaining computation
        if (biomeFilterType == BiomeFilterTypes.AnyBiome)
            return true;

        var biomeName = GameManager.Instance.World.ChunkCache.ChunkProvider
                .GetBiomeProvider()
                .GetBiomeAt((int)poiLocation.x, (int)poiLocation.y)
                .m_sBiomeName;
            
        if (biomeFilterType == BiomeFilterTypes.OnlyBiome && biomeName != biomeFilter)
        {
            return false;
        }

        if (biomeFilterType == BiomeFilterTypes.ExcludeBiome)
        {
            var excludedBiomes = biomeFilter.Split(new char[] { ',' });

            for (int j = 0; j < excludedBiomes.Length; j++)
            {
                if (biomeName == excludedBiomes[j])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool InSearchDistance(
        PrefabInstance prefabInstance,
        Vector2 worldPos,
        float maxSearchDistance)
    {
        var groundPos = new Vector2(
            (float)prefabInstance.boundingBoxPosition.x + (float)prefabInstance.boundingBoxSize.x / 2f,
            (float)prefabInstance.boundingBoxPosition.z + (float)prefabInstance.boundingBoxSize.z / 2f);
                
        float sqrMagnitude = (worldPos - groundPos).sqrMagnitude;

        // A negative max search distance means "anywhere in the world, no matter how distant"
        if (maxSearchDistance < 0)
            return sqrMagnitude > _MinSearchDistance;
        
        return (sqrMagnitude < maxSearchDistance && sqrMagnitude > _MinSearchDistance);
    }

    private bool IncludePrefab(PrefabInstance prefabInstance)
    {
        if (IncludedPrefabNames.Length > 0)
        {
            return IncludedPrefabNames.Any(name =>
                prefabInstance.name.ToLower().Contains(name.ToLower()));
        }

        return true;
    }

    private bool ExcludePrefab(PrefabInstance prefabInstance)
    {
        if (ExcludedPrefabNames.Length > 0)
        {
            return ExcludedPrefabNames.Any(name =>
                prefabInstance.name.ToLower().Contains(name.ToLower()));
        }

        return false;
    }

    public override void ParseProperties(DynamicProperties properties)
    {
        // Deprectated; for compatibility with SphereIICore's ObjectiveGotoPOISDX class
        if (properties.Values.ContainsKey("PrefabName"))
        {
            IncludedPrefabNames = new string[] { properties.Values["PrefabName"] };
        }

        if (properties.Values.ContainsKey("PrefabNames"))
        {
            IncludedPrefabNames = SplitString(properties.Values["PrefabNames"]).ToArray();
        }

        if (properties.Values.ContainsKey("ExcludedPrefabNames"))
        {
            ExcludedPrefabNames = SplitString(properties.Values["ExcludedPrefabNames"])
                // PrefabNames takes precedence over ExcludedPrefabNames
                .Where(excl => !IncludedPrefabNames.Any(incl => incl.ToLower() == excl.ToLower()))
                .ToArray();
        }

        base.ParseProperties(properties);
    }

    private static IEnumerable<string> SplitString(string str)
    {
        return str
            .Split(new char[] {','})
            .Select(s => s.Trim())
            .Where(s => !String.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
