using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable SYSLIB1045 // 转换为“GeneratedRegexAttribute”。

namespace GB2JIS
{
    internal static class CN2JA
    {
        private static readonly Encoding? GB2312;
        private static readonly Encoding? JIS;

        static private Dictionary<char, char> GB_JIS_EXTC = new()
        {
            {'濑', '瀬'},
            //{'面', '麺'},
        };

        static private Dictionary<string, string> GB_JIS_EXTS = new()
        {
            {"拉面", "拉麺"},
            {"面条", "麺条"},
        };

        static CN2JA()
        {
            #region Extented the supported string charsets
            //
            // Add GBK/Shift-JiS... to Encoding Supported
            // 使用CodePagesEncodingProvider去注册扩展编码。
            //
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                GB2312 = Encoding.GetEncoding("GB2312");
                JIS = Encoding.GetEncoding("SHIFT_JIS");

                InitUnihanTable();
                InitCustomDict();

                InitGBJISTable();
            }
            catch (Exception ex)
            {
                //Process.GetCurrentProcess().MainWindowHandle;
                MessageBox.Show(Application.Current.MainWindow, ex.Message);
            }
            finally
            {
                GC.Collect();
            }
            #endregion
        }

        static internal void InitCustomDict()
        {
            if (!System.IO.File.Exists("GB2JIS_CustomDict.txt")) return;

            GB_JIS_EXTS ??= [];
            var lines = System.IO.File.ReadAllLines("GB2JIS_CustomDict.txt");
            foreach (var line in lines)
            {
                if (line.StartsWith('#') || string.IsNullOrEmpty(line.Trim())) continue;
                try
                {
                    var kv = line.Split(["=>", "=", "\t", ">", ",", "|"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (kv.Length != 2) continue;
                    GB_JIS_EXTS[kv[0]] = kv[1];
                }
                catch { }
            }
        }

        #region Convert Chinese to Japanese Kanji
        static private List<char> GB2312_List { get; set; } = [];
        static private List<char> JIS_List { get; set; } = [];
        static internal void InitGBJISTable()
        {
            GB2312_List ??= [];
            JIS_List ??= [];
            GB_JIS_EXTC ??= [];

            if (JIS is null || GB2312 is null) return;

            if (JIS_List.Count == 0 || GB2312_List.Count == 0)
            {
                JIS_List.Clear();
                GB2312_List.Clear();

                var dat = Assembly.GetEntryAssembly()?.GetManifestResourceStream($"{AppDomain.CurrentDomain.FriendlyName}.GB2JIS.dat");
                if (dat is null) return;

                var jis_data = new byte[dat.Length];
                dat.Read(jis_data, 0, jis_data.Length);

                //var jis_data = Properties.Resources.GB2JIS;
                //var jis_count = jis_data.Length / 2;
                JIS_List = [.. JIS.GetString(jis_data)];
                GB2312_List = [.. GB2312.GetString(jis_data)];

                var jis = new byte[2];
                var gb2312 = new byte[2];
                for (var i = 0; i < 94; i++)
                {
                    gb2312[0] = (byte)(i + 0xA1);
                    for (var j = 0; j < 94; j++)
                    {
                        gb2312[1] = (byte)(j + 0xA1);
                        var offset = i * 94 + j;
                        GB2312_List[offset] = GB2312.GetString(gb2312).First();

                        jis[0] = jis_data[2 * offset];
                        jis[1] = jis_data[2 * offset + 1];
                        JIS_List[i * 94 + j] = JIS.GetString(jis).First();
                    }
                }

                foreach (var kv in GB_JIS_EXTC)
                {
                    GB2312_List = [.. GB2312_List, kv.Key];
                    JIS_List = [.. JIS_List, kv.Value];
                }
            }
        }

        static private bool InGB2312(char character)
        {
            GB2312_List ??= [];
            return (GB2312_List.LastIndexOf(character) >= 0);
        }

        static public char ConvertChinese2Japanese(this char character)
        {
            var result = character;

            InitGBJISTable();
            var idx = GB2312_List.LastIndexOf(result);
            if (idx >= 0) result = JIS_List[idx];

            return (result);
        }

        static public string ConvertChinese2Japanese(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Key, kv.Value));
            result = result.Select(ConvertChinese2Japanese).ToString();

            return (result ?? line);
        }

