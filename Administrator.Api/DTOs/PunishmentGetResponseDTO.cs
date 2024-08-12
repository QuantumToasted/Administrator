namespace Administrator.Api;

public sealed record PunishmentGetResponseDTO(IReadOnlyList<PunishmentDTO> Punishments, int? Next);