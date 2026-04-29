using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace Celeste.Mod.DawnHelper.Entities;

[CustomEntity("DawnHelper/nodeRefill")]
public class NodeRefill : Refill
{
    private Vector2[] nodes;
    private int index;
    private int outlineIndex;
    private float respawnTime;
    private bool restart;
    private bool drawPath;
    private bool drawOutlines;
    private Vector2 startPosition;
    private static Color COLOR_TWODASH;
    private static Color COLOR_ONEDASH;
    private Color LineColor => twoDashes ? COLOR_TWODASH : COLOR_ONEDASH;

    // evaluates the position at which particles should be emitted
    private Vector2 ParticlePosition => index == 0 ? Position : nodes[index - 1];

    public NodeRefill(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        restart = data.Bool("restart");
        COLOR_ONEDASH = Calc.HexToColorWithAlpha(data.Attr("OneDashLineColor"));
        COLOR_TWODASH = Calc.HexToColorWithAlpha(data.Attr("TwoDashLineColor"));
        respawnTime = data.Float("respawnTime");
        drawPath = data.Bool("drawPath");
        drawOutlines = data.Bool("drawOutlines");
        nodes = data.NodesOffset(offset);
        Get<PlayerCollider>().OnCollide = OnPlayer;
        startPosition = data.Position + offset;
    }

    // this tells MonoMod "hey, any calls to this method should be replaced with Entity.Update()"
    // this helps skip functionality from Refill as we're reimplementing it
    [MonoModLinkTo("Monocle.Entity", "System.Void Update()")]
    public void base_Update()
    {
        base.Update();
    }

    public override void Update()
    {
        base_Update();
        if (respawnTimer > 0f)
        {
            respawnTimer -= Engine.DeltaTime;
            if (respawnTimer <= 0f)
                foundyouRespawn();
        }
        else if (Scene.OnInterval(0.1f))
            level.ParticlesFG.Emit(p_glow, 1, ParticlePosition, Vector2.One * 5f);

        UpdatePosition();

        light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
        bloom.Alpha = light.Alpha * 0.8f;

        if (Scene.OnInterval(2f) && sprite.Visible)
        {
            flash.Play("flash", restart: true);
            flash.Visible = true;
        }
    }

    private void foundyouRespawn()
    {
        if (Collidable)
            // don't respawn if we're already there
            return;

        Collidable = true;
        sprite.Visible = true;
        outline.Visible = false;
        Depth = Depths.Pickups;
        wiggler.Start();
        Audio.Play(twoDashes ? SFX.game_10_pinkdiamond_return : SFX.game_gen_diamond_return, Position);
        level.ParticlesFG.Emit(p_regen, 16, ParticlePosition, Vector2.One * 2f);
    }

    public void UpdatePosition()
    {
        Vector2 fixedPosition = Collider.Position + Vector2.One * 8;
        sprite.Position = flash.Position = bloom.Position = light.Position = fixedPosition;
    }

    public override void Render()
    {
        base.Render();
        if (drawOutlines)
        {
            outline.Texture.DrawCentered(startPosition);
            for (outlineIndex = 0; outlineIndex < nodes.Length; outlineIndex++)
                outline.Texture.DrawCentered(nodes[outlineIndex]);
        }

        if (drawPath)
        {
            Draw.Line(startPosition, nodes[0], LineColor);
            for (outlineIndex = 0; outlineIndex < nodes.Length - 1; outlineIndex++)
                Draw.Line(nodes[outlineIndex], nodes[outlineIndex + 1], LineColor);
        }
    }

    private IEnumerator CustomRefillRoutine(Player player)
    {
        Celeste.Freeze(0.05f);
        yield return null;

        level.Shake();
        sprite.Visible = flash.Visible = false;
        Depth = Depths.BGDecals - 1;
        yield return 0.05f;

        float dashDirection = player.Speed.Angle();
        level.ParticlesFG.Emit(p_shatter, 5, ParticlePosition, Vector2.One * 4f, dashDirection - Calc.QuarterCircle);
        level.ParticlesFG.Emit(p_shatter, 5, ParticlePosition, Vector2.One * 4f, dashDirection + Calc.QuarterCircle);
        SlashFx.Burst(ParticlePosition, dashDirection);
    }

    private new void OnPlayer(Player player)
    {
        if (!player.UseRefill(twoDashes))
            // can't collect the refill, nothing to do
            return;

        Audio.Play(twoDashes ? SFX.game_10_pinkdiamond_touch : SFX.game_gen_diamond_touch, Position);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        Collidable = false;
        Add(new Coroutine(CustomRefillRoutine(player)));
        respawnTimer = respawnTime;
        if (index < nodes.Length)
        {
            Collider.Position = nodes[index] - Position - Vector2.One * 8;
            ++index;
            level.ParticlesFG.Emit(p_glow, 1, ParticlePosition, Vector2.One * 5f);
        }
        else if (restart)
        {
            Collider.CenterOrigin();
            index = 0;
        }
        else
            RemoveSelf();
    }
}
