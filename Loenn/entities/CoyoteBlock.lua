local fakeTilesHelper = require("helpers.fake_tiles")

local reboundType = {
    ["Rebound"] = 0,
    ["Bounce"] = 1,
    ["Ignore"] = 2
}

local onHit = {
    ["No Break"] = 0,
    ["Respawn"] = 1,
    ["One Use"] = 2
}

local CoyoteBlock = {}

CoyoteBlock.name = "DawnHelper/coyoteBlock"
CoyoteBlock.depth = 0

function CoyoteBlock.placements()
return {
    name = "coyote_block",
    data = {
        tileType = fakeTilesHelper.getPlacementMaterial(),
        blendIn = true,
        playSound = true,
        playDebrisSound = true,
        coyoteTime = 0.5,
        freezeFrameTime = 0.05,
        breakSound = "event:/game/general/wall_break_stone",
        staminaRefill = true,
        refillAmount = 0,
        flagOnDash = "",
        dashCooldown = 0.05,
        respawnTime = 3,
        fast = false,
        onHit = 1,
        reboundType = 0,
        coyoteTimeAfterDash = 0.5,
        coyoteOnlyWhenDashing = false,
        width = 8,
        height = 8
    }
}
end

CoyoteBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendIn")

CoyoteBlock.fieldOrder = {
    "x", "y",
    "width", "height",
    "tileType", "reboundType", "onHit",
    "refillAmount", "breakSound",
    "flagOnDash", "freezeFrameTime",
    "dashCooldown", "respawnTime",
    "coyoteTime", "coyoteTimeAfterDash",
    "coyoteOnlyWhenDashing", "staminaRefill", "blendIn",
    "fast", "playSound", "playDebrisSound"
}

CoyoteBlock.fieldInformation = {
    tileType = {
        options = function() return fakeTilesHelper.getTilesOptions() end,
        editable = false,
    },

    reboundType = {
        editable = false,
        options = reboundType
    },

    onHit = {
        editable = false,
        options = onHit
    }
}

return CoyoteBlock
