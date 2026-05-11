namespace PenguinRun
{
    internal enum PowerUpKind
    {
        Dash,
        Magnet,
        Shield,
        ScoreStar,
        GlideFeather,
        DoubleFishSnack,
        TimeHourglass,
        BubbleShield,      // 海洋专属：泡泡护盾，可在水中短暂冲刺
        SeahorseBoost,     // 海洋专属：海马加速，获得爆发速度
        CloudWalk,         // 天空专属：踏云而行，可空中二段跳
        WindRider,         // 天空专属：乘风滑翔，延长滑翔时间
        FishBomb,          // 通用：鱼形炸弹，捡到后下次受伤可自动反弹一次（额外护盾）
        SecondHeart,       // 通用：额外生命，最多让上限+1
    }
}
