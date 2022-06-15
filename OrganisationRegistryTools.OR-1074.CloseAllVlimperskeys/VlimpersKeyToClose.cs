namespace OrganisationRegistryTools.OR_1074.CloseAllVlimperskeys;

public record VlimpersKeyToClose(Guid OrganisationId, Guid KeyTypeId, string KeyValue, DateTime? ValidFrom, DateTime? ValidTo);
