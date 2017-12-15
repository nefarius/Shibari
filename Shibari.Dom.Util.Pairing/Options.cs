using CommandLine;

namespace Shibari.Dom.Util.Pairing
{
    class Options
    {
        [Option]
        public bool List { get; set; }

        [Option]
        public string Pair { get; set; }

        [Option]
        public string To { get; set; }
    }
}
