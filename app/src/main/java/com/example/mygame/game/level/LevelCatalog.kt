package com.example.mygame.game.level

object LevelCatalog {
    fun build(level: GameLevel, worldWidth: Float, worldHeight: Float): LevelContent =
        when (level) {
            GameLevel.CedarVillageRuins -> LevelOneData.build(worldWidth, worldHeight)
            GameLevel.IceLakeEchoValley -> LevelTwoData.build(worldWidth, worldHeight)
            GameLevel.MistDike -> LevelThreeData.build(worldWidth, worldHeight)
        }
}
