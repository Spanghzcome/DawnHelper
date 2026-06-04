using System;
using Celeste.Mod.DawnHelper.Misc;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/versatileSpring", "DawnHelper/ceilingVersatileSpring = LoadCeiling")]
[TrackedAs(typeof(Spring))]


public class VersatileSpring : Spring
{
    private string spritePath;
    private Orientations onOrientation;
    private readonly bool _cursed;
    private readonly bool _holdablesCanUse;
    private readonly bool _flagToggle;
    private readonly bool _drawOutline;
    private bool flagTrue;
    private bool _invertedVerticalMomentum;
    private readonly string _flagOnHit;
    private readonly BehindCollision _behindCollision;
    private readonly MomentumType _momentum;

    public VersatileSpring(EntityData data, Vector2 offset) : this(
        data.Position + offset,
        data.Attr("sprite"),
        data.Enum<Orientations>("orientation"),
        data.Bool("playerCanUse"),
        data.Bool("holdablesCanUse"),
        data.Bool("cursed"),
        data.Enum<MomentumType>("momentumType"),
        data.Enum<BehindCollision>("collisionFromBehind"),
        data.Attr("flagOnHit"),
        data.Bool("toggleFlag"),
        data.Bool("invertedVerticalMomentum"),
        data.Bool("drawOutline"))
    { }
    
