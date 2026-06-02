local drawableFunc = require("structs.drawable_function")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local BlapSwock = {}

BlapSwock.name = "DawnHelper/blapSwock"
BlapSwock.depth = -9999
BlapSwock.color = {1.0, 1.0, 1.0}
BlapSwock.nodeLimits = {1, 1}
BlapSwock.nodeLineRenderType = "line"
BlapSwock.nodeVisibility = "always"
BlapSwock.placements = {
    name = "BlapSwock",
    data = {
        spriteXMLName = "blapSwock",
        targetSprite = "objects/DawnHelper/blapSwock/target",
        retainDashDirection = true,
        dashDirectionSpeedRetention = true,
        speedRetention = true,
        radius = 75
    }
}

BlapSwock.sprite = function(room, entity)
return {
    drawableSprite.fromTexture("objects/DawnHelper/blapSwock/idle00", entity),
    drawableFunc.fromFunction(function() love.graphics.circle("line", entity.x, entity.y, entity.radius) end)
}
end

BlapSwock.nodeTexture = BlapSwock.placements.data.targetSprite

function BlapSwock.selection(room, entity)
return utils.rectangle(entity.x - 12, entity.y - 12, 24, 24), {utils.rectangle(entity.nodes[1].x - 10, entity.nodes[1].y - 10, 20, 20)}
end

return BlapSwock
