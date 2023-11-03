using Npgsql.Internal.TypeHandlers;
using Npgsql.Internal;
using Npgsql.Internal.TypeHandling;
using System.Text.Json;
using Administrator.Core;

namespace Administrator.Database;

// see https://github.com/npgsql/efcore.pg/issues/1107#issuecomment-945126627
public sealed class CustomJsonSerializerTypeHandlerResolverFactory(JsonSerializerOptions options) : TypeHandlerResolverFactory
{
    public override TypeHandlerResolver Create(NpgsqlConnector connector)
        => new JsonOverrideTypeHandlerResolver(connector, options);

    public override string? GetDataTypeNameByClrType(Type clrType)
        => null;

    public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
        => null;

    private class JsonOverrideTypeHandlerResolver : TypeHandlerResolver
    {
        private readonly JsonHandler? _jsonbHandler;

        internal JsonOverrideTypeHandlerResolver(NpgsqlConnector connector, JsonSerializerOptions options)
            => _jsonbHandler ??= new JsonHandler(
                connector.DatabaseInfo.GetPostgresTypeByName("jsonb"),
                connector.TextEncoding,
                isJsonb: true,
                options);

        public override NpgsqlTypeHandler? ResolveByDataTypeName(string typeName)
            => typeName == "jsonb" ? _jsonbHandler : null;

        public override NpgsqlTypeHandler? ResolveByClrType(Type type)
            // You can add any user-defined CLR types which you want mapped to jsonb
            => type == typeof(JsonDocument) || type == typeof(JsonMessage)
                ? _jsonbHandler
                : null;

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => null; // Let the built-in resolver do this
    }
}