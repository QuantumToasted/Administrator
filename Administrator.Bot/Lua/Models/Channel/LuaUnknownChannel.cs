using Disqord;

namespace Administrator.Bot;

public class LuaUnknownChannel(IChannel channel) : LuaChannel(channel), ILuaModel<LuaUnknownChannel>;