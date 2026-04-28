local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local SuperDashBumper = {}

SuperDashBumper.name = "DawnHelper/superDashBumper"
SuperDashBumper.nodeLineRenderType = "line"
SuperDashBumper.depth = 0
SuperDashBumper.placements = {
    {
        name = "DashBumper",
        data = {
            verticalDashStretching = false,
            static = true,
            soup = false,
            alwaysBoost = false,
            fast = false,
            respawnTimer = 0.6,
            launchDashSpeed = 280
        }
    },
    {
        name = "DashBumper(Fast)",
        data = {
            verticalDashStretching = true,
            static = true,
            soup = false,
            alwaysBoost = false,
            fast = true,
            respawnTimer = 0.6,
            launchDashSpeed = 280
        }
    },
    {
        name = "SuperDashBumper",
        data = {
            verticalDashStretching = false,
            static = true,
            soup = true,
            alwaysBoost = false,
            fast = false,
            respawnTimer = 0.6,
            launchDashSpeed = 280
        }
    },
    {
        name = "SuperDashBumper(Fast)",
        data = {
            verticalDashStretching = true,
            static = true,
            soup = true,
            alwaysBoost = false,
            fast = true,
            respawnTimer = 0.6,
            launchDashSpeed = 280
        }
    }
}

function SuperDashBumper.texture(room, entity)
    return entity.soup and "objects/DawnHelper/superDashBumper/Idle18" or "objects/DawnHelper/dashBumper/Idle18"
end

function SuperDashBumper.selection(room, entity)
    return utils.rectangle(entity.x - 12, entity.y -12, 24, 24)
end

SuperDashBumper.nodeLimits = {0, -1}

return SuperDashBumper
