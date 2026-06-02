local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local orientation = {
    Up = "Floor",
    Left = "WallRight",
    Down = "Ceiling",
    Right = "WallLeft"
}

local momentumType = {
    "Off",
    "HoldableOnly",
    "PlayerOnly",
    "Both"
}

local collisionFromBehind = {
    "Neither",
    "HoldableOnly",
    "PlayerOnly",
    "Both"
}

local VersatileSpring = {}
VersatileSpring.name = "DawnHelper/versatileSpring"
VersatileSpring.depth = -8501
VersatileSpring.placements = {
    {
        name = "spring",
        data = {
            orientation = orientation.Right,
            momentumType = "Off",
            collisionFromBehind = "Both",
            cursed = false,
            sprite = "",
            playerCanUse = true,
            holdablesCanUse = true,
            flagOnHit = "",
            toggleFlag = true,
            invertedVerticalMomentum = false,
            drawOutline = true
        }
    },
    {
        name = "momentum",
        data = {
            orientation = orientation.Right,
            momentumType = "Both",
            collisionFromBehind = "Both",
            cursed = false,
            sprite = "",
            playerCanUse = true,
            holdablesCanUse = true,
            flagOnHit = "",
            toggleFlag = true,
            invertedVerticalMomentum = false,
            drawOutline = true
        }
    }
}

VersatileSpring.fieldInformation = {
    momentumType = {
        editable = false,
        options = momentumType
    },
    orientation = {
        editable = false,
        options = orientation
    },
    collisionFromBehind = {
        editable = false,
        options = collisionFromBehind
    }
}

VersatileSpring.fieldOrder = {
    "x", "y",
    "orientation",
    "momentumType", "collisionFromBehind"
}


function VersatileSpring.sprite(room, entity, viewport)
    local sprite
    local spritePath = entity.sprite:gsub("/+$", "").."/"

    if entity.momentumType == "Off" and entity.sprite == "" then
        sprite = drawableSprite.fromTexture("objects/spring/00", entity)
    elseif entity.sprite == "" then
        sprite = drawableSprite.fromTexture("objects/DawnHelper/springGreen/00", entity)
    else
        sprite = drawableSprite.fromTexture(spritePath.."00", entity)
    end

    sprite:setJustification(0.5, 1)
    local orientation = entity.orientation or "Floor"
    if orientation == "WallLeft" then
        sprite.rotation = math.pi / 2
    elseif orientation == "WallRight" then
        sprite.rotation = -math.pi / 2
    elseif orientation == "Ceiling" then
        sprite.rotation = math.pi
    end

    return sprite
end

function VersatileSpring.selection(room, entity)
    local orientation = entity.orientation or "Floor"
    if orientation == "WallLeft" then
        return utils.rectangle(entity.x, entity.y - 6, 5, 12)
    elseif orientation == "WallRight" then
        return utils.rectangle(entity.x - 5, entity.y - 6, 5, 12)
    elseif orientation == "Ceiling" then
        return utils.rectangle(entity.x - 6, entity.y, 12, 5)
    else
        return utils.rectangle(entity.x - 6, entity.y - 5, 12, 5)
    end
end

function VersatileSpring.rotate(room, entity, direction)
    if entity.orientation == orientation.Up then
        entity.orientation = orientation.Left
    elseif entity.orientation == orientation.Left then
        entity.orientation = orientation.Down
    elseif entity.orientation == orientation.Down then
        entity.orientation = orientation.Right
    elseif entity.orientation == orientation.Right then
        entity.orientation = orientation.Up
    end

    return true
end

function VersatileSpring.flip(room, entity, horizontal, vertical)
    if entity.orientation == orientation.Up then
        entity.orientation = orientation.Down
    elseif entity.orientation == orientation.Down then
        entity.orientation = orientation.Up
    elseif entity.orientation == orientation.Right then
        entity.orientation = orientation.Left
    elseif entity.orientation == orientation.Left then
        entity.orientation = orientation.Right
    end

    return true
end
return VersatileSpring
