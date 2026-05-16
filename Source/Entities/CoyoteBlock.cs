using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/coyoteBlock")]

public class CoyoteBlock : Solid
{
    //wall of text
    public enum ReboundType
    {
        Rebound = 0,
        Bounce = 1,
        Ignore = 2
    }

    public enum OnHitType
    {
        NoBreak = 0,
        Respawn = 1,
        OneUse = 2,
    }
    private char tileType;
    private int dashRefill;
    private ReboundType reboundType;
    private bool playSound;
    private bool playDebrisSound;
    private bool staminaRefill;
    private bool blendIn;
    private bool speadlmao;
    private bool broken;
    private bool coyoteOnlyWhenDashing;
    private float coyoteTime;    
    private float coyoteTime2;
    private float freezeFrameTime;
    private float cooldown;
    private float respawnTime;
    private float timer;
    private float timer2;
    private string soundString;
    private string flagThingy;
    private OnHitType onHit;
    TileGrid tileGrid;
    
    public CoyoteBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, true)
    {
        //more wall
        tileType = data.Char("tileType");
        playSound = data.Bool("playSound");
        playDebrisSound = data.Bool("playDebrisSound");
        coyoteTime = data.Float("coyoteTime");
        coyoteTime2 = data.Float("coyoteTimeAfterDash");
        freezeFrameTime = data.Float("freezeFrameTime");
        soundString = data.Attr("breakSound");
        staminaRefill = data.Bool("staminaRefill");
        flagThingy = data.Attr("flagOnDash");
        dashRefill = data.Int("refillAmount");
        blendIn = data.Bool("blendIn");
        cooldown = data.Float("dashCooldown");
        onHit = data.Enum<OnHitType>("onHit");
        timer = data.Float("respawnTime");
        speadlmao = data.Bool("fast");
        reboundType = data.Enum<ReboundType>("reboundType");
        coyoteOnlyWhenDashing = data.Bool("coyoteOnlyWhenDashing");

        OnDashCollide = OnDashed;
    }

    public static void Load()
    {
        IL.Celeste.Player.SuperJump += bullshit;
        Everest.Events.Player.OnSpawn += AddCoyoteCheck;
    }

    public static void Unload()
    {
        IL.Celeste.Player.SuperJump -= bullshit;
        Everest.Events.Player.OnSpawn -= AddCoyoteCheck;
    }

    private static void AddCoyoteCheck(Player player) //Add Player Component
    {
        player.Add(new CoyoteCheck());
    }

    internal class CoyoteCheck : Component //Player Component
    {
        public bool hasCoyoteBlockFrames;
        public float origSpeed;
        public bool midAirJump;


        public CoyoteCheck() : base(false, false) {}
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (!blendIn)
        {
            tileGrid = GFX.FGAutotiler.GenerateBox(tileType, (int)Width / 8, (int)Height / 8).TileGrid;
            Add(new LightOcclude());
        }
        else
        {
            Level level = SceneAs<Level>();
            Rectangle tileBounds = level.Session.MapData.TileBounds;
            VirtualMap<char> solidsData = level.SolidsData;
            int x = (int)(X / 8f) - tileBounds.Left;
            int y = (int)(Y / 8f) - tileBounds.Top;
            int tilesX = (int)Width / 8;
            int tilesY = (int)Height / 8;
            tileGrid = GFX.FGAutotiler.GenerateOverlay(tileType, x, y, tilesX, tilesY, solidsData).TileGrid;
            Add(new EffectCutout());
            Depth = -10501;
        }
        Add(tileGrid);
        Add(new TileInterceptor(tileGrid, highPriority: true));
        if (CollideCheck<Player>())
        {
            RemoveSelf();
        }
    } 

    private DashCollisionResults OnDashed(Player player, Vector2 direction)
    {
        Level level = SceneAs<Level>();
        player.dashCooldownTimer = cooldown;
        if (!coyoteOnlyWhenDashing)
            player.jumpGraceTimer = coyoteTime;

        player.Dashes = dashRefill;
        
        if (staminaRefill)
            player.RefillStamina();
        
        Celeste.Freeze(freezeFrameTime);
        
        if (speadlmao) //I love speed preservation
            player.Get<CoyoteCheck>().origSpeed = Math.Abs(player.Speed.X);
        
        timer2 = coyoteTime; //Timer to determine how much time the player should have coyote frames 
        
        player.Get<CoyoteCheck>().hasCoyoteBlockFrames = true; //If the player has coyote frames from the block
        
        if (onHit != OnHitType.NoBreak)
            Break(player.Center,direction);
       
        if (!string.IsNullOrEmpty(flagThingy))
            level.Session.SetFlag(flagThingy);
        
        Audio.Play(soundString, Position);
        
        switch(reboundType)
        {
            case ReboundType.Rebound:
                return DashCollisionResults.Rebound;
            case ReboundType.Bounce:
                return DashCollisionResults.Bounce;
            case ReboundType.Ignore:
                player.dashAttackTimer = 0;
                return DashCollisionResults.Ignore;
        }
        return DashCollisionResults.Rebound;
    }

    private void Break(Vector2 from, Vector2 direction)
    {
        Level level = SceneAs<Level>();
        if (playSound)
            Audio.Play(soundString, Position);
        
        for (int i = 0; (float)i < Width / 8f; i++)
        {
            for (int j = 0; (float)j < Height / 8f; j++)
            {
                Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tileType, playDebrisSound).BlastFrom(from));
            }
        }

        if(!string.IsNullOrEmpty(flagThingy)) 
            level.Session.SetFlag(flagThingy);
        
        Collidable = false;
        if (onHit == OnHitType.Respawn)
        {
            respawnTime = timer;
            tileGrid.Alpha = 0.3f;
        }
        else
        {
            tileGrid.Visible = false;
        }
    }

    public override void Render()
    {
        base.Render();
        
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        
        if (!Collidable && onHit != OnHitType.OneUse)
        {
            Draw.HollowRect(Position, Width, Height, Color.White);
        }

        if (player.Get<CoyoteCheck>().hasCoyoteBlockFrames) //buble
        {
            Draw.Circle(player.Center - Vector2.UnitY * 3, 10, Color.LightSkyBlue, 2);
        }
        
    }
    
    public override void Update()
    {
        base.Update();
        
        if (Scene?.Tracker.GetEntity<Player>() is not { } player) return;
        
        if (player.Get<CoyoteCheck>().hasCoyoteBlockFrames)
        {
            if (timer2 > 0f)
            {
                timer2 -= Engine.DeltaTime;
                if (player.StateMachine.State == Player.StDash && coyoteOnlyWhenDashing || player.Get<CoyoteCheck>().midAirJump) //Checks if the player did a midair super jump so that timer2 gets reset
                {
                    timer2 = coyoteTime2;
                    player.jumpGraceTimer = coyoteTime2;
                    player.Get<CoyoteCheck>().midAirJump = false;
                }

                if (timer2 <= 0f)
                {
                    player.Get<CoyoteCheck>().hasCoyoteBlockFrames = false; //Disable hook
                    player.Get<CoyoteCheck>().origSpeed = 0;
                }
            }
        }

        if (respawnTime > 0f)
        {
            respawnTime -= Engine.DeltaTime;
            return;
        }
        if (!CollideCheck<Actor>() && !CollideCheck<Solid>() && onHit != OnHitType.OneUse)
        {
            tileGrid.Alpha = 1;
            Collidable = true;
        }
    }

    private static void bullshit(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(260f)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(spead);
        }
    }

    private static float spead(float orig, Player player)
    {
        if(player.Get<CoyoteCheck>() is not { } variableidfk) //null check
            return orig;

        if (player.Dashes >= 1 && variableidfk.hasCoyoteBlockFrames) //Allows for multiple midair super jumps if the player had two dashes
        {
            variableidfk.midAirJump = true;
        }

        if (!variableidfk.hasCoyoteBlockFrames || variableidfk.origSpeed < 260f)
           return orig;
        
        if (player.Dashes <= 0 && variableidfk.hasCoyoteBlockFrames)
            variableidfk.hasCoyoteBlockFrames = false;
        
        return variableidfk.origSpeed; //spped
    }
}