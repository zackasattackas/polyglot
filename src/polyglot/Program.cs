using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace polyglot
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                ParseCommandLine(args);

                if (ShowHelp)
                {
                    Console.Error.WriteLine(HelpText);
                    return;
                }

                Console.OutputEncoding = Encoding.Unicode;
                Console.WriteLine(await TranslateText(Text, SourceLanguage, TargetLanguage));
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ResetColor();
            }

            Console.ReadKey();
        }

        private static bool ShowHelp { get; set; }
        private static string SourceLanguage { get; set; }
        private static string TargetLanguage { get; set; }
        private static string Text { get; set; }

        private static void ParseCommandLine(IReadOnlyList<string> args)
        {
            var matches = Regex.Matches(args[0], "(?'showHelp'(-[?]|--help))|((?'source'\\w{0,2})(?==)|(?<==)(?'target'\\w{2}))");
            if (matches.Count == 0)
                throw new ArgumentException("Unexpected value: " + args[0]);

            var groups = matches.SelectMany(m => m.Groups).ToList();

            ShowHelp = groups.SingleOrDefault(m => m.Name == "showHelp") != null;
            if (ShowHelp)
                return;

            SourceLanguage = groups.SingleOrDefault(m => m.Name == "source")?.Value ?? CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            TargetLanguage = groups.Single(m => m.Name == "target").Value;
            Text = args.Count == 1 ? Console.In.ReadLine() : string.Join(" ", args.Skip(1));
        }

        private static async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {
            return await new GoogleTranslate().TranslateTextAsync(text, sourceLang, targetLang);
        }

        private const string HelpText =
            @"
polyglot - CLI for Google Translate

Usage: polyglot [source]=[target] [text]

Arguments

    source      The source language to translate from
    target      The target language to translate to
    text        The text to translate

Options

    -?|--help   Display this help information

Examples

    #1 Translate to Italian using the current system language
    $ polyglot =it Hi, how are you?
    Ciao, come stai?

    #2 Translate from Italian to Russian using pipeline
    $ polyglot =it Hi, how are you? | polyglot it=rus
    ";
    }

    public class GoogleTranslate
    {
        private static Lazy<HttpClient> _httpClient;
        private const string UriFormat = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}";

        public GoogleTranslate()
        {
            _httpClient = new Lazy<HttpClient>();
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang)
        {
            return (await JToken.LoadAsync(new JsonTextReader(await TranslateAsync(text, sourceLang, targetLang))))[0][0][0].ToString();
        }

        public async Task<TextReader> TranslateAsync(string text, string sourceLang, string targetLang)
        {
            return new  StreamReader(await _httpClient.Value.GetStreamAsync(new Uri(string.Format(UriFormat, sourceLang, targetLang, text))));
        }
    }
}
