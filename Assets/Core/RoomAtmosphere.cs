#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

// Stylized visual-only lighting/contact shadows. No physics, turns or gameplay state changes.
public sealed class RoomAtmosphere : IDisposable
{
    private readonly Texture2D softTexture;
    private readonly Sprite softSprite;
    private readonly Sprite maskSprite;
    private readonly Dictionary<Entity, SpriteRenderer> shadows = new Dictionary<Entity, SpriteRenderer>();
    private readonly Dictionary<Vector2Int, Color> floorTints = new Dictionary<Vector2Int, Color>();
    private bool disposed;

    public RoomAtmosphere(Transform parent, TestRoomSetup setup)
    {
        Transform root = new GameObject("Atmosphere").transform;
        root.SetParent(parent, false);
        softTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        softTexture.name = "Runtime soft falloff";
        softTexture.filterMode = FilterMode.Bilinear;
        softTexture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float falloff = Mathf.Clamp01(1f - dx * dx - dy * dy);
                pixels[y * 64 + x] = new Color(1, 1, 1, falloff * falloff);
            }
        }
        softTexture.SetPixels(pixels);
        softTexture.Apply();
        softSprite = Sprite.Create(softTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect);

        // Keep glow pools on the interior floor instead of spilling into the outside void.
        maskSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1, 0, SpriteMeshType.FullRect);
        GameObject maskObject = new GameObject("Interior light mask");
        maskObject.transform.SetParent(root, false);
        maskObject.transform.position = new Vector3((setup.Room.Grid.Width - 1) * 0.5f,
            (setup.Room.Grid.Height - 1) * 0.5f, 0);
        maskObject.transform.localScale = new Vector3(setup.Room.Grid.Width - 2, setup.Room.Grid.Height - 2, 1);
        SpriteMask mask = maskObject.AddComponent<SpriteMask>();
        mask.sprite = maskSprite;
        mask.isCustomRangeActive = true;
        mask.backSortingOrder = 1;
        mask.frontSortingOrder = 3;

        foreach (WallProp lamp in setup.Lamps)
        {
            Vector2 position = InwardLightPosition(lamp);
            SpriteRenderer glow = MakeSoft(root, "Violet light pool", 2, new Color(0.6f, 0.2f, 1f, 0.16f));
            glow.transform.position = new Vector3(position.x, position.y, 0);
            glow.transform.localScale = new Vector3(4.2f, 4.2f, 1);
            glow.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
        shadows.Add(setup.Player, MakeSoft(root, "Hero ground shadow", 3, new Color(0.015f, 0.01f, 0.025f, 0.45f)));
        shadows.Add(setup.Enemy, MakeSoft(root, "Sentinel ground shadow", 3, new Color(0.015f, 0.01f, 0.025f, 0.45f)));
        if (setup.SecondEnemy != null)
            shadows.Add(setup.SecondEnemy, MakeSoft(root, "Chamber archer shadow", 3, new Color(0.015f, 0.01f, 0.025f, 0.45f)));

        for (int x = 0; x < setup.Room.Grid.Width; x++)
        {
            for (int y = 0; y < setup.Room.Grid.Height; y++)
            {
                Color tint = new Color(0.82f, 0.85f, 0.92f);
                foreach (WallProp lamp in setup.Lamps)
                {
                    Vector2 light = InwardLightPosition(lamp);
                    float strength = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), light) / 3.4f);
                    tint += new Color(0.1f, 0.035f, 0.2f, 0) * strength * strength;
                }
                int hash = TileHash(x, y);
                float variation = 0.97f + ((uint)hash % 7u) * 0.01f;
                floorTints.Add(new Vector2Int(x, y), new Color(Mathf.Min(1f, tint.r * variation),
                    Mathf.Min(1f, tint.g * variation), Mathf.Min(1f, tint.b * variation), 1));
            }
        }
    }

    private SpriteRenderer MakeSoft(Transform root, string name, int order, Color color)
    {
        GameObject view = new GameObject(name);
        view.transform.SetParent(root, false);
        SpriteRenderer renderer = view.AddComponent<SpriteRenderer>();
        renderer.sprite = softSprite;
        renderer.sortingOrder = order;
        renderer.color = color;
        return renderer;
    }

    private static Vector2 InwardLightPosition(WallProp lamp)
    {
        Vector2 offset = lamp.Facing == WallFacing.North ? Vector2.down : lamp.Facing == WallFacing.South ? Vector2.up :
            lamp.Facing == WallFacing.East ? Vector2.left : Vector2.right;
        return new Vector2(lamp.XPos, lamp.YPos) + offset * 0.8f;
    }

    public static int TileHash(int x, int y)
    {
        return unchecked(x * 73856093 ^ y * 19349663);
    }

    public Color FloorTint(int x, int y)
    {
        return floorTints[new Vector2Int(x, y)];
    }

    public void RefreshShadows()
    {
        foreach (KeyValuePair<Entity, SpriteRenderer> pair in shadows)
        {
            Entity entity = pair.Key;
            LivingEntity living = entity as LivingEntity;
            pair.Value.enabled = living == null || living.IsAlive;
            pair.Value.transform.position = new Vector3(entity.XPos + (entity.FootprintWidth - 1) * 0.5f,
                entity.YPos + (entity.FootprintHeight - 1) * 0.5f - entity.FootprintHeight * 0.3f, 0);
            pair.Value.transform.localScale = new Vector3(entity.FootprintWidth * 0.78f, entity.FootprintHeight * 0.28f, 1);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        UnityEngine.Object.Destroy(softSprite);
        UnityEngine.Object.Destroy(maskSprite);
        UnityEngine.Object.Destroy(softTexture);
    }
}
#endif
