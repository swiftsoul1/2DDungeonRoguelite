#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;

public sealed class SkeletonSpriteAnimator : IDisposable
{
    private readonly Enemy enemy;
    private readonly SpriteRenderer view;
    private readonly Sprite[,] frames;
    private readonly LivingEntity target;
    private readonly bool hasShotRow;
    private readonly SkeletonAnimationTimeline timeline = new SkeletonAnimationTimeline();
    private readonly Sprite idleLegs;
    private readonly Sprite idleTorso;
    private readonly SpriteRenderer torsoView;
    private static readonly float[] IdleSway = { 0f, -0.65f, 0f, 0.65f };
    // Archer source rows have different ground lines; register feet without resizing poses.
    private static readonly float[] ArcherGroundPivots = { 0.02f, 0.04f, 0.09f, 0.09f, 0.08f, 0.19f };
    private int lastX, lastY;
    private Vector3 walkFrom;
    private float walkElapsed = 1;
    public bool Busy { get { return timeline.Busy; } }

    public SkeletonSpriteAnimator(Enemy enemy, SpriteRenderer view, Texture2D atlas, bool hasShotRow = false, LivingEntity target = null)
    {
        this.enemy = enemy; this.view = view;
        this.target = target; this.hasShotRow = hasShotRow;
        int rows = hasShotRow ? 6 : 5;
        frames = new Sprite[rows, 4];
        Texture2D daggerPatch = hasShotRow ? Resources.Load<Texture2D>("DungeonPrototype/Art/skeleton-archer-dagger-patch-v2") : null;
        if (daggerPatch != null && (daggerPatch.width != atlas.width || daggerPatch.height != atlas.height))
        {
            Debug.LogWarning("Archer dagger patch size differs from original atlas; retaining original frames.");
            daggerPatch = null;
        }
        lastX = enemy.XPos; lastY = enemy.YPos;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < 4; col++)
            {
                int left = col * atlas.width / 4, right = (col + 1) * atlas.width / 4;
                int bottom = (rows - 1 - row) * atlas.height / rows, top = (rows - row) * atlas.height / rows;
                float groundPivot = hasShotRow ? ArcherGroundPivots[row] : 0.08f;
                // Isolate the revision to the four melee poses and third death pose.
                // Keep all other approved pixels, especially the anchored idle source.
                Texture2D source = daggerPatch != null && (row == 2 || (row == 4 && col == 2)) ? daggerPatch : atlas;
                frames[row, col] = Sprite.Create(source, new Rect(left, bottom, right - left, top - bottom),
                    new Vector2(0.5f, groundPivot), (right - left) / 1.35f, 0, SpriteMeshType.FullRect);
            }
        // Reuse one registered pose for every idle step. Generated idle frames
        // drifted at the feet; only the torso now sways around a fixed hip seam.
        Rect idleRect = frames[0, 0].rect;
        int legHeight = Mathf.RoundToInt(idleRect.height * 0.34f);
        float pixelsPerUnit = frames[0, 0].pixelsPerUnit;
        float idlePivot = hasShotRow ? ArcherGroundPivots[0] : 0.08f;
        idleLegs = Sprite.Create(atlas, new Rect(idleRect.x, idleRect.y, idleRect.width, legHeight),
            new Vector2(0.5f, idleRect.height * idlePivot / legHeight), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        idleTorso = Sprite.Create(atlas, new Rect(idleRect.x, idleRect.y + legHeight, idleRect.width, idleRect.height - legHeight),
            new Vector2(0.5f, 0), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        var torsoObject = new GameObject("Idle upper-body sway");
        torsoObject.transform.SetParent(view.transform, false);
        torsoObject.transform.localPosition = new Vector3(0, (legHeight - idleRect.height * idlePivot) / pixelsPerUnit, 0);
        torsoView = torsoObject.AddComponent<SpriteRenderer>();
        torsoView.sprite = idleTorso;
        enemy.AttackPerformed += OnAttack;
        enemy.DamageReceived += OnDamage;
        view.sprite = frames[0, 0];
        Tick(0);
    }

    private void OnAttack(LivingEntity actor)
    {
        bool adjacent = target == null || actor.Grid == null || actor.Grid.AreAdjacent(actor, target);
        timeline.Play(SkeletonAnimationTimeline.AttackAction(hasShotRow && enemy.IsRanged, adjacent));
    }
    private void OnDamage(LivingEntity actor) { timeline.Play(actor.IsAlive ? SkeletonAction.Hit : SkeletonAction.Death); }

    public void Tick(float seconds)
    {
        if (enemy.XPos != lastX || enemy.YPos != lastY)
        {
            walkFrom = Ground(lastX, lastY);
            walkElapsed = 0;
            lastX = enemy.XPos; lastY = enemy.YPos;
            timeline.Play(SkeletonAction.Walk);
        }
        timeline.Advance(seconds);
        bool idle = timeline.Action == SkeletonAction.Idle;
        view.sprite = idle ? idleLegs : frames[(int)timeline.Action, timeline.Frame];
        torsoView.enabled = idle;
        if (idle)
        {
            torsoView.transform.localRotation = Quaternion.Euler(0, 0, IdleSway[timeline.Frame]);
            torsoView.color = view.color;
            torsoView.sortingLayerID = view.sortingLayerID;
            torsoView.sortingOrder = 11;
        }
        view.enabled = true; // Final death pose remains decorative; gameplay already frees the cell.
        view.transform.localScale = new Vector3(enemy.FootprintWidth, enemy.FootprintHeight, 1);
        walkElapsed += seconds;
        Vector3 destination = Ground(enemy.XPos, enemy.YPos);
        view.transform.position = walkElapsed < 0.48f ? Vector3.Lerp(walkFrom, destination, walkElapsed / 0.48f) : destination;
        view.sortingOrder = enemy.IsAlive ? 10 : 4;
    }

    private Vector3 Ground(int x, int y)
    {
        return new Vector3(x + (enemy.FootprintWidth - 1) * 0.5f,
            y + (enemy.FootprintHeight - 1) * 0.5f - enemy.FootprintHeight * 0.35f, 0);
    }

    public void Dispose()
    {
        enemy.AttackPerformed -= OnAttack;
        enemy.DamageReceived -= OnDamage;
        foreach (Sprite frame in frames) if (frame != null) UnityEngine.Object.Destroy(frame);
        UnityEngine.Object.Destroy(idleLegs);
        UnityEngine.Object.Destroy(idleTorso);
        if (torsoView != null) UnityEngine.Object.Destroy(torsoView.gameObject);
    }
}
#endif
