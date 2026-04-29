using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/superDashBumper")]
public class SuperDashBumper : Bumper
{
    public float timer;
    public float customSpeed;
    public bool Static;
    public bool soup;
    public bool demo;
    public bool spead;
    public bool alwaysBoost;
    public bool verticalStretch;
    private Vector2 fast;
    private Vector2 origspeed;

    public static void Load()
    {
        On.Celeste.Player.DashEnd += ResetSoupOnDashEnd;
        Everest.Events.Player.OnDie += ResetSoupOnDeath;
        Everest.Events.Player.OnRegisterStates += AddPlayerSoupData;
    }

    public static void Unload()
    {
        On.Celeste.Player.DashEnd -= ResetSoupOnDashEnd;
        Everest.Events.Player.OnDie -= ResetSoupOnDeath;
        Everest.Events.Player.OnRegisterStates -= AddPlayerSoupData;
    }

    private static void ResetSoupOnDashEnd(On.Celeste.Player.orig_DashEnd orig, Player self)
    {
        DisableTemporarySoup(self);
        orig(self);
    }

    private static void ResetSoupOnDeath(Player player)
    {
        if (player.Get<SoupData>() is { isTemporarySuperdash: true } soup)
        {
            // this is a temporary superdash; reset the variant
            soup.isTemporarySuperdash = false;
            SaveData.Instance.Assists.SuperDashing = false;
        }
    }

    private static void AddPlayerSoupData(Player player)
    {
        player.Add(new SoupData());
    }

    private static void DisableTemporarySoup(Player player)
    {
        if (player.Get<SoupData>() is { isTemporarySuperdash: true } soup)
        {
            // this is a temporary superdash; reset the variant
            soup.isTemporarySuperdash = false;
            SaveData.Instance.Assists.SuperDashing = false;
        }
    }

    public SuperDashBumper(EntityData data, Vector2 offset) : base(data, offset)
    {
        verticalStretch = data.Bool("verticalDashStretching");
        Static = data.Bool("static");
        spead = data.Bool("fast");
        alwaysBoost = data.Bool("alwaysBoost");
        soup = data.Bool("soup");
        timer = data.Float("respawnTimer");
        customSpeed = data.Float("launchDashSpeed");

        if (Static)
            Remove(sine);
        Get<PlayerCollider>().OnCollide = OnPlayer;

        // don't react to core changes; never appear to be a hazard
        Remove(Get<CoreModeListener>());

        // CreateOn modifies the sprite parameter in-place
        GFX.SpriteBank.CreateOn(sprite, soup ? "superDashBumper" : "dashBumper");
        sprite.Play("idle");
        sprite.CenterOrigin();
    }

    internal class SoupData : Component
    {
        public bool isTemporarySuperdash;
        public SoupData() : base(false, false) { }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        // we want the bumper to never appear like a hazard
        fireMode = false;
        spriteEvil.Visible = false;
        sprite.Visible = true;
    }

    // mostly copied from vanilla's Bumper.OnPlayer
    private new void OnPlayer(Player player)
    {
        if (Scene is not Level level)
            // sanity check: make sure we're in a level and put it in a "level" variable
            return;

        if (respawnTimer > 0f)
            // the bumper isn't collidable yet
            return;

        Audio.Play(SFX.game_06_pinballbumper_hit, Position);

        respawnTimer = timer;
        if (player.demoDashed)
            demo = true;
        Vector2 vector2 = ExplodeDashLaunch(player, Position, snapUp: false, sidesOnly: false);

        sprite.Play("hit", restart: true);
        light.Visible = false;
        bloom.Visible = false;

        level.DirectionalShake(vector2, 0.15f);
        level.Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f);
        level.Particles.Emit(P_Launch, 12, Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());

        if (!player.Inventory.NoRefills)
            player.RefillDash();
    }

    // also mostly copied from Player.ExplodeLaunch
    public Vector2 ExplodeDashLaunch(Player player, Vector2 from, bool snapUp = true, bool sidesOnly = false)
    {
        //Preserves speed because what good entity doesn't? 
        origspeed = player.Speed;

        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        Celeste.Freeze(0.1f);
        player.launchApproachX = null;
        Vector2 vector = (player.Center - from).SafeNormalize(-Vector2.UnitY);
        float num = Vector2.Dot(vector, Vector2.UnitY);
        if (snapUp && num <= -0.7f)
        {
            vector.X = 0f;
            vector.Y = -1f;
        }
        else if (num <= 0.65f && num >= -0.55f)
        {
            vector.Y = 0f;
            vector.X = Math.Sign(vector.X);
        }
        if (sidesOnly && vector.X != 0f)
        {
            vector.Y = 0f;
            vector.X = Math.Sign(vector.X);
        }
        player.Speed = customSpeed * vector;
        // Determines whether fast mode is activated
        if (spead)
        {
            fast = player.Speed.Sign() * Vector2.Max(player.Speed.Abs(), origspeed.Abs());
            player.Speed.X = fast.X;
            //In case you wanna mess with vertical dash stretching like a chad
        }

        if (player.Speed.Y <= 50f)
        {
            player.Speed.Y = Math.Min(-150f, player.Speed.Y);
            player.AutoJump = true;
        }
        if (player.Speed.X != 0f)
        {
            if (Input.MoveX.Value == Math.Sign(player.Speed.X) && !alwaysBoost)
            {
                player.explodeLaunchBoostTimer = 0f;
                player.Speed.X *= 1.2f;
            }
            else
            {
                player.explodeLaunchBoostTimer = 0.01f;
                player.Speed.X *= 1.2f;
            }
        }
        SlashFx.Burst(player.Center, player.Speed.Angle());
        player.RefillStamina();

        player.OverrideDashDirection = vector;
        player.StateMachine.ForceState(2);
        if (spead && verticalStretch)
            Alarm.Set(player, 0.03f, () => player.Speed.Y = fast.Y);

        // soup
        // if the variant isn't already enabled map-wide, mark this superdash as temporary
        // the variant will be turned back off in DashEnd
        if (soup && !SaveData.Instance.Assists.SuperDashing)
        {
            SaveData.Instance.Assists.SuperDashing = true;
            player.Get<SoupData>().isTemporarySuperdash = true;
        }

        //Prevents the dash direction override being permanent
        Alarm.Set(player, 0.1f, () => player.OverrideDashDirection = null);
        //Preserves crouched state when demo'd into 
        player.Ducking = demo;

        return vector;
    }
}