    public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        return new VersatileSpring(
            entityData.Position + offset,
            entityData.Attr("sprite"),
            Orientations.Ceiling,
            entityData.Bool("playerCanUse"),
            entityData.Bool("holdablesCanUse"),
            entityData.Bool("cursed"),
            entityData.Enum<MomentumType>("momentumType"),
            entityData.Enum<BehindCollision>("collisionFromBehind"),
            entityData.Attr("flagOnHit"),
            entityData.Bool("toggleFlag"),
            entityData.Bool("invertedVerticalMomentum"),
            entityData.Bool("drawOutline"));
    }

    public VersatileSpring(Vector2 position, string spritePath, Orientations orientation, bool playerCanUse, bool holdablesCanUse, bool cursed, MomentumType momentum, BehindCollision behindCollision, string flagOnHit, bool flagToggle, bool invertedverticalMomentum, bool drawOutline) : base(
        position, (Spring.Orientations)((int)orientation % 3), playerCanUse)
    {
        _drawOutline = drawOutline;
        _invertedVerticalMomentum = invertedverticalMomentum;
        _flagToggle = flagToggle;
        _flagOnHit = flagOnHit;
        _holdablesCanUse = holdablesCanUse;
        _behindCollision = behindCollision;
        _momentum = momentum;
        _cursed = cursed;
        
        if (string.IsNullOrWhiteSpace(spritePath))
        {
           if (_momentum != MomentumType.Off)
               spritePath = "objects/DawnHelper/springGreen/";
           else
               spritePath = "objects/spring/";
        }
        if (!spritePath.EndsWith("/"))
            spritePath += "/";
        
        sprite.Reset(GFX.Game, spritePath);
        sprite.Add("idle", "", 0.0f, new int[1]);
        sprite.Add("bounce", "", 0.07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
        sprite.Add("disabled", "white", 0.07f);
        sprite.Play("idle");
        sprite.Origin.X = sprite.Width / 2f;
        sprite.Origin.Y = sprite.Height;

        Get<PlayerCollider>().OnCollide = newOnCollide;
        Get<HoldableCollider>().OnCollide = newOnHoldable;
        PufferCollider pufferCollider = Get<PufferCollider>();

        switch (orientation)
        {
            case Orientations.Floor:
                sprite.Rotation = 0f;
                break;
            case Orientations.WallLeft:
                sprite.Rotation = (float)Math.PI / 2f;
                break;
            case Orientations.WallRight:
                sprite.Rotation = -(float)Math.PI / 2f;
                break;
            case Orientations.Ceiling:
                sprite.Rotation = (float)Math.PI;
                Collider.Top += 6f;
                pufferCollider.Collider.Top += 6;
                staticMover.SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY);
                staticMover.JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY);
                break;
        }
        onOrientation = orientation;
    }

    private void newOnCollide(Player player)
    {
        if (player.StateMachine.State == Player.StDreamDash || !playerCanUse)
            return;
        
        var origDashAttacking = player.DashAttacking;
        var origSpeedX = player.Speed.X;
        var origSpeedY = player.Speed.Y;

        if (origDashAttacking && player.Speed == Vector2.Zero)
        {
            Vector2 bDSpeed = player.beforeDashSpeed;
            origSpeedX = bDSpeed.X;
            origSpeedY = bDSpeed.Y;
        }
        
        Vector2 origDashDir = player.DashDir;

        var orientation = onOrientation;
        bool inverted = GravityHelperInterop.IsPlayerInverted();
        if (inverted)
        {
            switch (orientation)
            {
                case Orientations.Floor:
                    orientation = Orientations.Ceiling;
                    break;
                case Orientations.Ceiling:
                    orientation = Orientations.Floor;
                    break;
            }
        }

        switch (orientation)
        {
            case Orientations.Floor:
                if (_behindCollision is BehindCollision.HoldableOnly or BehindCollision.Off && player.Speed.Y < 0f)
                    return;
                
                player.Speed.X = 0;
                player.Speed.Y = 0;
                player.SuperBounce(inverted ? Bottom : Top);

                if (_momentum is MomentumType.Both or MomentumType.PlayerOnly)
                {
                    player.Speed.X = origSpeedX;
                    if (origSpeedY > 0f)
                    {
                        if (origDashDir.Y > 0f && origDashAttacking)
                            player.varJumpSpeed = (player.Speed.Y = -123.333336f);
                        else
                            player.Speed.Y = -185f;
                    }
                    else
                        player.varJumpSpeed = player.Speed.Y = origSpeedY - 185f;
                }
                break;

            case Orientations.WallLeft:
                if (_behindCollision is BehindCollision.HoldableOnly or BehindCollision.Off && player.Speed.X > 0f)
                    return;
                
                player.Speed.X = 0;
                player.Speed.Y = 0;
                player.SideBounce(1, CenterRight.X, CenterRight.Y);

                if (_momentum is MomentumType.Both or MomentumType.PlayerOnly)
                {
                    player.varJumpSpeed = player.Speed.Y = _invertedVerticalMomentum ? -origSpeedY + 140f : origSpeedY - 140f;
                    player.Speed.X = Math.Max(origSpeedX, 0f) + 240f;
                }
                break;

            case Orientations.WallRight:
                if (_behindCollision is BehindCollision.HoldableOnly or BehindCollision.Off && player.Speed.X < 0f)
                    return;
                
                player.Speed.X = 0;
                player.Speed.Y = 0;
                player.SideBounce(-1, CenterLeft.X, CenterLeft.Y);

                if (_momentum is MomentumType.Both or MomentumType.PlayerOnly)
                {
                    player.varJumpSpeed = player.Speed.Y = _invertedVerticalMomentum ? -origSpeedY + 140f : origSpeedY - 140f;
                    player.Speed.X = Math.Min(origSpeedX, 0f) - 240f;
                }
                break;

            case Orientations.Ceiling:
                if (_behindCollision is BehindCollision.HoldableOnly or BehindCollision.Off && player.Speed.Y > 0f)
                    return;
                
                player.Speed.X = 0;
                player.Speed.Y = 0;
                if (_cursed)
                    player.SuperBounce(Bottom);
                else
                    SuperCeilingBounce(player, inverted ? Top : Bottom);

                if (_momentum is MomentumType.Both or MomentumType.PlayerOnly)
                {
                    player.Speed.X = origSpeedX;
                    if (origSpeedY < 0f)
                    {
                        if (origDashDir.Y < 0f && origDashAttacking)
                            player.varJumpSpeed = player.Speed.Y = 123.333336f;
                        else
                            player.Speed.Y = 185f;
                    }
                    else
                        player.varJumpSpeed = player.Speed.Y = origSpeedY + 185f;
                }
                break;
        } 
        bounceAnimate();
    }

    private void newOnHoldable(Holdable holdable)
    {
        if (holdable.IsHeld || !_holdablesCanUse)
            return;
        bool inverted;
        Orientations orientation = onOrientation;

        if (holdable.Entity is Actor actor)
        {
            inverted = GravityHelperInterop.IsActorInverted(actor);
            if (inverted)
            {
                switch (orientation)
                {
                    case Orientations.Floor:
                        orientation = Orientations.Ceiling;
                        break;
                    case Orientations.Ceiling:
                        orientation = Orientations.Floor;
                        break;
                }
            }
        }

        var speed = holdable.GetSpeed();
            
        if (_momentum is MomentumType.Off or MomentumType.PlayerOnly)
        {
            if ((orientation != Orientations.Ceiling && holdable.HitSpring(this)) || (orientation == Orientations.Ceiling && HoldableHitCeilingSpring(holdable)))
            {  
                bounceAnimate();
            }
            else if (_behindCollision is BehindCollision.HoldableOnly or BehindCollision.Both)
            {
                DirectionCheck(holdable, orientation, speed);
                bounceAnimate();
            }

            return;
        }
            
        holdable.HitSpring(this); // Doesn't always need to be called, but is still included for compatibility in edge cases.
        switch (orientation)
        {
            case Orientations.Floor:
                if (_behindCollision is BehindCollision.PlayerOnly or BehindCollision.Off && speed.Y <= 0f)
                    break;
                        
                holdable.gravityTimer = 0.15f;
                if (holdable.Entity is Glider gliderF)
                {
                    gliderF.wiggler.Start();
                    speed.Y = Math.Min(speed.Y, 0f) - 160f;
                }
                else
                    speed.Y = Math.Min(speed.Y, 0f) - 160f;

                if (holdable.Entity is Actor actorF)
                    actorF.MoveV(GravityHelperInterop.IsActorInverted(actorF) ? actorF.Top - Bottom : Top - actorF.Bottom);
                bounceAnimate();
                        
                break;

            case Orientations.WallLeft:
                if (_behindCollision is BehindCollision.PlayerOnly or BehindCollision.Off && speed.X <= 0f)
                    break;
                        
                holdable.gravityTimer = 0.1f;
                if (holdable.Entity is Glider gliderL) {
                    gliderL.wiggler.Start();
                    speed.X = Math.Max(speed.X, 0f) + 160f;
                }
                else
                    speed.X = Math.Max(speed.X, 0f) + 220f;

                if (holdable.Entity is Actor actorR)
                {
                    actorR.MoveV(Calc.Clamp(CenterRight.Y - actorR.Bottom, -4f, 4f));
                    actorR.MoveH(CenterRight.X - actorR.Left);
                }

                speed.Y -= 80f;
                bounceAnimate();

                break;

            case Orientations.WallRight:
                if (_behindCollision is BehindCollision.PlayerOnly or BehindCollision.Off && speed.X >= 0f)
                    break;
                        
                holdable.gravityTimer = 0.1f;
                if (holdable.Entity is Glider gliderR)
                {
                    gliderR.wiggler.Start();
                    speed.X = Math.Min(speed.X, 0f) - 160f;
                }
                else
                    speed.X = Math.Min(speed.X, 0f) - 220f;

                if (holdable.Entity is Actor actorL)
                {
                    actorL.MoveV(Calc.Clamp(CenterLeft.Y - actorL.Bottom, -4f, 4f));
                    actorL.MoveH(CenterLeft.X - actorL.Right);
                }

                speed.Y -= 80f;
                bounceAnimate();

                break;

            case Orientations.Ceiling:
                if (_behindCollision is BehindCollision.PlayerOnly or BehindCollision.Off && speed.Y >= 0f)
                    break;
                        
                holdable.gravityTimer = 0.15f;
                if (holdable.Entity is Glider gliderC)
                {
                    gliderC.wiggler.Start();
                    speed.Y = Math.Max(speed.Y, 0f) + 160f;
                }
                else
                    speed.Y = Math.Max(speed.Y, 0f) + 160f;

                if (holdable.Entity is Actor actorC)
                    actorC.MoveV(GravityHelperInterop.IsActorInverted(actorC) ? actorC.Bottom - Top : Bottom - actorC.Top);
                bounceAnimate();

                break;
        }

        holdable.SetSpeed(speed);
    }

    public void SuperCeilingBounce(Player player, float fromY)
    {
        if (player.StateMachine.State == 4 && player.CurrentBooster != null)
        {
            player.CurrentBooster.PlayerReleased();
            player.CurrentBooster = null;
        }
        Collider collider = player.Collider;
        player.Collider = player.normalHitbox;
        player.MoveV(GravityHelperInterop.IsPlayerInverted() ? (player.Bottom - fromY) : (fromY - player.Top));
        if (!player.Inventory.NoRefills)
        {
            player.RefillDash();
        }
        player.RefillStamina();
        player.StateMachine.State = 0;
        player.jumpGraceTimer = 0f;
        player.varJumpTimer = 0f;
        player.AutoJump = true;
        player.AutoJumpTimer = 0f;
        player.dashAttackTimer = 0f;
        player.varJumpSpeed = 0f;
        player.gliderBoostTimer = 0f;
        player.wallSlideTimer = 1.2f;
        player.wallBoostTimer = 0f;
        player.Speed.X = 0f;
        player.Speed.Y = 180f;
       
        player.launched = false;
        player.level.DirectionalShake(-Vector2.UnitY, 0.1f);
        player.SceneAs<Level>()?.DirectionalShake(GravityHelperInterop.IsPlayerInverted() ? (-Vector2.UnitY) : Vector2.UnitY, 0.1f);
        player.Sprite.Scale = new Vector2(0.5f, 1.5f);
        player.Collider = collider;
    }
    
    private bool HoldableHitCeilingSpring(Holdable holdable)
    {
        if (holdable.IsHeld)
            return false;
        
        Vector2 speed = holdable.GetSpeed();
        if (speed.Y > 0f)
            return false;
        
        if (holdable.Entity is Actor actorC)
            actorC.MoveV(Bottom - actorC.Top);
        speed.X *= 0.5f;
        speed.Y = 160f;
        holdable.SetSpeed(speed);
        
        if (holdable.Entity is Glider glider)
        {
            glider.noGravityTimer = 0.15f;
            glider.wiggler.Start();
        }
        else
            holdable.gravityTimer = 0.15f;
        
        return true;
    }

    private void DirectionCheck(Holdable holdable, Orientations orientation, Vector2 speed)
    {
        switch (orientation)
        {
            case Orientations.Floor:
                if (speed.Y < 0f)
                {
                    speed.X *= 0.5f;
                    speed.Y = -160f;
                    holdable.gravityTimer = 0.15f;
                    
                    if (holdable.Entity is Glider DCgliderF)
                        DCgliderF.wiggler.Start();
                    
                    if (holdable.Entity is Actor actorF)
                        actorF.MoveV(Top - actorF.Bottom);
                }
                
                break;
            
            case Orientations.WallLeft:
                if (speed.X > 0f)
                {
                    if (holdable.Entity is Glider DCgliderL)
                    {
                        speed.X = 160f;
                        DCgliderL.wiggler.Start();
                    }
                    else
                        speed.X = 220f;
                    
                    speed.Y = -80f;
                    holdable.gravityTimer = 0.1f;
                    
                    if (holdable.Entity is Actor actorR)
                    {
                        actorR.MoveV(Calc.Clamp(CenterRight.Y - actorR.Bottom, -4f, 4f));
                        actorR.MoveH(CenterRight.X - actorR.Left);
                    }
                }

                break;
            
            case Orientations.WallRight:
                if (speed.X < 0f)
                {
                    if (holdable.Entity is Glider DCgliderR)
                    {
                        speed.X = -160f;
                        DCgliderR.wiggler.Start();
                    }
                    else
                        speed.X = 220f;
                    
                    speed.Y = -80f;
                    holdable.gravityTimer = 0.1f;
                    
                    if (holdable.Entity is Actor actorL)
                    {
                        actorL.MoveV(Calc.Clamp(CenterLeft.Y - actorL.Bottom, -4f, 4f));
                        actorL.MoveH(CenterLeft.X - actorL.Right);
                    }
                }

                break;
            
            case Orientations.Ceiling:
                if (speed.Y > 0f)
                {
                    speed.X *= 0.5f;
                    speed.Y = 160f;
                    holdable.gravityTimer = 0.15f;
                    
                    if (holdable.Entity is Glider DCgliderC)
                        DCgliderC.wiggler.Start();
                    
                    if (holdable.Entity is Actor actorC)
                        actorC.MoveV(Bottom - actorC.Top);
                }

                break;
        }
        
        holdable.SetSpeed(speed);
    }

    private void bounceAnimate()
    {
        BounceAnimate();
        
        Level level = SceneAs<Level>();
        flagTrue = level.Session.GetFlag(_flagOnHit);

        if (!string.IsNullOrEmpty(_flagOnHit))
        {
            level.Session.SetFlag(_flagOnHit, !flagTrue || !_flagToggle);
        }
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Render()")]
    public void base_Render()
    {
        base.Render();
    }
    
    public override void Render()
    {
        if (Collidable && _drawOutline)
            sprite.DrawOutline();
        base_Render();
    }

    public new enum Orientations
    {
        Floor,
        WallLeft,
        WallRight,
        Ceiling,
    }

    public enum MomentumType
    {
        Off,
        PlayerOnly,
        HoldableOnly,
        Both
    }

    public enum BehindCollision
    {
        Off,
        HoldableOnly,
        PlayerOnly,
        Both
    }
}







