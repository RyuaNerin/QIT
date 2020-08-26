using System.Collections.Generic;
using CommandLine;

namespace TiX
{
    internal class Args
    {
        public static readonly Args Default = new Args();

        public static Args Parse(string[] args)
        {
            return (Parser.Default.ParseArguments<Args>(args) as Parsed<Args>)?.Value ?? Default;
        }

        private Args()
        {
        }

        [Option("install")] public int    OptionInstallation    { get; set; }
        [Option("statis" )] public bool   CaptureScreenPart     { get; set; }
        [Option("pipe"   )] public bool   UsePipe               { get; set; }
        [Option("notext" )] public bool   TweetWithoutText      { get; set; }
        [Option("text"   )] public string Text                  { get; set; }
        [Option("reply"  )] public string In_Reply_To_Status_Id { get; set; }
        [Option("sd"     )] public string SchemeData            { get; set; }

        [Option]
        public List<string> Files { get; set; }
    }
}
