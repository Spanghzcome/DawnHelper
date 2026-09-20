local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local SuperDashBumper = {}

local dashType = {
    ["Dash"] = 0,
    ["SuperDash"] = 1,
    ["RedDash"] = 2
}

SuperDashBumper.name = "DawnHelper/superDashBumper"
SuperDashBumper.nodeLineRenderType = "line"
SuperDashBumper.depth = 0
SuperDashBumper.placements = {
    {
        name = "DashBumper",
        data = {
            verticalDashStretching = false,
            dashType = 0,
            static = true,
            alwaysBoost = false,
            fast = false,
            respawnTimer = 0.6,
            consumeDash = false,
            noRefill = false,
            launchDashSpeed = 280,
            eightWayDash = false,
            horizontalSnap = true
        }
    },
    {
        name = "SuperDashBumper",
        data = {
            verticalDashStretching = false,
            dashType = 1,
            static = true,
            alwaysBoost = false,
            fast = false,
            respawnTimer = 0.6,
            consumeDash = false,
            noRefill = false,
            launchDashSpeed = 280,
            eightWayDash = false,
            horizontalSnap = true
        }
    },
    {
        name = "RedDashBumper",
        data = {
            verticalDashStretching = true,
            dashType = 2,
            static = true,
            alwaysBoost = false,
            fast = false,
            respawnTimer = 0.6,
            consumeDash = false,
            noRefill = false,
            launchDashSpeed = 280,
            eightWayDash = false,
            horizontalSnap = true
        }
    }
}

SuperDashBumper.fieldInformation = {
    dashType = {
        editable = false,
        options = dashType
    }
}

function SuperDashBumper.texture(room, entity)
    local spriteType

    if entity.dashType == 2 then
        spriteType = "objects/DawnHelper/redDashBumper/Idle18"
    elseif entity.dashType == 1 then
        spriteType = "objects/DawnHelper/superDashBumper/Idle18"
    else
        spriteType = "objects/DawnHelper/dashBumper/Idle18"
    end

    return spriteType
end

function SuperDashBumper.selection(room, entity)
    local main = utils.rectangle(entity.x - 12, entity.y - 12, 24, 24)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x - 12, node.y - 12, 24, 24)
        end
    end

    return main, nodes
end

SuperDashBumper.nodeLimits = {0, 1}

return SuperDashBumper
