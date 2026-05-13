using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace GB2JIS
{
    internal static class CN2JA
    {
        static private Encoding? GB2312;
        static private Encoding? JIS;

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
            #endregion
        }

        static internal void InitCustomDict()
        {
            if (!System.IO.File.Exists("GB2JIS_CustomDict.txt")) return;
            var lines = System.IO.File.ReadAllLines("GB2JIS_CustomDict.txt");
            foreach (var line in lines)
            {
                if (line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;
                try
                {
                    var kv = line.Split(new[] { "=>", "=", "\t", ">", ",", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (kv.Length != 2) continue;
                    GB_JIS_EXTS[kv[0]] = kv[1];
                }
                catch { }
            }
        }

        #region Convert Chinese to Japanese Kanji
        static private List<char> GB2312_List { get; set; } = new List<char>();
        static private List<char> JIS_List { get; set; } = new List<char>();
        static internal void InitGBJISTable()
        {
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
                var jis_count = jis_data.Length / 2;
                JIS_List = JIS.GetString(jis_data).ToList();
                GB2312_List = GB2312.GetString(jis_data).ToList();

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
            result = string.Join("", result.ToCharArray().Select(c => ConvertChinese2Japanese(c)));
            //result = new string(result.ToCharArray().Select(c => ConvertChinese2Japanese(c)).ToArray());

            return (result);
        }

        static public IList<string> ConvertChinese2Japanese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange(lines.Select(l => ConvertChinese2Japanese(l)).ToList());

            return (result);
        }

        static private bool InJIS(char character)
        {
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
            result = string.Join("", result.ToCharArray().Select(c => ConvertJapanese2Chinese(c)));
            //result = new string(line.ToCharArray().Select(c => ConvertJapanese2Chinese(c)).ToArray());

            return (result);
        }

        static public IList<string> ConvertJapanese2Chinese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange(lines.Select(l => ConvertJapanese2Chinese(l)).ToList());

            return (result);
        }
        #endregion

        #region Unicode Unihan routines
        static private Dictionary<string, string[]> unihan_sc_dict { get; set; } = new();
        static private Dictionary<string, string[]> unihan_tc_dict { get; set; } = new();
        static private Dictionary<string, string[]> unihan_ja_dict { get; set; } = new();

        static internal void InitUnihanTable()
        {
            if (!System.IO.File.Exists("Unihan_Variants.txt")) return;

            if (unihan_sc_dict.Count == 0 || unihan_tc_dict.Count == 0 || unihan_ja_dict.Count == 0)
            {
                unihan_sc_dict.Clear();
                unihan_tc_dict.Clear();
                unihan_ja_dict.Clear();

                var unihan = System.IO.File.ReadAllLines("Unihan_Variants.txt");
                foreach (var line in unihan)
                {
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;
                    try
                    {
                        var m = Regex.Match(line, @"^U\+([0-9A-F]{4,5})\t(.+?)\t(U\+([0-9A-F]{4,5})\s?)+.*$");
                        if (!m.Success) continue;
                        var uni_value = char.ConvertFromUtf32(int.Parse(m.Groups[1].Value.ToString().Trim(), System.Globalization.NumberStyles.HexNumber));
                        var uni_type = m.Groups[2].Value.ToString().Trim();
                        var uni_variant = m.Groups[3].Value.ToString().Trim().Split().Select(u => char.ConvertFromUtf32(int.Parse(u.Trim().Substring(2), System.Globalization.NumberStyles.HexNumber))).ToArray();

                        if (uni_type.Equals("kSimplifiedVariant")) unihan_sc_dict[uni_value] = uni_variant;
                        if (uni_type.Equals("kTraditionalVariant")) unihan_tc_dict[uni_value] = uni_variant;
                        if (uni_type.Equals("kJapaneseVariant")) unihan_ja_dict[uni_value] = uni_variant;
                    }
                    catch { }
                }
            }
        }

        static public bool UnihanValid => unihan_sc_dict?.Count > 0 && unihan_tc_dict?.Count > 0 && unihan_ja_dict?.Count >= 0;

        static public string UnihanSC2TC(this char character)
        {
            var result = character.ToString();
            if (unihan_tc_dict?.ContainsKey(result) ?? false)
            {
                var variants = unihan_tc_dict[result];
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

            result.AddRange(lines.Select(l => UnihanSC2TC(l)).ToList());

            return (result);
        }

        static public string UnihanTC2SC(this char character)
        {
            var result = character.ToString();
            if (unihan_sc_dict?.ContainsKey(result) ?? false)
            {
                var variants = unihan_sc_dict[result];
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

            result.AddRange(lines.Select(l => UnihanTC2SC(l)).ToList());

            return (result);
        }

        static public string UnihanChinese2Japanese(this char character)
        {
            var result = character.ToString();
            if (unihan_ja_dict?.ContainsKey(result) ?? false)
            {
                var variants = unihan_ja_dict[result];
                if (variants.Length > 0) result = string.Join("/", variants);
            }
            else if (InGB2312(character)) 
                result = ConvertChinese2Japanese(character).ToString();
            else if (unihan_tc_dict?.ContainsKey(result) ?? false)
            {
                var variants = unihan_tc_dict[result];
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

            result.AddRange(lines.Select(l => UnihanChinese2Japanese(l)).ToList());

            return (result);
        }

        static public string UnihanJapanese2Chinese(this char character)
        {
            var result = character.ToString();
            if (unihan_sc_dict?.ContainsKey(result) ?? false)
            {
                var variants = unihan_sc_dict[result];
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

            result.AddRange(lines.Select(l => UnihanJapanese2Chinese(l)).ToList());

            return (result);
        }

        #endregion
    }
}
