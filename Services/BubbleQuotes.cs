namespace PetWindow.Services;

public static class BubbleQuotes
{
    private static readonly string[] Lines =
    {
        "摸鱼使我快乐。",
        "记得喝水哦。",
        "代码写完了吗？",
        "今天也要加油。",
        "别盯着我，去干活。",
        "右键菜单有更多选项。",
        "拖到边缘我会吸附。",
        "最小化到托盘就不占任务栏啦。"
    };

    private static int _i;

    public static string Next()
    {
        var s = Lines[_i % Lines.Length];
        _i++;
        return s;
    }
}
