using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CommonElementsInclude
{
    internal class Program
    {
        private static Dictionary<string, string> KeyToTemplate = new Dictionary<string, string>();
        private const string RENDER_BODY = "<RenderBody/>";

        private static void Main(string[] args)
        {
            Console.WriteLine("WSOFT CommonElementsInclude Version 0.8");
            Console.WriteLine("Copyright (c) WSOFT All rights reserved.");

            if (args.Length < 1)
                return;

            string layout;
            Console.Write("レイアウトファイルを取得しています...");
            if (args[0].Contains("://"))
            {
                var wc = new WebClient();
                layout = wc.DownloadString(args[0]);
            }
            else
            {
                layout = File.ReadAllText(args[0]);
            }

            args = args[1..];
            
            try
            {
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
                            {
                                KeyToTemplate[key] = context;
                            }
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
            catch (Exception ex)
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
            args = new string[] { "K:\\LocalFiles\\Desktop\\wsoft.ws" };
            foreach (string arg in args)
            {
                if (arg == "--min")
                {
                    continue;
                }
                string dir = Path.GetFullPath(arg);
                Console.WriteLine(dir + "を検索しています...");
                if (Directory.Exists(dir))
                {
                    ReplaceDirectory(dir, "site", minify);
                }
            }
            Console.WriteLine("完了しました。");
        }

        private static void ReplaceDirectory(string dir, string target, bool minify)
        {
            FormatDirectory(Path.Combine(dir, target));
            foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (!file.Contains(".git"))
                {
                    string to = Path.Combine(dir, target, file.Substring(dir.Length + 1));
                    CreateDirIfNotExists(Path.GetDirectoryName(to));
                    if (file.ToLower().EndsWith(".html") || file.ToLower().EndsWith(".js"))
                    {
                        string raw = File.ReadAllText(file);
                        string new_str = raw;
                        bool replace = false;
                        if (!Regex.IsMatch(raw, "<[ ]*common[ ]*role[ ]*=[ ]*\".*\"[ ]*/[ ]*>"))
                        {
                            foreach (Match m in Regex.Matches(raw, "<common\\b[^>]*?\\bclass\\s*=\\s*[\"']([^\"']*)[\"'][^>]*>(.*?)<\\/common>|<common\\b[^>]*?\\bclass\\s*=\\s*[\"']([^\"']*)[\"'][^>]*\\/?>"))
                            {
                                if (m.Groups.Count > 3)
                                {
                                    /// タグ内のコンテクスト
                                    string context = m.Groups[2].Value;
                                    /// タグのclass属性
                                    string key = m.Groups[3].Value;
                                    if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(m.Groups[1].Value))
                                    {
                                        key = m.Groups[1].Value;
                                    }

                                    if (!string.IsNullOrEmpty(key) && KeyToTemplate.ContainsKey(key))
                                    {
                                        string template = KeyToTemplate[key];

                                        /// テンプレート変数を埋めていく
                                        if (context.Contains('<'))
                                        {
                                            try
                                            {
                                                XElement xml = XElement.Parse(context);

                                                foreach (XElement vars in xml.Elements())
                                                {
                                                    template=template.Replace("@"+vars.Name.ToString(),vars.Value);
                                                }
                                            }
                                            catch { }
                                        }

                                        /// テンプレートで置換
                                        new_str = new_str.Replace(m.Value,new_str);

                                        replace = true;
                                    }

                                }
                            }
                        }
                        if (minify)
                        {
                            new_str = new_str.Replace("\r", "").Replace("\n", "");
                        }
                        if (replace || minify)
                        {
                            File.WriteAllText(to, new_str);
                            Console.WriteLine(file + "に対して置き換えを行いました");
                        }
                        else
                        {
                            File.Copy(file, to, true);
                        }
                    }
                    else
                    {
                        File.Copy(file, to, true);
                    }
                }

            }
        }

        private static void CreateDirIfNotExists(string dir)
        {
            string top = Path.GetDirectoryName(Path.GetFullPath(dir));
            if (!Directory.Exists(dir))
            {
                CreateDirIfNotExists(top);
                Directory.CreateDirectory(dir);
            }

        }

        private static void FormatDirectory(string dir)
        {
            CreateDirIfNotExists(dir);
            foreach (string ds in Directory.GetDirectories(dir))
            {
                FormatDirectory(ds);
            }
            foreach (string fi in Directory.GetFiles(dir, "*"))
            {
                File.Delete(fi);
            }
            Directory.Delete(dir);
        }
    }
}