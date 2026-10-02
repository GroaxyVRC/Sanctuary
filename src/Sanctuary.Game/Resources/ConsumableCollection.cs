using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using Sanctuary.Core.Collections;
using Sanctuary.Game.Resources.Definitions;

namespace Sanctuary.Game.Resources;

public class ConsumableCollection
{
    private readonly ILogger _logger;

    public ObservableConcurrentDictionary<int, BoomboxDefinition> Boomboxes { get; } = new();
    public ObservableConcurrentDictionary<int, CakeItemDefinition> Cakes { get; } = new();
    public ObservableConcurrentDictionary<int, FoodEffectDefinition> FoodEffects { get; } = new();
    public ObservableConcurrentDictionary<int, TransformAbilityDefinition> Transformations { get; } = new();
    public ObservableConcurrentDictionary<int, RandomTransformFoodDefinition> RandomTransformFoods { get; } = new();
    public ObservableConcurrentDictionary<int, PartyFavorDefinition> PartyFavors { get; } = new();

    public ConsumableCollection(ILogger logger)
    {
        _logger = logger;
    }

    public bool Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("Failed to find file \"{file}\"", filePath);
            return false;
        }

        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Converters = { new JsonStringEnumConverter() } // parse CakeItemType from strings ("BossCake"/"ScaredyCake")
            };

            using var jsonDocument = JsonDocument.Parse(fileStream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            });

            var consumables = new ConsumableDefinitions();
            JsonElement boomboxDefinitions = default;

            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                switch (property.Name.ToLowerInvariant())
                {
                    case "boomboxes":
                        boomboxDefinitions = property.Value;
                        break;
                    case "cakes":
                        consumables.Cakes = property.Value.Deserialize<List<CakeItemDefinition>>(jsonSerializerOptions) ?? [];
                        break;
                    case "foodeffects":
                        consumables.FoodEffects = property.Value.Deserialize<List<FoodEffectDefinition>>(jsonSerializerOptions) ?? [];
                        break;
                    case "transformations":
                        consumables.Transformations = property.Value.Deserialize<List<TransformAbilityDefinition>>(jsonSerializerOptions) ?? [];
                        break;
                    case "randomtransformfoods":
                        consumables.RandomTransformFoods = property.Value.Deserialize<List<RandomTransformFoodDefinition>>(jsonSerializerOptions) ?? [];
                        break;
                    case "partyfavors":
                        consumables.PartyFavors = property.Value.Deserialize<List<PartyFavorDefinition>>(jsonSerializerOptions) ?? [];
                        break;
                }
            }

            if (boomboxDefinitions.ValueKind == JsonValueKind.Array)
            {
                foreach (var boomboxDefinition in boomboxDefinitions.EnumerateArray())
                {
                    try
                    {
                        var entry = boomboxDefinition.Deserialize<BoomboxDefinition>(jsonSerializerOptions);

                        if (entry is not null)
                            consumables.Boomboxes.Add(entry);
                        else
                            _logger.LogError("Invalid null boombox definition in file \"{file}\".", filePath);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse boombox definition in file \"{file}\". Entry={entry}", filePath, boomboxDefinition.GetRawText());
                    }
                }
            }
            else if (boomboxDefinitions.ValueKind != JsonValueKind.Undefined)
            {
                _logger.LogError("Invalid boombox definitions in file \"{file}\".", filePath);
            }

            foreach (var entry in consumables.Boomboxes)
            {
                if (entry.ItemId <= 0 || entry.EffectIds is null || entry.DanceSequence is null
                    || entry.DanceDurationsMs is null || entry.IndependentDanceDurationsMs is null
                    || !float.IsFinite(entry.Range) || entry.Range <= 0 || entry.DurationMs <= 0
                    || !float.IsFinite(entry.SpawnOffset) || entry.ModelId <= 0
                    || entry.DanceBlendMs < 0 || entry.TransformReapplyDelayMs < 0
                    || ((entry.SynchronizedDances || entry.StandingDanceAnimationId == 0) && entry.DanceDurationsMs.Count == 0)
                    || (entry.DanceDurationsMs.Count > 0 && entry.DanceSequence.Length == 0)
                    || entry.DanceDurationsMs.Values.Any(d => d is null || d.Length != entry.DanceSequence.Length || d.Any(ms => ms <= 0))
                    || entry.IndependentDanceDurationsMs.Values.Any(d => d is null || d.Count == 0 || d.Any(clip => clip.Key <= 0 || clip.Value <= 0)))
                {
                    _logger.LogError("Invalid boombox definition. ItemId={id} \"{file}\"", entry.ItemId, filePath);
                    continue;
                }

                if (!Boomboxes.TryAdd(entry.ItemId, entry))
                {
                    _logger.LogWarning("Failed to add Boombox entry. ItemId={id} \"{file}\"", entry.ItemId, filePath);
                    continue;
                }
            }
            _logger.LogInformation("Loaded {count} Boombox definitions.", Boomboxes.Count);

            foreach (var entry in consumables.FoodEffects)
            {
                if (!FoodEffects.TryAdd(entry.AbilityId, entry))
                {
                    _logger.LogWarning("Failed to add FoodEffect entry. AbilityId={id} \"{file}\"", entry.AbilityId, filePath);
                    return false;
                }
            }
            _logger.LogInformation("Loaded {count} FoodEffect definitions.", FoodEffects.Count);

            foreach (var entry in consumables.Cakes)
            {
                if (!Cakes.TryAdd(entry.ItemId, entry))
                {
                    _logger.LogWarning("Failed to add Cake entry. ItemId={id} \"{file}\"", entry.ItemId, filePath);
                    return false;
                }
            }
            _logger.LogInformation("Loaded {count} Cake definitions.", Cakes.Count);

            foreach (var entry in consumables.Transformations)
            {
                if (!Transformations.TryAdd(entry.AbilityId, entry))
                {
                    _logger.LogWarning("Failed to add Transformation entry. AbilityId={id} \"{file}\"", entry.AbilityId, filePath);
                    return false;
                }
            }
            _logger.LogInformation("Loaded {count} Transformation definitions.", Transformations.Count);

            foreach (var entry in consumables.RandomTransformFoods)
            {
                if (!RandomTransformFoods.TryAdd(entry.ItemId, entry))
                {
                    _logger.LogWarning("Failed to add RandomTransformFood entry. ItemId={id} \"{file}\"", entry.ItemId, filePath);
                    return false;
                }
            }
            _logger.LogInformation("Loaded {count} RandomTransformFood definitions.", RandomTransformFoods.Count);

            foreach (var entry in consumables.PartyFavors)
            {
                if (!PartyFavors.TryAdd(entry.ItemId, entry))
                {
                    _logger.LogWarning("Failed to add PartyFavor entry. ItemId={id} \"{file}\"", entry.ItemId, filePath);
                    return false;
                }
            }
            _logger.LogInformation("Loaded {count} PartyFavor definitions.", PartyFavors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse file \"{file}\".", filePath);
            return false;
        }

        if (Boomboxes.Count == 0 && FoodEffects.Count == 0 && Transformations.Count == 0 && Cakes.Count == 0
            && RandomTransformFoods.Count == 0 && PartyFavors.Count == 0)
        {
            _logger.LogError("No data was loaded from \"{file}\"", filePath);
            return false;
        }

        return true;
    }
}
