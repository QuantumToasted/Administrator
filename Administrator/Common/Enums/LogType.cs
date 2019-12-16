namespace Administrator.Common
{
    public enum LogType
    {
        Disable,
        Ban,
        Unban,
        Kick,
        Mute,
        Unmute,
        Warn,
        Unwarn, // TODO: Better name
        Modmail,
        Appeal,
        Suggestion,
        SuggestionArchive, // TODO: Better name?
        MessageDelete,
        MessageUpdate,
        Join,
        Leave,
        UsernameUpdate,
        NicknameUpdate,
        AvatarUpdate,
        UserRoleUpdate
    }
}