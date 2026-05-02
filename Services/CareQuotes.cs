namespace PetWindow.Services;

/// <summary>宠物主动关心的文案（与点击吐槽分开）。</summary>
public static class CareQuotes
{
    private static readonly string[] Lines =
    {
        "嗨，我在这里陪着你呢～",
        "有没有好好吃饭？别饿着呀。",
        "久坐对身体不好，起来伸个懒腰吧。",
        "今天也辛苦了，给自己点个赞。",
        "眼睛累的话看看远处，休息一下。",
        "喝水了吗？我去不了厨房，但你可以～",
        "心情不好的话，深呼吸三次试试。",
        "进度慢一点也没关系，稳稳地来。",
        "夜里别熬太晚，身体最重要。",
        "有需要就右键叫我，我一直都在。",
        "天气多变，出门记得看温度哦。",
        "工作久了，记得活动一下手腕。",
        "你今天已经很棒了，别对自己太苛刻。",
        "放松一下，音乐或散步都很治愈。",
        "记得和家人朋友聊聊天呀。"
    };

    private static readonly Random Rng = new();

    public static string Next() => Lines[Rng.Next(Lines.Length)];
}
