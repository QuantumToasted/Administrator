using System.ComponentModel;

namespace Administrator.Common
{
    public enum EmojiType
    {
        [Description("⬆️")]
        Upvote,
        [Description("⬇️")]
        Downvote,
        [Description("🎉")]
        LevelUp,
        [Description("⭐")]
        Star
    }
}