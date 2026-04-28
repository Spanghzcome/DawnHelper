local NodeRefill = {}

NodeRefill.name = "DawnHelper/nodeRefill"
NodeRefill.depth = -100
NodeRefill.nodeLimits = {1, -1}
NodeRefill.nodeLineRenderType = "line"
NodeRefill.nodeVisiblity = "always"
NodeRefill.placements = {
    {
        name = "oneDash",
        data = {
            restart = false,
            twoDash = false,
            drawPath = true,
            drawOutlines = true,
            respawnTime = "0.03",
            OneDashLineColor = "007C00",
            TwoDashLineColor = "F4C1CD"
        }
    },
    {
        name = "twoDash",
        data = {
            restart = false,
            twoDash = true,
            drawPath = true,
            drawOutlines = true,
            respawnTime = "0.03",
            OneDashLineColor = "007C00",
            TwoDashLineColor = "F4C1CD"
        }
    }
}

function NodeRefill.texture(room, entity)
    return entity.twoDash and "objects/refillTwo/idle00" or "objects/refill/idle00"
end

NodeRefill.fieldInformation = {
    OneDashLineColor = {
        fieldType = "color",
        useAlpha = true
    },
    TwoDashLineColor = {
        fieldType = "color",
        useAlpha = true
    }
}

return NodeRefill
