using System.Reflection;
using System.Threading.Tasks;

namespace Ovl.UI.Services.Stubs;

/// <summary>
/// 无硬件环境下的服务占位代理：为任意 vendor 服务接口生成"空实现"，
/// 所有成员返回默认值（void → 无操作，Task → 已完成，值类型 → default，引用类型 → null）。
/// <para>
/// 用途：真机服务（运动控制、晶圆传输、视觉等）尚未接入时，页面 VM 的构造函数仍可正常完成，
/// 从而让界面先行迁移到位、可视化验证。真机实现属 RealDevice 硬件工厂阶段替换。
/// </para>
/// <para>
/// 注意：调用方若对返回值立即解引用（如 <c>proxy.GetXxx().Foo()</c>），仍会 NRE；
/// 遇到此类调用应在 VM 侧判空，而不是让桩返回伪造对象。
/// </para>
/// </summary>
public class NullServiceProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            return null;
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
    public static T Create<T>() where T : class => DispatchProxy.Create<T, NullServiceProxy>();
}
