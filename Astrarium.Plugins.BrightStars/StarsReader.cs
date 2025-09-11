using Astrarium.Algorithms;
using Astrarium.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Astrarium.Plugins.BrightStars
{
    [Singleton(typeof(IStarsReader))]
    public class StarsReader : IStarsReader
    {
        /// <summary>
        /// Length of single record in BSC5 data file
        /// </summary>
        private const int BSC5_RECORD_LEN = 198;

        /// <summary>
        /// Length of single record in BSC4s data file
        /// </summary>
        private const int BSC4SUP_RECORD_LEN = 213;

        /// <summary>
        /// Stars count in BSC catalogue
        /// </summary>
        private const int BSC_STARS_COUNT = 9110;

        /// <summary>
        /// File path to the Bright Star Catalogue v5 file (<see href="https://cdsarc.cds.unistra.fr/ftp/cats/V/50/") />
        /// </summary>
        private readonly string BSC5_FILE = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data", "bsc5.dat");

        /// <summary>
        /// File path to the Bright Star Catalogue v4 supplementary file (<see href="https://cdsarc.cds.unistra.fr/ftp/cats/V/36B/") />
        /// </summary>
        private readonly string BSC4SUP_FILE = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data", "bsc4s.dat");

        /// <summary>
        /// File with greek alphabet letters abbreviations and full names
        /// </summary>
        private readonly string ALPHABET_FILE = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data", "Alphabet.dat");

        /// <summary>
        /// Sky instance
        /// </summary>
        private readonly ISky sky;

        public StarsReader(ISky sky)
        {
            this.sky = sky;
        }

        /// <summary>
        /// Reads stars data
        /// </summary>
        public ICollection<Star> ReadStars()
        {
            List<Star> stars = new List<Star>();
            stars.AddRange(ReadBSC5Stars());
            stars.AddRange(ReadBSC4SupStars());
            return stars;
        }

        private ICollection<Star> ReadBSC5Stars()
        {
            List<Star> stars = new List<Star>();

            string line = "";

            using (var sr = new StreamReader(BSC5_FILE, Encoding.Default))
            {
                while (line != null && !sr.EndOfStream)
                {
                    line = sr.ReadLine();

                    Star star = null;

                    if (line[94] != ' ')
                    {
                        star = new Star();
                        star.Number = ushort.Parse(line.Substring(0, 4).Trim());
                        star.Name = line.Substring(4, 10);

                        string hdNumber = line.Substring(25, 6).Trim();
                        if (!string.IsNullOrEmpty(hdNumber))
                        {
                            star.HDNumber = hdNumber;
                        }

                        string saoNumber = line.Substring(31, 6).Trim();
                        if (!string.IsNullOrEmpty(saoNumber))
                        {
                            star.SAONumber = uint.Parse(saoNumber);
                        }

                        string fk5Number = line.Substring(37, 4).Trim();
                        if (!string.IsNullOrEmpty(fk5Number))
                        {
                            star.FK5Number = ushort.Parse(fk5Number);
                        }

                        string varName = line.Substring(51, 9).Trim();
                        if (!string.IsNullOrEmpty(varName) &&
                            !varName.Equals("Var?") &&
                            !varName.Equals("Var") &&
                            !line.Substring(51, 3).Trim().Equals(star.Name.Substring(3, 3).Trim()))
                        {
                            star.VariableName = varName;
                        }

                        star.Alpha0 = (float)new HMS(
                                            Convert.ToUInt32(line.Substring(75, 2)),
                                            Convert.ToUInt32(line.Substring(77, 2)),
                                            Convert.ToDouble(line.Substring(79, 4), CultureInfo.InvariantCulture)
                                        ).ToDecimalAngle();

                        star.Delta0 = (line[83] == '-' ? -1 : 1) * (float)new DMS(
                                                    Convert.ToUInt32(line.Substring(84, 2)),
                                                    Convert.ToUInt32(line.Substring(86, 2)),
                                                    Convert.ToUInt32(line.Substring(88, 2))
                                                ).ToDecimalAngle();

                        if (line[148] != ' ')
                        {
                            star.PmAlpha = Convert.ToSingle(line.Substring(148, 6), CultureInfo.InvariantCulture);
                        }
                        if (line[154] != ' ')
                        {
                            star.PmDelta = Convert.ToSingle(line.Substring(154, 6), CultureInfo.InvariantCulture);
                        }

                        star.Magnitude = Convert.ToSingle(line.Substring(102, 5), CultureInfo.InvariantCulture);
                        star.Color = line[129];

                        string identifier = star.Names.FirstOrDefault(n => sky.StarNames.ContainsKey(n));
                        if (identifier != null)
                        {
                            star.ProperName = sky.StarNames[identifier];
                        }
                    }

                    stars.Add(star);
                }
            }

            return stars;
        }

        private ICollection<Star> ReadBSC4SupStars()
        {
            List<Star> stars = new List<Star>();

            string line = "";

            using (var sr = new StreamReader(BSC4SUP_FILE, Encoding.Default))
            {
                ushort count = 0;
                while (line != null && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    count++;

                    Star star = new Star();
                    star.Name = new string(' ', 10);
                    star.Number = (ushort)(BSC_STARS_COUNT + count);

                    string hdNumber = line.Substring(0, 8).Trim();
                    if (!string.IsNullOrEmpty(hdNumber))
                    {
                        star.HDNumber = hdNumber;
                    }

                    string saoNumber = line.Substring(19, 6).Trim();
                    if (!string.IsNullOrEmpty(saoNumber))
                    {
                        star.SAONumber = uint.Parse(saoNumber);
                    }

                    //string fk5Number = line.Substring(37, 4).Trim();
                    //if (!string.IsNullOrEmpty(fk5Number))
                    //{
                    //    star.FK5Number = ushort.Parse(fk5Number);
                    //}

                    //string varName = line.Substring(51, 9).Trim();
                    //if (!string.IsNullOrEmpty(varName) &&
                    //    !varName.Equals("Var?") &&
                    //    !varName.Equals("Var") &&
                    //    !line.Substring(51, 3).Trim().Equals(star.Name.Substring(3, 3).Trim()))
                    //{
                    //    star.VariableName = varName;
                    //}

                    star.Alpha0 = (float)new HMS(
                                        Convert.ToUInt32(line.Substring(69, 2)),
                                        Convert.ToUInt32(line.Substring(72, 2)),
                                        Convert.ToDouble(line.Substring(75, 4), CultureInfo.InvariantCulture)
                                    ).ToDecimalAngle();

                    star.Delta0 = (line[80] == '-' ? -1 : 1) * (float)new DMS(
                                                Convert.ToUInt32(line.Substring(81, 2)),
                                                Convert.ToUInt32(line.Substring(84, 2)),
                                                Convert.ToUInt32(line.Substring(87, 2))
                                            ).ToDecimalAngle();

                    if (line[148] != ' ')
                    {
                        star.PmAlpha = Convert.ToSingle(line.Substring(148, 6), CultureInfo.InvariantCulture);
                    }
                    if (line[155] != ' ')
                    {
                        star.PmDelta = Convert.ToSingle(line.Substring(155, 6), CultureInfo.InvariantCulture);
                    }

                    star.Magnitude = Convert.ToSingle(line.Substring(104, 4), CultureInfo.InvariantCulture);
                    star.Color = line[129];

                    string identifier = star.Names.FirstOrDefault(n => sky.StarNames.ContainsKey(n));
                    if (identifier != null)
                    {
                        star.ProperName = sky.StarNames[identifier];
                    }
                    

                    stars.Add(star);
                }
            }

            return stars;
        }

        public StarDetails GetStarDetails(ushort hrNumber)
        {
            if (hrNumber > 0 && hrNumber <= BSC_STARS_COUNT)
                return GetBSC5StarDetails(hrNumber);
            else if (hrNumber > BSC_STARS_COUNT)
                return GetBSC4SupStarDetails(hrNumber);
            else
                return null;
        }

        private StarDetails GetBSC5StarDetails(ushort hrNumber)
        {
            var details = new StarDetails();

            using (var sr = new StreamReader(BSC5_FILE, Encoding.Default))
            {
                //sr.BaseStream.Seek((hrNumber - 1) * BSC5_RECORD_LEN, SeekOrigin.Begin);
                int count = 0;
                string line;
                do
                {
                    line = sr.ReadLine();
                    count++;
                }
                while (count < hrNumber);

                details.IsInfraredSource = line[41] == 'I';
                details.SpectralClass = line.Substring(127, 20).Trim();
                details.Pecularity = line.Substring(147, 1).Trim();

                string radialVelocity = line.Substring(166, 4).Trim();

                details.RadialVelocity = string.IsNullOrEmpty(radialVelocity) ? (int?)null : int.Parse(radialVelocity);
            }

            return details;
        }

        private StarDetails GetBSC4SupStarDetails(ushort hrNumber)
        {
            var details = new StarDetails();

            using (var sr = new StreamReader(BSC4SUP_FILE, Encoding.Default))
            {
                int count = 0;
                string line;
                do
                {
                    line = sr.ReadLine();
                    count++;
                }
                while (count < hrNumber - BSC_STARS_COUNT);
                details.SpectralClass = line.Substring(127, 20).Trim();
            }

            return details;
        }

        public Dictionary<string, string> ReadAlphabet()
        {
            Dictionary<string, string> alphabet = new Dictionary<string, string>();
            using (var sr = new StreamReader(ALPHABET_FILE, Encoding.Default))
            {
                string line = "";
                while (line != null && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    string[] chunks = line.Split('=');
                    alphabet.Add(chunks[0].Trim(), chunks[1].Trim());
                }
            }
            return alphabet;
        }
    }

    public class StarDetails
    {
        public int? RadialVelocity { get; set; }
        public bool IsInfraredSource { get; set; }
        public string SpectralClass { get; set; }
        public string Pecularity { get; set; }
    }
}
