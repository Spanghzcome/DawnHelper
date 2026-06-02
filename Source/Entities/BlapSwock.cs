using System;
using System.Reflection;
using Celeste.Mod.DawnHelper.Misc;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/blapSwock")]
[Tracked]
public class BlapSwock : Entity
{
    private float radius;
    private Sprite sprite;
    private Image targetSprite;
    private BloomPoint bloom;
    private VertexLight light;
    private ParticleType BlapSwock_Glow;
    private ParticleType dashPull;
    private float timer;
    private float particlesRemainder;
    private Level level;
    private Vector2 vector;
    private Vector2 origSpeed;
    private Vector2 fast;
    private Vector2 target;
    private EventInstance loop;
    private LedgeBlocker thing;
    private static ILHook dashCoroutineHook;
    private bool dashDirectionSpeedRetention;
    private bool fun;
    private bool spead;
    
    public static void Load()
    {
        On.Celeste.Player.DashEnd += DreamJumpAnalog; 
        dashCoroutineHook = new ILHook(typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), IL_Player_DashCoroutine);
        Everest.Events.Player.OnSpawn += AddDashInsideCheck;
    }

    public static void Unload()
    {
        On.Celeste.Player.DashEnd -= DreamJumpAnalog;
        dashCoroutineHook?.Dispose();
        Everest.Events.Player.OnSpawn -= AddDashInsideCheck;
    }
    
    public BlapSwock(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        fun = data.Bool("retainDashDirection", true);
        dashDirectionSpeedRetention = data.Bool("dashDirectionSpeedRetention", true);
        spead = data.Bool("speedRetention", true);
        target = data.Nodes[0] + offset;
        radius = data.Float("radius", 50);
        
        Collider = new Circle(radius);
        Add(new DashListener(OnDash));
        Add(sprite = GFX.SpriteBank.Create(data.Attr("spriteXMLName")));
        sprite.Play("idle");
        targetSprite = new Image(GFX.Game[data.Attr("targetSprite")]);
        targetSprite.JustifyOrigin(0.5f, 0.5f);
        Add(bloom = new BloomPoint(0.2f, radius));
        Add(light = new VertexLight(Color.MediumVioletRed, 1f, 16, 48));
        BlapSwock_Glow = new ParticleType()
        {
         
            Size = 1f,
            Color = Calc.HexToColor("ff0000"),
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
        dashPull = new ParticleType(SwapBlock.P_Move)
        {
            Color = Calc.HexToColor(data.Attr("particleColor1", "fbf236")),
            Color2 = Calc.HexToColor(data.Attr("particleColor2", "6abe30"))
        };
    }

    private static void AddDashInsideCheck(Player player)
    {
        player.Add(new DashInsideCheck());
    }
    
    internal class DashInsideCheck : Component
    {
        public bool dashedInside;
        public DashInsideCheck() : base(false, false) {}
    }

    private void OnDash(Vector2 direction)
    {
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        if(player.Get<DashInsideCheck>() is not { } check) return;
        
        if (CollideCheck(player))
        {
            Add(thing = new LedgeBlocker());
            if (dashDirectionSpeedRetention)
                origSpeed = player.beforeDashSpeed;
            else
                origSpeed = player.Speed;

            if (player.StateMachine.State == Player.StDash)
            {
                Audio.Play("event:/game/05_mirror_temple/swapblock_move", player.Center);
                loop = Audio.Loop("event:/game/06_reflection/badeline_pull_rumble_loop", player.Center);
                
                vector = (player.Center - target);
                vector = vector.SafeNormalize();
                var inverted = GravityHelperInterop.IsPlayerInverted();
                if (inverted)
                    vector.Y *= -1;
                
                player.Speed = 360f * vector;
                check.dashedInside = true;
                if (spead)
                {
                    origSpeed = origSpeed.Length() * vector;
                    fast = player.Speed.Sign() * Vector2.Max(player.Speed.Abs(), origSpeed.Abs());
                    
                    player.Speed.X = -fast.X;
                    player.Speed.Y = -fast.Y;
                    if (!fun)
                    {
                        player.OverrideDashDirection = vector;
                        Alarm.Set(player, 0.1f, () => player.OverrideDashDirection = null);
                    }
                }
            }
        }
    }
    
    private static void DreamJumpAnalog(On.Celeste.Player.orig_DashEnd orig, Player self)
    {
        bool wallBounceCheck = false;
        if (self.DashDir.X == 0 && self.DashDir.Y == -1)
            wallBounceCheck = true;
        
        orig(self);
        
        if (self.Get<DashInsideCheck>() is { dashedInside: true } && !wallBounceCheck)
        {
            self.jumpGraceTimer = 0.1f;
        }
    }

    private static float DetermineDashLength(float orig, Player player)
    {
        BlapSwock blapSwock = player.CollideFirst<BlapSwock>();
        if (blapSwock == null)
            return orig;
        DashAttackLength(player, blapSwock);
        
        return Vector2.Distance(player.Center, blapSwock.target) / player.Speed.Length();
    }

    private static void DashAttackLength(Player player, BlapSwock blapSwock)
    {
        player.dashAttackTimer = Vector2.Distance(player.Center, blapSwock.target) / player.Speed.Length() + 0.05f;
        player.dashTrailCounter = (int)Math.Round(Vector2.Distance(player.Center, blapSwock.target));
    }
    public static void IL_Player_DashCoroutine(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
    
        while (cursor.TryGotoNext(MoveType.After,
                       instr => instr.MatchLdcR4(0.3f) || instr.MatchLdcR4(0.15f)))
        {
            cursor.EmitLdloc1();
            cursor.EmitDelegate(DetermineDashLength);
        }
    }
        
    public override void Added(Scene scene)
    {
        base.Added(scene);
        level = SceneAs<Level>();
    }

    public override void Render()
    {
        base.Render();
        
        float scale = 0.5f * (0.5f + ((float)Math.Sin(timer) + 1f) * 0.25f);  
        targetSprite.Position = target;
        targetSprite.Render();
        Draw.Circle(Position, radius, Color.White * scale, 10);
        
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
       
        if (CollideCheck(player))
        {
            Draw.Line(player.Center - Vector2.UnitY * 5, target - Vector2.UnitY * 5, Color.White * scale, 2);
            Draw.Line(player.Center + Vector2.UnitY * 5, target + Vector2.UnitY * 5, Color.White * scale, 2);
        }
        
        if (player.StateMachine.State == Player.StDash && player.Get<DashInsideCheck>().dashedInside)
        {
            Draw.Line(player.Center - Vector2.UnitY * 5, target - Vector2.UnitY * 5, Color.Red * scale, 2);
            Draw.Line(player.Center + Vector2.UnitY * 5, target + Vector2.UnitY * 5, Color.Red * scale, 2);
            targetSprite.Color = Color.Red * scale;
        }

        else
        {
            targetSprite.Color = Color.White * scale;
            player.Get<DashInsideCheck>().dashedInside = false;
        }
    }

    public override void Update()
    {
        base.Update();
        
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        
        Vector2 position;
        Vector2 positionRange;
        float direction;
        float num;
        timer += Engine.DeltaTime * 4f;

        if (Scene.OnInterval(0.1f))
        {
            level.ParticlesFG.Emit(BlapSwock_Glow, 1, Position, Vector2.One * (radius - 15));
        }

        light.Alpha = Calc.Approach(light.Alpha, targetSprite.Visible ? 0f : 1f, 4f * Engine.DeltaTime);
        bloom.Alpha = light.Alpha * 0.1f;
        if (player.Get<DashInsideCheck>() is not null)
        {
            if (player.Get<DashInsideCheck>().dashedInside && Scene.OnInterval(0.02f))
            {
                Vector2 normal = target - player.Center;
                if (normal.X > 0)
                {
                    position = player.CenterLeft;
                    positionRange = Vector2.UnitY * (player.Height - 6f);
                    direction = MathF.PI;
                    num = Math.Max(2f, player.Height / 14f);
                }
                else if (normal.X < 0f)
                {
                    position = player.CenterRight;
                    positionRange = Vector2.UnitY * (player.Height - 6f);
                    direction = 0f;
                    num = Math.Max(2f, player.Height / 14f);
                }
                else if (normal.Y > 0f)
                {
                    position = player.TopCenter;
                    positionRange = Vector2.UnitX * (player.Width - 6f);
                    direction = -MathF.PI / 2f;
                    num = Math.Max(2f, player.Width / 14f);
                }
                else
                {
                    position = player.BottomCenter;
                    positionRange = Vector2.UnitX * (player.Width - 6f);
                    direction = MathF.PI / 2f;
                    num = Math.Max(2f, player.Width / 14f);
                }

                particlesRemainder += num;
                int num2 = (int)particlesRemainder;
                particlesRemainder -= num2;
                positionRange *= 0.5f;
                SceneAs<Level>().Particles.Emit(dashPull, num2, position, positionRange, direction);
            }
        }

        if (player.StateMachine.State == Player.StDash && player.Get<DashInsideCheck>().dashedInside)
        {
            sprite.Play("blapping");
        }
        
        else if (CollideCheck(player) && !player.Get<DashInsideCheck>().dashedInside && sprite.CurrentAnimationID == "idle")
        {
            sprite.Play("noticed");
            Audio.Play("event:/game/06_reflection/pinballbumper_reset", Position);
            Audio.Play("event:/new_content/game/10_farewell/glider_land", Position);
        }
        else if (sprite.CurrentAnimationID == "blapping" && player.StateMachine.State != Player.StDash)
        {
            sprite.Play("stop");
            Audio.Play("event:/game/00_prologue/car_down", Position);
            Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", player.Center);
            Audio.Stop(loop);
            Remove(thing);
        }
        
        else if (sprite.CurrentAnimationID == "alerted" && !CollideCheck(player))
        {
            sprite.Play("left");
            Audio.Play("event:/new_content/game/10_farewell/puffer_reform", Position);
        }
    }
}