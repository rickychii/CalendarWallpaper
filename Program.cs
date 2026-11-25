using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace ConsoleApp1
{
    internal class Program
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 action, UInt32 uParam, String vParam, UInt32 winIni);

        internal static readonly UInt32 SPI_SETDESKWALLPAPER = 0x14;
        internal static readonly UInt32 SPIF_UPDATEINIFILE = 0x01;
        internal static readonly UInt32 SPIF_SENDWININICHANGE = 0x02;

        static void Main(string[] args)
        {

            int width = 1920;
            int height = 1080;

            Console.Title = "Rickychii 13-04-2025";
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Clear();

            String newWallpaper = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\dailycalendar.jpg";

            Font font = new Font("Arial", 20, FontStyle.Bold);
            Font fontAppointments = new Font("Arial", 11, FontStyle.Regular);
            Brush brushPassato = Brushes.DarkGray;
            Brush brushOggi = new SolidBrush(Color.FromArgb(144,238,144));
            Brush brushFuturo = new SolidBrush(Color.FromArgb(0, 223, 255));
            Brush pallini = Brushes.DarkGray;
            Brush palliniCircle = Brushes.DarkBlue;
            Brush triangleOggi = Brushes.GreenYellow;
            Pen lineBg = new Pen(new SolidBrush(Color.FromArgb(0x23, 0x23, 0x23)));

            int posX = 1380;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {

                // Colore di sfondo
                g.Clear(Color.RoyalBlue);

                //LINEE BG:
                g.DrawLine(lineBg, posX, 0, posX, 1080);

                DateTime today = DateTime.Today;
                DateTime threeDaysAgo = today.AddDays(-3);

                //APPUNTAMENTI!
                Dictionary<String, String[]> appointments = appointmentsFileHandler(today);

                //PRIMA LINEA BG:
                int posY1 = 70;
                g.DrawLine(lineBg, posX, posY1, 1920, posY1);

                // Testo
                for (int i = 0; i < 7; ++i) {
                    
                    string text = threeDaysAgo.DayOfWeek.ToString();
                    String giorno = threeDaysAgo.Day < 10 ? "0" + threeDaysAgo.Day : Convert.ToString(threeDaysAgo.Day);
                    String mese = threeDaysAgo.Month < 10 ? "0" + threeDaysAgo.Month : Convert.ToString(threeDaysAgo.Month);
                    text += "  " + giorno + "/" + mese + "/" + threeDaysAgo.Year;

                    Brush b = null;
                    //GIORNO PASSATO
                    if (threeDaysAgo < today)
                    {
                        b = brushPassato;
                    }
                    //OGGI?
                    else if (threeDaysAgo == today) 
                    {
                        b = brushOggi;
                    }
                    //FUTURO?
                    else
                    {
                        b = brushFuturo;
                    }

                    PointF position = new PointF(1500, (100 * i) + 100);
                    g.DrawString(text, font, b, position);

                    //GIORNO PASSATO?
                    if (threeDaysAgo < today) {
                        g.FillEllipse(palliniCircle, 1443, (100 * i) + 107, 19, 19);
                        g.FillEllipse(pallini, 1445, (100 * i) + 109, 15, 15);
                    }

                    //GIORNO ATTUALE?
                    if (threeDaysAgo == today) {
                        // Definisci i punti del triangolo
                        Point[] points = new Point[]
                        {
                            new Point(1443, (100) * i + 108),  // Vertice in alto
                            new Point(1443, (100) * i + 128),  // Vertice in basso sinistra
                            new Point(1470, (100) * i + 118)  // Vertice in basso destra
                        };
                        // Riempi il triangolo con il pennello
                        g.FillPolygon(triangleOggi, points);
                    }

                    //APPOINTMENTS:
                    String key = getAppointmenKey(Convert.ToString(today.Year), mese, giorno);
                    String textOggi = "Impegni: ";
                    if (appointments != null && appointments.ContainsKey(key))
                    {
                        String[] impegniOggi = appointments[key];
                        String impegno = String.Join(", ", impegniOggi).Trim();
                        if (!String.IsNullOrEmpty(impegno))
                        {
                            textOggi += impegno;
                            if (textOggi.Length > 79) {
                                //MAX 2 LINES!
                                textOggi = textOggi.Substring(0, 79) + Environment.NewLine + textOggi.Substring(79).Trim();
                            }
                        }
                    }
                    PointF positionOggi = new PointF(1390, (100 * i) + 132);
                    g.DrawString(textOggi, fontAppointments, b, positionOggi);


                    //LINEE BG:
                    int posY = 100 * i + 170;
                    g.DrawLine(lineBg, posX,posY, 1920, posY);

                    threeDaysAgo = threeDaysAgo.AddDays(1);
                }

                // Salva il file JPG
                bitmap.Save(newWallpaper, ImageFormat.Jpeg);

                Console.WriteLine("Enjoy your daily wallpaper.");

                //IMPOSTA LO SFONDO DEL DESKTOP:
                ReloadSettings(newWallpaper);
            }
        }



        internal static void ReloadSettings(String newWallpaper)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, newWallpaper, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        internal static Dictionary<String, String[]> appointmentsFileHandler(DateTime today) {

            Dictionary<String, String[]> impegni = new Dictionary<String, String[]>();

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Mycalendar\\")) {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Mycalendar\\");
            }
            String mycalendar = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Mycalendar\\appointments.json";

            if (!File.Exists(mycalendar))
            {
                DateTime endYear = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);
                int daysUntilEndYear = (endYear - today).Days;

                for (int i = 0; i <= daysUntilEndYear; ++i)
                {
                    String giorno = "";
                    String mese = "";

                    giorno = today.Day < 10 ? "0" + today.Day : today.Day.ToString();
                    mese = today.Month < 10 ? "0" + today.Month : today.Month.ToString();

                    String key = getAppointmenKey(Convert.ToString(today.Year), mese, giorno);
                    impegni[key] = new string[] { };

                    today = today.AddDays(1);
                }

                String json = JsonConvert.SerializeObject(impegni, Formatting.Indented);

                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Mycalendar\\appointments.json", json);
            }
            else {
                String json = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Mycalendar\\appointments.json");
                try
                {
                    impegni = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(json);
                }
                catch {
                    impegni = null;
                }
            }

            return impegni;


        }


        internal static String getAppointmenKey(String anno, String mese, String giorno) {
            return $"{anno}-{mese}-{giorno}";
        }
    }
}