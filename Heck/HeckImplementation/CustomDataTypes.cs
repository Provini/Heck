﻿using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.Animation;
using Heck.Deserialize;
using static Heck.HeckController;

namespace Heck;

internal class HeckObjectData : IObjectCustomData, ICopyable<IObjectCustomData>
{
    internal HeckObjectData(
        BeatmapObjectData beatmapObjectData,
        CustomData customData,
        Dictionary<string, Track> beatmapTracks,
        float bpm,
        bool v2)
    {
        try
        {
            Track = customData.GetNullableTrackArray(beatmapTracks, v2)?.ToList();
        }
        catch (Exception e)
        {
            Plugin.Log.DeserializeFailure(e, beatmapObjectData, bpm);
        }
    }

    private HeckObjectData(HeckObjectData original)
    {
        Track = original.Track;
    }

    internal IReadOnlyList<Track>? Track { get; }

    public IObjectCustomData Copy()
    {
        return new HeckObjectData(this);
    }
}

internal class HeckCoroutineEventData : ICustomEventCustomData
{
    private readonly string[] _excludedStrings = [V2_TRACK, V2_DURATION, V2_EASING, TRACK, DURATION, EASING, REPEAT];

    internal HeckCoroutineEventData(
        CustomEventData customEventData,
        Dictionary<string, List<object>> pointDefinitions,
        Dictionary<string, Track> beatmapTracks,
        bool v2)
    {
        CustomData data = customEventData.customData;

        IEnumerable<Track> tracks = data.GetTrackArray(beatmapTracks, v2);

        IEnumerable<string> propertyKeys = data.Keys.Where(n => _excludedStrings.All(m => m != n)).ToArray();
        List<CoroutineInfo> coroutineInfos = [];
        foreach (Track track in tracks)
        {
            bool path = customEventData.eventType switch
            {
                ANIMATE_TRACK => false,
                ASSIGN_PATH_ANIMATION => true,
                _ => throw new InvalidOperationException("Custom event was not of correct type.")
            };

            foreach (string propertyKey in propertyKeys)
            {
                if (!v2)
                {
                    if (path)
                    {
                        HandlePathProperty(propertyKey);
                    }
                    else
                    {
                        HandleProperty(propertyKey);
                    }
                }
                else
                {
                    if (path)
                    {
                        Track.GetPathAliases(propertyKey).Do(n => HandlePathProperty(n, propertyKey));
                    }
                    else
                    {
                        Track.GetAliases(propertyKey).Do(n => HandleProperty(n, propertyKey));
                    }
                }

                continue;

                void CreateInfo(BaseProperty property, IPropertyBuilder builder, string name, string? alias)
                {
                    CoroutineInfo coroutineInfo = new(
                        builder.GetPointData(data, alias ?? name, pointDefinitions),
                        property,
                        track);
                    coroutineInfos.Add(coroutineInfo);
                }

                void HandlePathProperty(string name, string? alias = null)
                {
                    IPropertyBuilder? builder = Track.GetPathBuilder(name);
                    if (builder == null)
                    {
                        Plugin.Log.Error($"Could not find path property [{name}]");
                        return;
                    }

                    CreateInfo(track.GetOrCreatePathProperty(name, builder), builder, name, alias);
                }

                void HandleProperty(string name, string? alias = null)
                {
                    IPropertyBuilder? builder = Track.GetBuilder(name);
                    if (builder == null)
                    {
                        Plugin.Log.Error($"Could not find property [{name}]");
                        return;
                    }

                    CreateInfo(track.GetOrCreateProperty(name, builder), builder, name, alias);
                }
            }
        }

        Duration = data.Get<float?>(v2 ? V2_DURATION : DURATION) ?? 0f;
        Easing = data.GetStringToEnum<Functions?>(v2 ? V2_EASING : EASING) ?? Functions.easeLinear;
        CoroutineInfos = coroutineInfos;

        if (!v2)
        {
            Repeat = data.Get<int?>(REPEAT) ?? 0;
        }
    }

    internal IReadOnlyList<CoroutineInfo> CoroutineInfos { get; }

    internal float Duration { get; }

    internal Functions Easing { get; }

    internal int Repeat { get; }

    internal readonly struct CoroutineInfo
    {
        internal CoroutineInfo(IPointDefinition? pointDefinition, BaseProperty property, Track track)
        {
            PointDefinition = pointDefinition;
            Property = property;
            Track = track;
        }

        internal IPointDefinition? PointDefinition { get; }

        internal BaseProperty Property { get; }

        internal Track Track { get; }
    }
}

internal class HeckInvokeEventData : ICustomEventCustomData
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    internal HeckInvokeEventData(CustomBeatmapData beatmapData, CustomEventData customEventData)
    {
        IDictionary<string, CustomEventData> eventDefinitions =
            beatmapData.customData.GetRequired<IDictionary<string, CustomEventData>>(EVENT_DEFINITIONS);
        string eventName = customEventData.customData.GetRequired<string>(EVENT);
        CustomEventData = eventDefinitions[eventName];
    }

    internal CustomEventData CustomEventData { get; }
}
