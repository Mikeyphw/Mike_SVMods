using System;
using System.Collections.Generic;
using System.Linq;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Default builder that copies only a known-safe whitelist of fields from the source snapshot.
/// </summary>
public sealed class CloneObjectDataBuilder : ICloneObjectDataBuilder
{
    public CloneObjectData Build(CloneObjectDataRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.Spec is null) throw new ArgumentNullException(nameof(request.Spec));
        if (request.Source is null) throw new ArgumentNullException(nameof(request.Source));

        var o = request.Overrides;

        var name = o?.Name ?? request.Source.Name;
        var description = o?.Description ?? request.Source.Description;

        var texture = o?.TextureAssetName ?? request.Source.TextureAssetName ?? string.Empty;
        var spriteIndex = o?.SpriteIndex ?? request.Source.SpriteIndex;

        var tags = MergeTags(request.Source.ContextTags, o?.AdditionalContextTags);
        var fields = MergeFields(request.Source.CustomFields, o?.AdditionalCustomFields);

        return new CloneObjectData(
            Id: request.Spec.CloneItemId,
            Name: name,
            Description: description,
            Price: request.Source.Price,
            Edibility: request.Source.Edibility,
            Category: request.Source.Category,
            ContextTags: tags,
            CustomFields: fields,
            TextureAssetName: texture,
            SpriteIndex: spriteIndex
        );
    }

    private static IReadOnlyList<string> MergeTags(IReadOnlyList<string>? source, IReadOnlyList<string>? add)
    {
        var list = new List<string>();
        if (source is not null)
            list.AddRange(source.Where(static s => !string.IsNullOrWhiteSpace(s)));

        if (add is not null)
            list.AddRange(add.Where(static s => !string.IsNullOrWhiteSpace(s)));

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, string> MergeFields(
        IReadOnlyDictionary<string, string>? source,
        IReadOnlyDictionary<string, string>? add)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (source is not null)
        {
            foreach (var (k, v) in source)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                dict[k] = v ?? string.Empty;
            }
        }

        if (add is not null)
        {
            foreach (var (k, v) in add)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                dict[k] = v ?? string.Empty;
            }
        }

        return dict;
    }
}
