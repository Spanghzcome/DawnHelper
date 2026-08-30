using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/statePickupController")]
[Tracked]

public class StatePickupController : Entity
{
    private readonly bool Swim;
    private readonly bool StarFly;
    private readonly bool RedDash;
    private readonly bool SummitLaunch;
    private readonly bool persistent;
    private readonly bool resetToNormal;
    
    public StatePickupController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Swim = data.Bool("pickupInSwim", true);
        StarFly = data.Bool("pickupInFeather", true);
        RedDash = data.Bool("pickupInRedBooster", true);
        SummitLaunch = data.Bool("pickupInSummitLaunch", true);
        persistent = data.Bool("persistent", true);
        resetToNormal = data.Bool("resetToNormal", false);
        
        if (persistent)
            Tag = Tags.Persistent;
    }

    public static void Load()
    {
        On.Celeste.Player.SwimUpdate += SwimUpdateHook;
        On.Celeste.Player.StarFlyUpdate += StarFlyUpdateHook;
        On.Celeste.Player.RedDashUpdate += RedDashUpdateHook;
        On.Celeste.Player.SummitLaunchUpdate += SummitLaunchUpdateHook;
    }

    public static void Unload()
    {
        On.Celeste.Player.SwimUpdate -= SwimUpdateHook;
        On.Celeste.Player.StarFlyUpdate -= StarFlyUpdateHook;
        On.Celeste.Player.RedDashUpdate -= RedDashUpdateHook;
        On.Celeste.Player.SummitLaunchUpdate -= SummitLaunchUpdateHook; 
    }

    private static int SwimUpdateHook(On.Celeste.Player.orig_SwimUpdate orig, Player self)
    {
        if (self.Scene.Tracker.GetEntity<StatePickupController>() is { Swim: true })
        {
            if (self.Holding == null)
            {
                if (Input.GrabCheck && !self.IsTired && self.CanUnDuck)
                {
                    foreach (Holdable h in self.Scene.Tracker.GetComponents<Holdable>())
                    {
                        if (h.Check(self) && self.Pickup(h))
                        {
                            return 8;
                        }
                    }
                }
            }
            else
            {
                if (!Input.GrabCheck && self.minHoldTimer <= 0f)
                {
                    self.Throw();
                }
            }
        }

        return orig(self);
    }

    private static int StarFlyUpdateHook(On.Celeste.Player.orig_StarFlyUpdate orig, Player self)
    {
        if (self.Scene.Tracker.GetEntity<StatePickupController>() is { StarFly: true } controller)
        {
            if (self.Holding == null)
            {
                if (Input.GrabCheck && !self.IsTired && self.CanUnDuck)
                {
                    foreach (Holdable h in self.Scene.Tracker.GetComponents<Holdable>())
                    {
                        if (h.Check(self) && self.Pickup(h))
                        {
                            if (controller.resetToNormal)
                                return 8;
                            
                            self.Collider = self.starFlyHitbox;
                            self.hurtbox = self.starFlyHurtbox;
                            self.Play("event:/char/madeline/crystaltheo_lift");
                            Vector2 begin = self.Holding.Entity.Position - self.Position;
                            Vector2 carryOffsetTarget = new Vector2(0f, -12f);
                            SimpleCurve curve =
                                new SimpleCurve(
                                    control: new Vector2(begin.X + Math.Sign(begin.X) * 2, carryOffsetTarget.Y - 2f),
                                    begin: begin, end: carryOffsetTarget);
                            self.carryOffset = begin;
                            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, start: true);
                            tween.OnUpdate = delegate(Tween t) { self.carryOffset = curve.GetPoint(t.Eased); };
                            self.Add(tween);
                        }
                    }
                }
            }
            else
            {
                if (!Input.GrabCheck && self.minHoldTimer <= 0f)
                {
                    if (self.Holding != null)
                    {
                        if (Input.MoveY.Value == 1)
                        {
                            self.Drop();
                        }
                        else
                        {
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                            self.Holding.Release(Vector2.UnitX * (float)self.Facing);
                            self.Play("event:/char/madeline/crystaltheo_throw");
                        }

                        self.Holding = null;
                    }
                }
            }
        }

        return orig(self);
    }

    private static int RedDashUpdateHook(On.Celeste.Player.orig_RedDashUpdate orig, Player self)
    {
        if (self.Scene.Tracker.GetEntity<StatePickupController>() is { RedDash: true } controller)
        {
            if (self.Holding == null)
            {
                if (Input.GrabCheck && !self.IsTired && self.CanUnDuck)
                {
                    foreach (Holdable h in self.Scene.Tracker.GetComponents<Holdable>())
                    {
                        if (h.Check(self) && self.Pickup(h))
                        {
                            if (controller.resetToNormal)
                                return 8;
                            
                            self.Play("event:/char/madeline/crystaltheo_lift");
                            Vector2 begin = self.Holding.Entity.Position - self.Position;
                            Vector2 carryOffsetTarget = new Vector2(0f, -12f);
                            SimpleCurve curve =
                                new SimpleCurve(
                                    control: new Vector2(begin.X + Math.Sign(begin.X) * 2, carryOffsetTarget.Y - 2f),
                                    begin: begin, end: carryOffsetTarget);
                            self.carryOffset = begin;
                            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, start: true);
                            tween.OnUpdate = delegate(Tween t) { self.carryOffset = curve.GetPoint(t.Eased); };
                            self.Add(tween);
                        }
                    }
                }
            }
            else
            {
                if (!Input.GrabCheck && self.minHoldTimer <= 0f)
                {
                    if (self.Holding != null)
                    {
                        if (Input.MoveY.Value == 1)
                        {
                            self.Drop();
                        }
                        else
                        {
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                            self.Holding.Release(Vector2.UnitX * (float)self.Facing);
                            self.Play("event:/char/madeline/crystaltheo_throw");
                        }

                        self.Holding = null;
                    }
                }
            }
        }
        return orig(self);
    }

    private static int SummitLaunchUpdateHook(On.Celeste.Player.orig_SummitLaunchUpdate orig, Player self)
    {
        if (self.Scene.Tracker.GetEntity<StatePickupController>() is { SummitLaunch: true } controller)
        {
            if (self.Holding == null)
            {
                if (Input.GrabCheck && !self.IsTired && self.CanUnDuck)
                {
                    foreach (Holdable h in self.Scene.Tracker.GetComponents<Holdable>())
                    {
                        if (h.Check(self) && self.Pickup(h))
                        {
                            if (controller.resetToNormal)
                                return 8;
                            
                            self.Play("event:/char/madeline/crystaltheo_lift");
                            Vector2 begin = self.Holding.Entity.Position - self.Position;
                            Vector2 carryOffsetTarget = new Vector2(0f, -12f);
                            SimpleCurve curve =
                                new SimpleCurve(
                                    control: new Vector2(begin.X + Math.Sign(begin.X) * 2, carryOffsetTarget.Y - 2f),
                                    begin: begin, end: carryOffsetTarget);
                            self.carryOffset = begin;
                            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, start: true);
                            tween.OnUpdate = delegate(Tween t) { self.carryOffset = curve.GetPoint(t.Eased); };
                            self.Add(tween);
                        }
                    }
                }
            }
            else
            {
                if (!Input.GrabCheck && self.minHoldTimer <= 0f)
                {
                    if (self.Holding != null)
                    {
                        if (Input.MoveY.Value == 1)
                        {
                            self.Drop();
                        }
                        else
                        {
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                            self.Holding.Release(Vector2.UnitX * (float)self.Facing);
                            self.Play("event:/char/madeline/crystaltheo_throw");
                        }

                        self.Holding = null;
                    }
                }
            }
        }
        return orig(self);
    }
}