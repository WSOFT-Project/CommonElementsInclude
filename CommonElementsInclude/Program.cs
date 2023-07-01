using System.IO;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommonElementsInclude
{
    internal class Program
    {
        static Dictionary<string,string> KeyToTemplate= new Dictionary<string,string>();
        const string RENDER_BODY = "<RenderBody/>";
        static void Main(string[] args)
        {
            Console.WriteLine("WSOFT CommonElementsInclude Version 0.6");
            Console.WriteLine("Copyright (c) WSOFT All rights reserved.");
            Console.Write("レイアウトファイルを取得しています...");
            var wc = new WebClient();
            try
            {
                string layout = wc.DownloadString("https://wsoft.ws/common/layout.html");
                Console.WriteLine("完了。");
                Console.Write("ファイルを解析中...");
                if (Regex.IsMatch(layout, "<[ ]*common[ ]*role[ ]*=[ ]*\"layout\"[ ]*/[ ]*>"))
                {
                    foreach (Match m in Regex.Matches(layout, "<(common)\\s+(?:[^>]*?\\bclass\\s*=\\s*[\"']([^\"']*)[\"'][^>]*?|[^>]*?\\bclass\\s*=\\s*([^\"'\\s>]+)[^>]*?)\\s*\\/?>([\\s\\S]*?)<\\/\\1\\s*>"))
                    {
                        if (m.Groups.Count > 4)
                        {
                            string context = m.Groups[4].Value;
                            string key = m.Groups[2].Value;

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(context))
                                KeyToTemplate[key] = context;
                        }
                    }
                    Console.WriteLine("完了。");
                }
                else
                {
                    Console.WriteLine("失敗。");
                    Console.WriteLine("レイアウトファイルは適切に構成されたものではありません。");
                    Console.WriteLine("レイアウトファイルの読込中にエラーが発生しました。終了します。");
                    return;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("失敗。");
                Console.WriteLine(ex.Message);
                Console.WriteLine("レイアウトファイルの読込中にエラーが発生しました。終了します。");
                return;
            }

            /*
            string HEADER = wc.DownloadString("https://wsoft.ws/common/header.html").Replace("\r","").Replace("\n","");
            string FOOTER = wc.DownloadString("https://wsoft.ws/common/footer.html").Replace("\r", "").Replace("\n", "");
            string MODAL = wc.DownloadString("https://wsoft.ws/common/modal.html").Replace("\r", "").Replace("\n", "");
            */

            bool minify = false;
            foreach (string arg in args)
            {
                if (arg == "--min")
                {
                    continue;
                }
                string dir = Path.GetFullPath(arg);
                Console.WriteLine(dir+"を検索しています...");
                if (Directory.Exists(dir))
                {
                    ReplaceDirectory(dir,"*.html",minify);
                    ReplaceDirectory(dir, "*.js", minify);
                }
            }
            Console.WriteLine("完了しました。");
        }
        static void ReplaceDirectory(string dir,string pattern,bool minify)
        {
            foreach (string file in Directory.GetFiles(dir,pattern, SearchOption.AllDirectories))
            {
                string raw = File.ReadAllText(file);
                string new_str = raw;
                bool replace = false;
                if(!Regex.IsMatch(raw, "<[ ]*common[ ]*role[ ]*=[ ]*\"layout\"[ ]*/[ ]*>"))
                {
                    foreach (Match m in Regex.Matches(raw, "<common\\b[^>]*?\\bclass\\s*=\\s*[\"']([^\"']*)[\"'][^>]*>(.*?)<\\/common>|<common\\b[^>]*?\\bclass\\s*=\\s*[\"']([^\"']*)[\"'][^>]*\\/?>\r\n"))
                    {
                        if (m.Groups.Count > 3)
                        {
                            string context = m.Groups[2].Value;
                            string key = m.Groups[3].Value;
                            if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(m.Groups[1].Value))
                                key = m.Groups[1].Value;

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(context) && KeyToTemplate.ContainsKey(key))
                                new_str = new_str.Replace(m.Value, KeyToTemplate[key].Replace(RENDER_BODY, context));
                            replace = true;

                        }
                    }
                }
                if (minify)
                {
                    new_str = new_str.Replace("\r", "").Replace("\n", "");
                }
                File.WriteAllText(file, new_str);
                if (replace || minify)
                {
                    Console.WriteLine(file + "に対して置き換えを行いました");
                }
            }
        }
    }
}