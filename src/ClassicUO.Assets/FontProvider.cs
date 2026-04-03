using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using SixLabors.Fonts;

namespace ClassicUO.Assets;

/// <summary>
///     Represents a collection of font faces organized by their family name.
/// </summary>
/// <param name="FamilyName">
///     The name of the font family associated with the collection of font faces.
/// </param>
/// <param name="FontFaces">
///     An array of byte arrays, where each byte array represents a raw font file belonging to the family.
/// </param>
public record struct FontsByFamily(string FamilyName, byte[][] FontFaces);

/// <summary>
///     Provides methods to retrieve system font data
/// </summary>
public static class FontProvider
{
    /// Retrieves system fonts grouped by font family.
    /// <returns>
    ///     An enumerable collection of FontsByFamily objects, where each object represents a font family
    ///     along with its corresponding font data. If a font family cannot be resolved or has no fonts,
    ///     it is excluded from the returned collection.
    /// </returns>
    /// <remarks>
    ///     The returned collection is a 'yielded' one, meaning it should be iterated once.
    ///     Multiple iterations will result in computation overhead.
    /// </remarks>
    public static IEnumerable<FontsByFamily> GetSystemFonts() =>
        from family in SystemFonts.Families
        select GetSystemFontsByFamily(family)
        into @group
        where @group.HasValue
        select @group.Value;

    /// <summary>
    ///     Retrieves the font faces and the corresponding family name for a given font family.
    /// </summary>
    /// <param name="family">The font family from which font data will be retrieved.</param>
    /// <returns>
    ///     A <see cref="FontsByFamily" /> structure containing the family name and its font faces
    ///     represented as byte arrays or <c>null</c> if the font data could not be retrieved.
    /// </returns>
    private static FontsByFamily? GetSystemFontsByFamily(FontFamily family)
    {
        if (!family.TryGetPaths(out IEnumerable<string> familyPaths))
        {
            Log.Warn($"Could not obtain physical font paths for family '{family.Name}'");
            return null;
        }

        ConcurrentBag<byte[]> fontData = [];
        Task ioTasks = Parallel.ForEachAsync(familyPaths, async (path, cToken) =>
        {
            try
            {
                fontData.Add(await File.ReadAllBytesAsync(path, cToken));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read font file '{path}' - {e.Message}");
            }
        });

        Task.WaitAll(ioTasks);

        if (fontData.IsEmpty)
            return null;

        return new FontsByFamily(family.Name, fontData.ToArray());
    }
}
