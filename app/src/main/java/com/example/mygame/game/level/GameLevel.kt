package com.example.mygame.game.level

enum class GameLevel {
    CedarVillageRuins,
    IceLakeEchoValley,
    /** 第三关占位；体量化与正式机制在 [LevelThreeData] 中迭代。 */
    MistDike,
    ;

    /**
     * 主线通关后接续的下一关；最后一关为 `null`，仍停留在本关并交由存档与菜单处理。
     */
    fun storyNext(): GameLevel? = when (this) {
        CedarVillageRuins -> IceLakeEchoValley
        IceLakeEchoValley -> MistDike
        MistDike -> null
    }
}
