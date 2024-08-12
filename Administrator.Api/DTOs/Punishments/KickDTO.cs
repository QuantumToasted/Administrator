using Administrator.Core;

namespace Administrator.Api;

public sealed class KickDTO(IKick kick) : PunishmentDTO(kick), IKick;