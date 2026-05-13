using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GB2JIS
{
    internal static class CN2JA
    {

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
            }
            catch (Exception ex)
            {
                //Process.GetCurrentProcess().MainWindowHandle;
                MessageBox.Show(Application.Current.MainWindow, ex.Message);
            }
            #endregion
        }

        #region Convert Chinese to Japanese Kanji
        static private Encoding? GB2312; 
        static private Encoding? JIS;
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
            }
        }

        static public char ConvertChinese2Japanese(this char character)
        {
            var result = character;

            InitGBJISTable();
            var idx = GB2312_List.IndexOf(result);
            if (idx >= 0) result = JIS_List[idx];

            return (result);
        }

        static public string ConvertChinese2Japanese(this string line)
        {
            var result = line;

            result = string.Join("", line.ToCharArray().Select(c => ConvertChinese2Japanese(c)));
            //result = new string(line.ToCharArray().Select(c => ConvertChinese2Japanese(c)).ToArray());

            return (result);
        }

        static public IList<string> ConvertChinese2Japanese(this IEnumerable<string> lines)
        {
            var result = new List<string>();

            result.AddRange(lines.Select(l => ConvertChinese2Japanese(l)).ToList());

            return (result);
        }

        static public char ConvertJapanese2Chinese(this char character)
        {
            var result = character;

            InitGBJISTable();
            var idx = JIS_List.IndexOf(result);
            if (idx >= 0) result = GB2312_List[idx];

            return (result);
        }

        static public string ConvertJapanese2Chinese(this string line)
        {
            var result = line;

            result = string.Join("", line.ToCharArray().Select(c => ConvertJapanese2Chinese(c)));
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
    }
}
