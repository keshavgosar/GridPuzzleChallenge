namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Unity's .NET Standard 2.1 scripting backend doesn't ship this marker
    /// type, even though the compiler understands C# 9 'init' syntax.
    /// This empty polyfill satisfies the compiler with zero runtime cost.
    /// </summary>
    internal static class IsExternalInit { }
}
