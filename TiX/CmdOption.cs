using System.Collections.Generic;
using CommandLine;

namespace TiX
{
    internal class CmdOption
    {
        public static readonly CmdOption Default = new CmdOption();

        public static CmdOption Parse(string[] args)
        {
            return (Parser.Default.ParseArguments<CmdOption>(args) as Parsed<CmdOption>)?.Value ?? Default;
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
