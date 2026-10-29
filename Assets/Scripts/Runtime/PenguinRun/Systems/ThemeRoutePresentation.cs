namespace PenguinRun
{
    internal static class ThemeRoutePresentation
    {
        public static string ChoiceLabels(RouteEventSnapshot snapshot)
        {
            if (!snapshot.AwaitingChoice) return string.Empty;
            var action = snapshot.Id switch
            {
                "ice-fracture" => "跳跃线",
                "cedar-rescue" => "滑铲线",
                _ => string.Empty,
            };
            return action.Length == 0
                ? $"左道 安心：{snapshot.SafeLabel}    右道 冒险：{snapshot.RiskLabel}"
                : $"左道 稳健补给：{snapshot.SafeLabel}\n右道 {action}：{snapshot.RiskLabel}";
        }
    }
}
