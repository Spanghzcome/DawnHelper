local drawableSpriteStruct = require("structs.drawable_sprite")
local drawing = require("utils.drawing")
local utils = require("utils")
local enums = require("consts.celeste_enums")

local textures = {"wood", "dream", "temple", "templeB", "cliffside", "reflection", "core", "moon"}

local function getTexture(entity)
    return entity.texture and entity.texture ~= "default" and entity.texture or "wood"
end

local ExtendingJumpThru = {}

ExtendingJumpThru.name = "DawnHelper/extendingJumpThru"
ExtendingJumpThru.depth = -9000
ExtendingJumpThru.canResize = {true, false}
ExtendingJumpThru.nodeLimits = {1, 2}
ExtendingJumpThru.nodeLineRenderType = "line"
ExtendingJumpThru.nodeVisibility = "always"
ExtendingJumpThru.fieldInformation = {
    texture = {
        options = textures
    },
    surfaceIndex = {
        options = enums.tileset_sound_ids,
        fieldType = "integer"
    }
}
ExtendingJumpThru.placements = {}

for i, texture in ipairs(textures) do
    ExtendingJumpThru.placements[i] = {
        name = texture,
        data = {
            width = 8,
            texture = texture,
            surfaceIndex = -1,
            left = false,
            liftSpeedMultiplier = 1,
            extendLength = 64,
            speed = 240,
            startExtended = false
        }
    }
end

function ExtendingJumpThru.sprite(room, entity)
    local textureRaw = getTexture(entity)
    local texture = "objects/jumpthru/" .. textureRaw

    local x, y = entity.x or 0, entity.y or 0
    local width = entity.width or 8

    local startX, startY = math.floor(x / 8) + 1, math.floor(y / 8) + 1
    local stopX = startX + math.floor(width / 8) - 1
    local len = stopX - startX

    local sprites = {}

    for i = 0, len do
        local quadX = 8
        local quadY = 8

        if i == 0 then
            quadX = 0
            quadY = room.tilesFg.matrix:get(startX - 1, startY, "0") ~= "0" and 0 or 8

        elseif i == len then
            quadY = room.tilesFg.matrix:get(stopX + 1, startY, "0") ~= "0" and 0 or 8
            quadX = 16
        end

        local sprite = drawableSpriteStruct.fromTexture(texture, entity)

        sprite:setJustification(0, 0)
        sprite:addPosition(i * 8, 0)
        sprite:useRelativeQuad(quadX, quadY, 8, 8)

        table.insert(sprites, sprite)
    end

    return sprites
end


return ExtendingJumpThru
