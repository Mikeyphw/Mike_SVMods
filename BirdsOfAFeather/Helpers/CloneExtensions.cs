namespace Mikew.BirdsOfAFeather.Helpers;

/// <summary>
/// Clone helpers that avoid leaking types into <c>StardewValley.*</c> namespaces.
/// </summary>
public static class CloneExtensions
{
    public static T? CloneOrDeepClone<T>(this T? source, bool deepClone) where T : class
    {
        return ObjectCloner.Clone(source, deepClone ? ObjectCloner.CloneType.Deep : ObjectCloner.CloneType.Shallow);
    }
}
