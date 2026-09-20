using System;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/seaGlide")]
public class SeaGlide : TheoCrystal
{
    private float dashCooldownTimer;
    private DisplacementRenderer.Burst burst;
    private ParticleType particleType;
    private SoundSource loopingSFX;
    private bool glideDashedSound;
    private bool holdToActivate;
    private float maxGlideSpeed;
    private float glideDashSpeed;
    private float glideDashCooldown;
    
    public SeaGlide(EntityData data, Vector2 offset) : base(data, offset)
    {
        holdToActivate = data.Bool("holdJumpToActivate");
        maxGlideSpeed = data.Float("maxGlideSpeed", 190f);
        glideDashSpeed = data.Float("glideDashSpeed", 320f);
        glideDashCooldown = data.Float("glideDashCooldown", 2f);
        
        GFX.SpriteBank.CreateOn(sprite, data.Attr("spriteXMLName"));
        sprite.Play("active");
        Add(loopingSFX = new SoundSource());

        particleType = new ParticleType()
        {
            Size = 1f,
            Color = Calc.HexToColor("#7ddeee"),
            Color2 = Calc.HexToColor("ffffff"),
            ColorMode = ParticleType.ColorModes.Blink,
            FadeMode = ParticleType.FadeModes.InAndOut,
            Direction = -MathF.PI / 2f,
            DirectionRange = 1.3962634f,
            SpeedMin = 5f,
            SpeedMax = 10f,
            LifeMin = 0.6f,
            LifeMax = 1f
        };
    }
    
    public static void Load()
    {
        On.Celeste.Player.SwimUpdate += GlideState;
        IL.Celeste.Player.UseRefill += UseRefillHook;
        IL.Celeste.Player.OnCollideV += AllowVerticalDashEvents;
        Everest.Events.Player.OnSpawn += AddDashThingy;
    }

    public static void Unload()
    {
        On.Celeste.Player.SwimUpdate -= GlideState;
        IL.Celeste.Player.UseRefill -= UseRefillHook;
        IL.Celeste.Player.OnCollideV -= AllowVerticalDashEvents;
        Everest.Events.Player.OnSpawn -= AddDashThingy;
    }

    private static void AddDashThingy(Player player)
    {
        player.Add(new DashThingy());
    }

    internal class DashThingy() : Component(false, false)
    {
        public float dashTimer;
    }

    private static int GlideState(On.Celeste.Player.orig_SwimUpdate orig, Player self)
    {
        if (self.Holding?.Entity is not SeaGlide seaGlide) return orig(self);
        if (self.Get<DashThingy>() is not { } dashThingy) return orig(self);
        if (seaGlide.holdToActivate && !Input.Jump.Check) return orig(self);
        
        if (!self.SwimCheck())
            return 0;
        
        Vector2 value = Input.Feather.Value;
        if (value == Vector2.Zero)
        {
            value = self.starFlyLastDir;
        }

        Vector2 vector = self.Speed.SafeNormalize(Vector2.Zero);
        vector = (self.starFlyLastDir = ((!(vector == Vector2.Zero))
            ? vector.RotateTowards(value.Angle(), 8f * Engine.DeltaTime)
            : value));
        float target;
        
        if (vector != Vector2.Zero && Vector2.Dot(vector, value) >= 0.45f)
        {
            self.starFlySpeedLerp = Calc.Approach(self.starFlySpeedLerp, 1f, 2 * Engine.DeltaTime / 1f);
            target = MathHelper.Lerp(seaGlide.maxGlideSpeed * 0.74f, seaGlide.maxGlideSpeed, self.starFlySpeedLerp);
        }
        else
        {
            self.starFlySpeedLerp = 0f;
            target = seaGlide.maxGlideSpeed * 0.74f;
        }
        
        float val = self.Speed.Length();

        if (dashThingy.dashTimer <= 0)
        {
            val = Calc.Approach(val, target, 1000f * Engine.DeltaTime);
            self.Speed = vector * val;
        }
        else if (dashThingy.dashTimer > 0 && val <= seaGlide.glideDashSpeed)
        {
            self.Speed = vector * seaGlide.glideDashSpeed;
        }
        else
            self.Speed = vector * val;

        self.DashDir = self.Speed.SafeNormalize(); // Makes dash attack interactions consistent
        self.CorrectDashPrecision(self.DashDir);
        
        if (Input.Jump.Pressed && self.SwimJumpCheck())
        {
            self.Jump();
            return 0;
        }
        
        return 3;
    }

