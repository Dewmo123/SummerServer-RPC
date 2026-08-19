namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcMethodRegistry
{
    private readonly IReadOnlyDictionary<string, IJsonRpcMethodDefinition> _methods;

    public JsonRpcMethodRegistry(IEnumerable<IJsonRpcMethodDefinition> definitions)
    {
        // JSON-RPC 메서드명은 계약상 대소문자를 구분하므로 Ordinal 비교를 사용한다.
        Dictionary<string, IJsonRpcMethodDefinition> methods = new(StringComparer.Ordinal);

        foreach (IJsonRpcMethodDefinition definition in definitions)
        {
            if (!methods.TryAdd(definition.MethodName, definition))
            {
                throw new InvalidOperationException($"JSON-RPC 메서드가 중복 등록되었습니다: {definition.MethodName}");
            }
        }

        _methods = methods;
    }

    public bool TryGetMethod(string methodName, out IJsonRpcMethodDefinition? definition) =>
        _methods.TryGetValue(methodName, out definition);
}