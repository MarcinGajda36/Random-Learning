using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MarcinGajda.Collections;

public class WeakTableProd
{
    private static readonly ConditionalWeakTable<string, Dictionary<string, string>> LangDictionaryCache =
        new ConditionalWeakTable<string, Dictionary<string, string>>();

    public void LangDictionaryUser()
    {

    }

    public async Task<Dictionary<string, string>> LangDictionary(string lang, string authorization)
    {
        //var langCode = Lang2Iso639.Iso2Char(lang);
        string langCode = lang;
        if (LangDictionaryCache.TryGetValue(langCode, out Dictionary<string, string>? cachedIdWordPairs))
        {
            return cachedIdWordPairs;
        }

        var idWordPairs = new Dictionary<string, string>();
        string langContent = await GetLang(authorization, langCode);
        IEnumerable<IEnumerable<int>> jobject = Enumerable.Range(0, 10).Select(i => Enumerable.Range(0, i));
        foreach (IEnumerable<int> pair in jobject)
        {
            foreach (int jproperty in pair)
            {
                string keyWordId = "name";
                string keyWordTranslation = "tr";
                _ = idWordPairs.TryAdd(keyWordId, keyWordTranslation);
            }
        }
        LangDictionaryCache.AddOrUpdate(langCode, idWordPairs);
        return idWordPairs;
    }

    private static readonly ConditionalWeakTable<string, string> TranslationsCache = new ConditionalWeakTable<string, string>();

    private async Task<string> GetLang(string authorization, string langId)
    {
        if (TranslationsCache.TryGetValue(langId, out string? cached))
        {
            return cached;
        }
        else
        {
            string result = await Task.FromResult("lang");
            TranslationsCache.AddOrUpdate(langId, result);
            return result;
        }
    }
}
public static class WeakTableCache<T>
    where T : class, new()
{
    private static readonly ConditionalWeakTable<string, T> LangDictionaryCache
        = new ConditionalWeakTable<string, T>();

    public static T GetOrAdd(string key, Func<string, T> valueFactory)
        => LangDictionaryCache.GetValue(key, k => valueFactory(k));
}
