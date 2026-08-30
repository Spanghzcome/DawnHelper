local drawableSprite = require("structs.drawable_sprite")

local SeaGlide = {}

SeaGlide.name = "DawnHelper/seaGlide"
SeaGlide.depth = 100
SeaGlide.placements = {
    name = "sea_glide",
    data = {
        holdJumpToActivate = false,
        maxGlideSpeed = 190,
        glideDashSpeed = 320,
        glideDashCooldown = 2,
        spriteXMLName = "seaGlide"
    }
}

SeaGlide.fieldOrder = {
    "x", "y",
    "maxGlideSpeed", "glideDashSpeed",
    "glideDashCooldown", "spriteXMLName"
}

local offsetY = -8
local texture = "objects/DawnHelper/seaGlide/idle"

function SeaGlide.sprite(room, entity)
local sprite = drawableSprite.fromTexture(texture, entity)
local outlineSprite1 = drawableSprite.fromTexture(texture, entity)
local outlineSprite2 = drawableSprite.fromTexture(texture, entity)
local outlineSprite3 = drawableSprite.fromTexture(texture, entity)
local outlineSprite4 = drawableSprite.fromTexture(texture, entity)

outlineSprite1:setColor({0,0,0})
outlineSprite2:setColor({0,0,0})
outlineSprite3:setColor({0,0,0})
outlineSprite4:setColor({0,0,0})

sprite.y += offsetY
outlineSprite1.y += offsetY + 1
outlineSprite2.y += offsetY - 1
outlineSprite3.y += offsetY
outlineSprite4.y += offsetY

outlineSprite3.x += 1
outlineSprite4.x -= 1

return {outlineSprite1, outlineSprite2, outlineSprite3, outlineSprite4, sprite}
end

return SeaGlide
