using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParseJsonPointerTest
{
    public class Program
    {
        private static readonly Regex escapingRegex = new Regex("~(?<code>0|1)", RegexOptions.Compiled);

        static string[] samples = new string[]
        {
            "path/without/escaping",
            "path/with/tilde/~0in/the/middle",
            "path/with/escaped~1slash",
            "path/with/both/~0tidle/and/~1slash",
            "/leading/slash/path/with/both/~0tidle/and/~1slash"
        };

        public static void Main(string[] args)
        {
            foreach (var path in samples)
            {
                if (ParsePathWithCustomImplementation(path).Except(ParsePathWithSplitAndRegex(path)).Count() == 0)
                {
                    Console.WriteLine($"OK with {path}");
                }
                else
                {
                    Console.WriteLine($"KO with {path}");
                }
            }

            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"RUN {i+1}:");
                Measure(nameof(ParsePathWithSplitAndRegex), ParsePathWithSplitAndRegex);
                Measure(nameof(ParsePathWithSplitAndReplace), ParsePathWithSplitAndReplace);
                Measure(nameof(ParsePathWithCustomImplementation), ParsePathWithCustomImplementation);
            }

            Console.ReadLine();
        }

        public static void Measure(string name, Func<string, string[]> impl)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 15000; i++)
            {
                foreach (var path in samples)
                {
                    impl(path);
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"{name} :{stopwatch.ElapsedMilliseconds} ms.");
        }

        public static string[] ParsePathWithSplitAndRegex(string path)
        {

            return path
                .Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => escapingRegex.Replace(segment, (match) => match.Groups["code"].Value == "0" ? "~" : "/"))
                .ToArray();
        }

        public static string[] ParsePathWithSplitAndReplace(string path)
        {

            return path
                .Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Replace("~0", "~").Replace("~1", "/"))
                .ToArray();
        }

        public static string[] ParsePathWithCustomImplementation(string path)
        {
            List<string> strings = new List<string>();
            StringBuilder sb = new StringBuilder(path.Length);

            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    if (sb.Length > 0)
                    {
                        strings.Add(sb.ToString());
                        sb.Length = 0;
                    }
                }
                else if (path[i] == '~')
                {
                    ++i;
                    if (i >= path.Length)
                    {
                        throw new Exception("JSON pointer escaping sequence incomplete");
                    }

                    if (path[i] == '0')
                    {
                        sb.Append('~');
                    }
                    else if (path[i] == '1')
                    {
                        sb.Append('/');
                    }
                    else
                    {
                        throw new Exception("Invalid JSON pointer escaping sequence");
                    }
                }
                else
                {
                    sb.Append(path[i]);
                }
            }

            return strings.ToArray();
        }
    }
}
