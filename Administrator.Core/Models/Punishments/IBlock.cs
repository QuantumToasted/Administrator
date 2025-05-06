using Disqord;

namespace Administrator.Core;

public interface IBlock : IRevocablePunishment, IChannelEntity, IExpiringEntity;