using CommandLine;

namespace Administrator.Common
{
    public abstract class MassPunishment
    {
        [Option('i', "interactive", Required = false, HelpText = "masspunishment_interactive", SetName = "verbose")]
        public bool IsInteractive { get; set; }

        [Option("regex", Required = false, HelpText = "masspunishment_regex", SetName = "targets")]
        public string RegexString { get; set; }

        [Option('t', "targets", Required = false, HelpText = "masspunishment_targets", SetName = "targets")]
        public ulong[] Targets { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "masspunishment_verbose", SetName = "verbose")]
        public bool IsVerbose { get; set; }

        [Option('r', "reason", Required = false, HelpText = "masspunishment_reason")]
        public string Reason { get; set; }

        [Option('c', "cases", Required = false, HelpText = "masspunishment_cases")]
        public bool CreateCases { get; set; }

        [Option('h', "help", Required = false, HelpText = "masspunishment_help")]
        public bool GetHelp { get; set; }

        [Option('p', "preview", Required = false, HelpText = "masspunishment_preview", SetName = "verbose")]
        public bool PreviewPunishments { get; set; }
    }
}