    private static void UseRefillHook(ILContext il)
    {
        ILLabel thing = null!;
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNextBestFit(MoveType.After,instr => instr.MatchBlt(out thing))) //Adds an extra argument to an if statement
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CooldownArgument);
            c.EmitBrtrue(thing);
            
            c.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("RefillStamina"));
            
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(RefillGlideCooldown);
        }
    }

    private static bool CooldownArgument(Player self)
    {
        if (self.Holding?.Entity is not SeaGlide seaGlide) return false;

        if (seaGlide.dashCooldownTimer > 0)
        {
            return true;
        }
        
        return false;
    }

    private static void RefillGlideCooldown(Player self)
    {
        if (self.Holding?.Entity is not SeaGlide seaGlide) return;

        seaGlide.dashCooldownTimer = 0;
    }

    private static void AllowVerticalDashEvents(ILContext il)
    {
        ILLabel thing = null!;
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNextBestFit(MoveType.After, instr => instr.MatchLdcI4(3), instr => instr.MatchBneUn(out thing))) // Allows dash events to occur when a block is hit vertically. Adds an extra argument to an if statement
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(plswork);
            c.EmitBrfalse(thing);

            thing = null!;
            c.GotoNextBestFit(MoveType.After, instr =>  instr.MatchLdcI4(1), instr => instr.MatchBeq(out thing)); // This hook is mainly to prevent dream block step noises from repeatedly playing when swimming on a dream block. Adds an extra argument to an if statement
            
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(plswork);
            c.EmitBrfalse(thing);
        }
    }

    private static bool plswork(Player self)
    {
        if (self.Holding?.Entity is not SeaGlide seaGlide) return true;

        return false;
    }

    public override void Render()
    {
        sprite.DrawSimpleOutline();
        base.Render();
    }

    public override void Update()
    {
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        if (player.Get<DashThingy>() is not { } dashThingy) return;

        if (dashThingy.dashTimer > 0)
        {
            dashThingy.dashTimer -= Engine.DeltaTime;
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Engine.DeltaTime;
            sprite.Play("inactive");
        }

        if (dashCooldownTimer <= 0 && sprite.CurrentAnimationID == "inactive")
        {
            Audio.Play("event:/game/06_reflection/feather_reappear", Center);
            sprite.Play("active");
        }

        if (player.StateMachine.State == Player.StSwim && Hold.IsHeld)
        {
            sprite.Position = player.Center - Position;
            sprite.Rotation = player.Speed.Angle() + MathHelper.PiOver2;
            
            if (!holdToActivate || Input.Jump.Check)
            {
                Vector2 dir = player.Speed.SafeNormalize(Vector2.Zero);
                float angle = (-dir).Angle();

                if (Scene.OnInterval(0.02f))
                {
                    (Scene as Level).ParticlesBG.Emit(particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f),
                        new Vector2(3f, 3f), angle);
                }

                if (!loopingSFX.Playing)
                    loopingSFX.Play("event:/DawnHelper/seaGlide/seaGlideMove");

                if (dashCooldownTimer <= 0)
                {
                    if (Input.Dash.Pressed)
                    {
                        glideDashedSound = true;
                        Celeste.Freeze(0.05f);
                        if (!SaveData.Instance.Assists.DashAssist)
                        {
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }

                        dashThingy.dashTimer = 0.4f;
                        dashCooldownTimer = glideDashCooldown;
                        player.dashAttackTimer = 0.5f;
                        burst = (Scene as Level).Displacement.AddBurst(Center, 0.4f, 0f, 32f);

                        Audio.Play("event:/DawnHelper/seaGlide/glideDashActivate", Center);
                        Audio.Play("event:/char/madeline/water_dash_gen", Center);
                    }
                }
            }

            else
            {
                loopingSFX.Stop();
            }
        }
        
        else
        {
            sprite.Rotation = 0f;
            sprite.Position = ExactPosition - Position;
            loopingSFX.Stop();
            if (glideDashedSound) {
                Audio.Play("event:/char/madeline/water_dash_out", Center);
                glideDashedSound = false;
            }
        }

        base.Update();
    }
}