using System.Reflection;

namespace Ovl.Biz.Services.Equipment;

/// <summary>
/// 设备服务占位代理：为 vendor 的设备/硬件服务接口生成空实现，
/// 使页面 ViewModel 在无真机环境下也能完成构造与渲染。
/// <para>
/// 默认返回：<c>void</c> → 无操作；<c>Task</c> → 已完成；<c>Task&lt;T&gt;</c> → 默认值；
/// 值类型 → <c>default</c>；引用类型 → <c>null</c>。
/// </para>
/// <para>
/// 可通过 <c>overrides</c> 按方法名覆写少量成员（例如让"真空检测"返回 true），
/// 让界面呈现合理状态；真机接入时替换为正式实现（RealDevice 阶段）。
/// </para>
/// </summary>
public class DefaultServiceProxy : DispatchProxy
{
    private IReadOnlyDictionary<string, Func<object?[]?, object?>>? _overrides;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            return null;
        }

        if (_overrides is not null && _overrides.TryGetValue(targetMethod.Name, out var handler))
        {
            return handler(args);
        }

        var returnType = targetMethod.ReturnType;

        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = returnType.GetGenericArguments()[0];
            var innerValue = innerType.IsValueType ? Activator.CreateInstance(innerType) : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(innerType)
                .Invoke(null, [innerValue]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    /// <summary>为指定服务接口创建空实现实例。</summary>
    /// <param name="overrides">按方法名覆写的成员，键为方法名，值为 (参数数组) → 返回值。</param>
    public static T Create<T>(IReadOnlyDictionary<string, Func<object?[]?, object?>>? overrides = null)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, DefaultServiceProxy>();
        ((DefaultServiceProxy)(object)proxy)._overrides = overrides;
        return proxy;
    }
}
