using System;
using System.Collections.Generic;

public enum SkeletonAction { Idle, Walk, Attack, Hit, Death, Shoot }

// Cosmetic clock only: advancing frames cannot move, attack or spend gameplay turns.
public sealed class SkeletonAnimationTimeline
{
    public static SkeletonAction AttackAction(bool ranged, bool adjacent)
    {
        return ranged && !adjacent ? SkeletonAction.Shoot : SkeletonAction.Attack;
    }
    private readonly Queue<SkeletonAction> pending = new Queue<SkeletonAction>();
    public SkeletonAction Action { get; private set; } = SkeletonAction.Idle;
    public int Frame { get; private set; }
    public bool Busy { get { return Action != SkeletonAction.Idle && !(Action == SkeletonAction.Death && Frame == 3); } }
    private double elapsed;
    private bool dying;

    public static double FrameSeconds(SkeletonAction action)
    {
        return action == SkeletonAction.Idle ? 0.22 : action == SkeletonAction.Hit ? 0.08 : 0.12;
    }

    public void Play(SkeletonAction action)
    {
        if (dying || action == SkeletonAction.Idle) return;
        if (action == SkeletonAction.Death)
        {
            dying = true;
            pending.Clear();
            Begin(action);
        }
        else if (!Busy) Begin(action);
        else if (pending.Count < 8) pending.Enqueue(action);
    }

    public void Advance(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (dying && Frame == 3) return;
        elapsed += seconds;
        while (elapsed >= FrameSeconds(Action))
        {
            elapsed -= FrameSeconds(Action);
            if (Frame < 3) { Frame++; continue; }
            if (dying) { elapsed = 0; return; }
            if (Action == SkeletonAction.Idle) Frame = 0;
            else if (pending.Count > 0) { Action = pending.Dequeue(); Frame = 0; }
            else { Action = SkeletonAction.Idle; Frame = 0; }
        }
    }

    private void Begin(SkeletonAction action) { Action = action; Frame = 0; elapsed = 0; }
}
