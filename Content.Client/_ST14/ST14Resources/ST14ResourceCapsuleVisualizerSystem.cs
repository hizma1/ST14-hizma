using Content.Shared._ST14.ST14Resources;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._ST14.ST14Resources;

public sealed class ST14ResourceCapsuleVisualizerSystem : VisualizerSystem<ST14ResourceCapsuleVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ST14ResourceCapsuleVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, ST14ResourceCapsuleVisuals.FillFraction, out float fraction, args.Component))
            return;

        Entity<SpriteComponent?> ent = (uid, args.Sprite);
        if (!SpriteSystem.LayerMapTryGet(ent, component.Layer, out var layer, false))
            return;

        var level = ContentHelpers.RoundToLevels(Math.Clamp(fraction, 0f, 1f), 1, component.MaxFillLevels + 1);
        if (level <= 0)
        {
            SpriteSystem.LayerSetVisible(ent, layer, false);
            return;
        }

        SpriteSystem.LayerSetVisible(ent, layer, true);
        SpriteSystem.LayerSetRsiState(ent, layer, component.FillBaseName + level);

        if (AppearanceSystem.TryGetData(uid, ST14ResourceCapsuleVisuals.Color, out Color color, args.Component))
            SpriteSystem.LayerSetColor(ent, layer, color);
    }
}