        static public IList<string> ConvertChinese2Japanese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(ConvertChinese2Japanese)]);

            return (result);
        }

        static private bool InJIS(char character)
        {
            JIS_List ??= [];
            return (JIS_List.LastIndexOf(character) >= 0);
        }

        static public char ConvertJapanese2Chinese(this char character)
        {
            var result = character;

            InitGBJISTable();
            var idx = JIS_List.LastIndexOf(result);
            if (idx >= 0) result = GB2312_List[idx];

            return (result);
        }

        static public string ConvertJapanese2Chinese(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Value, kv.Key));
            result = result.Select(ConvertJapanese2Chinese).ToString();

            return (result ?? line);
        }

        static public IList<string> ConvertJapanese2Chinese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(ConvertJapanese2Chinese)]);

            return (result);
        }
        #endregion

        #region Unicode Unihan routines
        static private Dictionary<string, string[]> Unihan_SC_Dict { get; set; } = [];
        static private Dictionary<string, string[]> Unihan_TC_Dict { get; set; } = [];
        static private Dictionary<string, string[]> Unihan_JA_Dict { get; set; } = [];

        static internal void InitUnihanTable()
        {
            Unihan_SC_Dict ??= [];
            Unihan_TC_Dict ??= [];
            Unihan_JA_Dict ??= [];

            if (!System.IO.File.Exists("Unihan_Variants.txt")) return;

            if (Unihan_SC_Dict?.Count == 0 || Unihan_TC_Dict?.Count == 0 || Unihan_JA_Dict?.Count == 0)
            {
                Unihan_SC_Dict?.Clear();
                Unihan_TC_Dict?.Clear();
                Unihan_JA_Dict?.Clear();

                var pat = new Regex(@"^U\+([0-9A-F]{4,5})\t(.+?)\t(U\+([0-9A-F]{4,5})\s?)+.*$", RegexOptions.IgnoreCase);
                var unihan = System.IO.File.ReadAllLines("Unihan_Variants.txt");
                foreach (var line in unihan)
                {
                    if (line.StartsWith('#') || string.IsNullOrEmpty(line)) continue;
                    try
                    {
                        if (pat.Match(line) is not { Success: true } m) continue;
                        var uni_value = char.ConvertFromUtf32(int.Parse(m.Groups[1].Value.Trim(), System.Globalization.NumberStyles.HexNumber));
                        var uni_type = m.Groups[2].Value.Trim();
                        var uni_variant = m.Groups[3].Value.Trim().Split().Select(u => char.ConvertFromUtf32(int.Parse(u.Trim()[2..], System.Globalization.NumberStyles.HexNumber))).ToArray();

                        if (uni_type.Equals("kSimplifiedVariant")) Unihan_SC_Dict?[uni_value] = uni_variant;
                        if (uni_type.Equals("kTraditionalVariant")) Unihan_TC_Dict?[uni_value] = uni_variant;
                        if (uni_type.Equals("kJapaneseVariant")) Unihan_JA_Dict?[uni_value] = uni_variant;
                    }
                    catch { }
                }
            }
        }

        static public bool UnihanValid => Unihan_SC_Dict?.Count > 0 && Unihan_TC_Dict?.Count > 0 && Unihan_JA_Dict?.Count >= 0;

        static public string UnihanSC2TC(this char character)
        {
            var result = character.ToString();
            Unihan_TC_Dict ??= [];
            if (Unihan_TC_Dict?.ContainsKey(result) ?? false)
            {
                var variants = Unihan_TC_Dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            return (result);
        }

        static public string UnihanSC2TC(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Key, kv.Value));
            result = string.Join("", result.ToCharArray().Select(c => UnihanSC2TC(c)));

            return (result);
        }

        static public IList<string> UnihanSC2TC(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(UnihanSC2TC)]);

            return (result);
        }

        static public string UnihanTC2SC(this char character)
        {
            var result = character.ToString();
            Unihan_SC_Dict ??= [];
            if (Unihan_SC_Dict?.ContainsKey(result) ?? false)
            {
                var variants = Unihan_SC_Dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            return (result);
        }

        static public string UnihanTC2SC(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Value, kv.Key));
            result = string.Join("", result.ToCharArray().Select(c => UnihanTC2SC(c)));

            return (result);
        }

        static public IList<string> UnihanTC2SC(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(UnihanTC2SC)]);

            return (result);
        }

        static public string UnihanChinese2Japanese(this char character)
        {
            var result = character.ToString();
            Unihan_JA_Dict ??= [];
            Unihan_TC_Dict ??= [];

            if (Unihan_JA_Dict?.ContainsKey(result) ?? false)
            {
                var variants = Unihan_JA_Dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            else if (InGB2312(character)) 
                result = ConvertChinese2Japanese(character).ToString();
            else if (Unihan_TC_Dict?.ContainsKey(result) ?? false)
            {
                var variants = Unihan_TC_Dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            else result = ConvertChinese2Japanese(character).ToString();
            return (result);
        }

        static public string UnihanChinese2Japanese(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Key, kv.Value));
            result = string.Join("", result.ToCharArray().Select(c => UnihanChinese2Japanese(c)));

            return (result);
        }

        static public IList<string> UnihanChinese2Japanese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(UnihanChinese2Japanese)]);

            return (result);
        }

        static public string UnihanJapanese2Chinese(this char character)
        {
            var result = character.ToString();
            Unihan_SC_Dict ??= [];

            if (Unihan_SC_Dict?.ContainsKey(result) ?? false)
            {
                var variants = Unihan_SC_Dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            else result = ConvertJapanese2Chinese(character).ToString();
            return (result);
        }

        static public string UnihanJapanese2Chinese(this string line)
        {
            var result = line;

            result = GB_JIS_EXTS.Aggregate(result, (current, kv) => current.Replace(kv.Value, kv.Key));
            result = string.Join("", result.ToCharArray().Select(c => UnihanJapanese2Chinese(c)));

            return (result);
        }

        static public IList<string> UnihanJapanese2Chinese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange([.. lines.Select(UnihanJapanese2Chinese)]);

            return (result);
        }

        #endregion
    }
}
