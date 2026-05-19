local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local enums = require("consts.celeste_enums")

local NodeSwapBlock = {}

NodeSwapBlock.name = "DawnHelper/nodeSwapBlock"
NodeSwapBlock.depth = -9999
NodeSwapBlock.color = {1.0, 1.0, 1.0}
NodeSwapBlock.nodeLimits = {1, -1}
NodeSwapBlock.nodeLineRenderType = "line"
NodeSwapBlock.nodeVisibility = "always"
NodeSwapBlock.placements = {
        name = "normal",
        data = {
            width = 16,
            height = 16,
            toggle = true,
            renderBG = false,
            noLoop = false,
            speed = 360,
            returnSpeed = 360 * 0.4,
            drawPath = true,
            directory = "objects/swapblock",
            particleColor1 = "fbf236",
            particleColor2 = "6abe30",
            emitParticles = true
        }
}

return NodeSwapBlock

