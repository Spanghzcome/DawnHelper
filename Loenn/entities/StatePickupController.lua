local StatePickupController = {}

StatePickupController.name = "DawnHelper/statePickupController"
StatePickupController.texture = "DawnHelper/LoennIcons/StatePickupController"
StatePickupController.placements = {
    name = "StatePickupController",
    data = {
        persistent = true,
        pickupInSwim = true,
        pickupInFeather = true,
        pickupInRedBooster = true,
        pickupInSummitLaunch = true,
        resetToNormal = false
    }
}

return StatePickupController
