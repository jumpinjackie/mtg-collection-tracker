﻿using System.Globalization;
using System.Text;

namespace MtgCollectionTracker.Data;

public class Utils
{
    public static string? NormalizeCardName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString.EnumerateRunes())
        {
            var unicodeCategory = Rune.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
    }
}
