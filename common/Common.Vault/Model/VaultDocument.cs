using System;

namespace Common.Vault.Model
{
    public class VaultQueryDocument
    {
        public string Id { get; set; }
        public string Database { get; set; }
        public string AccountNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public int StartPage { get; set; }
        public string OutputFolder { get; set; }
        public int Resolution { get; set; } = 800; //512, 640, 800, 1024, 1280
        public int Orientation { get; set; } = 0; //0=0, 1=90, 2=180, 3=270
        public OutputFormat OutputFormat { get; set; } = OutputFormat.raw;

    }

    public enum OutputFormat
    {
        gif = 0,
		collect = 1,
        pdf = 2,
        raw = 3,
        png = 4,
        tiff = 5,
        text = 9
    }
}
