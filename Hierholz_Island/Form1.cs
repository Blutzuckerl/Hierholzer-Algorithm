using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

// (right-click References > Add Reference > Assemblies > Framework > Microsoft.VisualBasic).

namespace Hierholz_Island
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            panel1.MouseDown += Panel1_MouseDown;
            panel1.MouseMove += Panel1_MouseMove;
            panel1.MouseUp += Panel1_MouseUp;



            // Double Buffering
            panel1.GetType().GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance).SetValue(panel1, true, null);

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 450;
            animationTimer.Tick += AnimationTimer_Tick;

        }
        Color Kreiscolor = Color.Red;
        //private List<Point> Inseln = new List<Point>(); // Liste der Kreismittelpunkt
        private List<Insel> Inseln = new List<Insel>();
        private List<Bruecke> Bruecken = new List<Bruecke>();
        private List<List<int>> adjazenzliste = new List<List<int>>();
        private const int kreisRadius = 20;
        private const int MindestAbstand = 60;
        //private Boolean bruecken_inseln = true;
        int nextAutoIndex = 0;
        char lastName = 'A';
        int startNr = -1;//
        Boolean bagger = false;
        Boolean AutoColorPicker = false;
        Point startPoint = new Point(0, 0);
        private int draggedIslandIndex = -1;
        private Point dragStartPosition;
        private static readonly Random getrandom = new Random();

        private System.Windows.Forms.Timer animationTimer; // Neu für die Animation
        private List<int> eulerWeg = null; // Speichert den Eulerkreis
        private List<Teilkreis> teilkreise = new List<Teilkreis>();
        private readonly Color[] teilkreisPalette = new Color[]
        {
            Color.FromArgb(0, 121, 191),   // Blau
            Color.FromArgb(237, 28, 36),   // Rot
            Color.FromArgb(57, 181, 74),   // Gruen
            Color.FromArgb(241, 90, 34),   // Orange
            Color.FromArgb(146, 39, 143),  // Magenta
            Color.FromArgb(0, 166, 81),    // Dunkelgruen
            Color.FromArgb(255, 127, 39),  // Hellorange
            Color.FromArgb(63, 72, 204)    // Indigo
        };
        private readonly string[] teilkreisFarbNamen = new string[]
        {
            "blau", "rot", "gruen", "orange", "magenta", "dunkelgruen", "hellorange", "indigo"
        };
        private int currentPathIndex = 0; // Index für die Animation
        private bool brueckenFarbwechselAktiv = false;
        private int brueckenFarbwechselSchritt = 0;
        private const int BrueckenFarbwechselSchritte = 12;
        public static Color GetRandomColor()//random farben generieren
        {
            lock (getrandom)
            {
                // Generiert eine zufällige Zahl zwischen 0 (inklusive) und 256 (exklusive), also 0-255.
                int r = getrandom.Next(0, 256);
                int g = getrandom.Next(0, 256);
                int b = getrandom.Next(0, 256);

                return Color.FromArgb(255, r, g, b);
            }
        }
        private void button3_Click(object sender, EventArgs e)//
        {
            // Dateidialog konfigurieren
            OpenFile.Filter = "Alle Textdateien (*.txt;*.xml;*.csv)|*.txt;*.xml;*.csv|*.txt;*.csv|Textdateien (*.txt;*.csv)|XML Dateien (*.xml)|*.xml";
            OpenFile.Title = "Karte laden";
            OpenFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

            if (OpenFile.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(OpenFile.FileName)) return;

            // Dateiendung extrahieren und entsprechend laden
            try
            {

                string ext = Path.GetExtension(OpenFile.FileName).ToLowerInvariant();

                switch (ext)
                {
                    case ".xml":
                        LoadGraphFromXml(OpenFile.FileName);// XML-Format laden
                        break;
                    case ".txt":
                    case ".csv":
                        LoadGraphFromText(OpenFile.FileName);// Einfaches Textformat laden
                        break;
                    default:
                        MessageBox.Show("Dateiformat wird nicht unterstützt!", "Fehler",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                }

                panel1.Invalidate();
                MessageBox.Show("Karte erfolgreich geladen!", "Info",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden: " + ex.Message, "Fehler",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D3)            // Taste 3 -> alle Inseln löschen
            {
                AutoColorPicker = !AutoColorPicker;// Automatischer Farbmodus an/aus
            }
        }
        void BrueckeZeichen(Graphics g)// Alle Brücken zeichnen
        {
            using (var edgeFont = new Font("Arial", 12, FontStyle.Bold))
            foreach (var br in Bruecken)
            {
                if (br.InselA < 0 || br.InselA >= Inseln.Count || br.InselB < 0 || br.InselB >= Inseln.Count) continue;
                Point a = Inseln[br.InselA].Position;
                Point b = Inseln[br.InselB].Position;

                using (var pen = new Pen(br.Farbe, 4))
                {
                    g.DrawLine(pen, a, b);
                }

                // Brückennamen an die Kante schreiben.
                float midX = (a.X + b.X) / 2f;
                float midY = (a.Y + b.Y) / 2f;
                float vx = b.X - a.X;
                float vy = b.Y - a.Y;
                float laenge = (float)Math.Sqrt(vx * vx + vy * vy);
                float offX = 0f;
                float offY = -10f;
                if (laenge > 0.01f)
                {
                    offX = -vy / laenge * 10f;
                    offY = vx / laenge * 10f;
                }

                string label = br.Name.ToString();
                SizeF textSize = g.MeasureString(label, edgeFont);
                var textPos = new PointF(midX + offX - textSize.Width / 2f, midY + offY - textSize.Height / 2f);
                var bgRect = new RectangleF(textPos.X - 2f, textPos.Y - 1f, textSize.Width + 4f, textSize.Height + 2f);

                using (var bgBrush = new SolidBrush(Color.FromArgb(210, Color.White)))
                using (var textBrush = new SolidBrush(Color.Black))
                {
                    g.FillRectangle(bgBrush, bgRect);
                    g.DrawString(label, edgeFont, textBrush, textPos);
                }
            }
            // Zeichne den Eulerkreis-Pfad wenn vorhanden
            if (eulerWeg != null && eulerWeg.Count > 1)
            {
                VisualisierePfad(g);
            }
        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            // Kanten zuerst zeichnen
            BrueckeZeichen(e.Graphics);
            // Alle gespeicherten Kreise zeichnen
            foreach (var insel in Inseln)
            {
                // Prüfe ob das die ausgewählte Insel ist
                int inselIndex = Inseln.IndexOf(insel);
                bool isSelected = (inselIndex == startNr);
                if (isSelected)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.Yellow),
                        insel.Position.X - kreisRadius - 5,
                        insel.Position.Y - kreisRadius - 5,
                        (kreisRadius + 5) * 2,
                        (kreisRadius + 5) * 2);
                }
                // Kreis mittig zum Klickpunkt zeichnen
                e.Graphics.FillEllipse(new SolidBrush(insel.Farbe),
                    insel.Position.X - kreisRadius,
                    insel.Position.Y - kreisRadius,
                    kreisRadius * 2,
                    kreisRadius * 2);

                // Optional: Einen schwarzen Rand zeichnen
                e.Graphics.DrawEllipse(Pens.Black,
                    insel.Position.X - kreisRadius,
                    insel.Position.Y - kreisRadius,
                    kreisRadius * 2,
                    kreisRadius * 2);
                // nach dem Zeichnen des Kreises
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                {
                    Color textColor = GetContrastingColor(insel.Farbe);
                    using (var brush = new SolidBrush(textColor))
                    {
                        var textPos = new Point(insel.Position.X - 6, insel.Position.Y - 8);
                        e.Graphics.DrawString(insel.Name.ToString(), font, brush, textPos);
                    }

                }

            }


        }
        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            draggedIslandIndex = -1;// Drag beenden
        }
        private void BrueckeBauen(int a, int b)
        {
            if (a == b) return;
            if (a < 0 || a >= adjazenzliste.Count || b < 0 || b >= adjazenzliste.Count) return;
            if (adjazenzliste[a].Contains(b)) return;

            adjazenzliste[a].Add(b);
            adjazenzliste[b].Add(a);

            char brName = (char)('a' + (Bruecken.Count % 26));
            var startFarbe = ErmittleBrueckenStartfarbe(a, b);
            Bruecken.Add(new Bruecke(a, b, startFarbe, brName)
            {
                StartFarbe = startFarbe,
                ZielFarbe = startFarbe
            });
            if (animationTimer != null) animationTimer.Stop();
            eulerWeg = null;
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;
        }

        private void BrueckeEinreißen(int a, int b)
        {
            if (a == b) return;
            if (a < 0 || a >= adjazenzliste.Count || b < 0 || b >= adjazenzliste.Count) return;
            if (adjazenzliste[a].Contains(b)) adjazenzliste[a].Remove(b);
            if (adjazenzliste[b].Contains(a)) adjazenzliste[b].Remove(a);
            RebuildBridgeListFromAdjacency();
            if (animationTimer != null) animationTimer.Stop();
            eulerWeg = null;
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;
        }

        private int GetInselIndexAt(Point p)// Gibt den Index der Insel zurück, die sich am Punkt p befindet (innerhalb des Kreises), oder -1 wenn keine Insel dort ist
        {
            for (int i = 0; i < Inseln.Count; i++)
            {
                double dx = Inseln[i].Position.X - p.X;
                double dy = Inseln[i].Position.Y - p.Y;
                if ((dx * dx + dy * dy) <= (kreisRadius * kreisRadius)) return i;
            }
            return -1;
        }

        private void HandleMouseInput(MouseEventArgs e)// Alle Mausklicks hier verarbeiten
        {
            // if (Colorpicker.ShowDialog() != DialogResult.Cancel && !string.IsNullOrWhiteSpace(Colorpicker.Color.ToString()))
            //  {
            int Abstand = 60;
            char nameChar = ' ';
            int inr = GetInselIndexAt(e.Location);// -1 = NULL
                                                  // Phytagoras für Abstand prüfen
            bool ueberschneidung = Inseln.Any(p =>
            {
                // Differenz
                double dx = p.Position.X - e.X;
                double dy = p.Position.Y - e.Y;
                return (dx * dx + dy * dy) < (Abstand * Abstand);// Pythagoras 
            });

            //Finde die erste Insel, die getroffen wurde (innerhalb des KreisRadius)
            var InselGetroffen = Inseln.FirstOrDefault(i =>
            {
                double dx = i.Position.X - e.X;
                double dy = i.Position.Y - e.Y;
                return (dx * dx + dy * dy) <= (kreisRadius * kreisRadius);
            });


            if (inr != -1)// Insel getroffen
            {
                if (e.Button == MouseButtons.Middle)// Mittelklick auf Insel
                {
                    if (!bagger)
                    {
                        while (true)// Schleife, um bei Namenskonflikten erneut zu fragen
                        {
                            char newName = AskNameChar(Inseln[inr].Name); // aktueller Name als Default
                            if (newName == '\0') return; // Abbruch bei leerer Eingabe

                            // Normalisieren auf Großbuchstaben für Vergleich
                            char newNameUpper = char.ToUpperInvariant(newName);

                            // Prüfen, ob der Name bereits von einer anderen Insel verwendet wird
                            bool nameBereitsVorhanden = Inseln.Any(i => i != InselGetroffen && char.ToUpperInvariant(i.Name) == newNameUpper);

                            if (nameBereitsVorhanden)
                            {
                                MessageBox.Show($"Der Name '{newName}' ist bereits vergeben. Bitte wähle einen anderen Buchstaben.", "Name bereits vergeben", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue; // nochmal fragen
                            }

                            // Name ist frei -> setzen und neu zeichnen
                            Inseln[inr].Name = newNameUpper;
                            panel1.Invalidate();
                            return;
                        }


                    }
                    // Rechtsklick auf Insel -> Name ändern

                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (!bagger)
                    {
                        if (startNr == -1)
                        {
                            startNr = inr;// Startpunkt für Brücke setzen

                        }
                        else
                        {
                            // zweiter Punkt -> Verbindung speichern

                            if (startNr != inr)// Verbindungsbruch vermeiden
                            {
                                BrueckeBauen(startNr, inr);
                            }
                            startNr = -1;
                            panel1.Invalidate();
                            // textBox2.Text = "Erfolgreich verbunden!";

                        }
                        return;
                    }
                    else
                    {

                        // Insel löschen
                        // 1. Insel aus Liste entfernen
                        Inseln.RemoveAt(inr);

                        // 2. Adjazenzliste anpassen:
                        // Entferne alle Verbindungen der Insel
                        adjazenzliste.RemoveAt(inr);

                        // 3. Indizes in den verbleibenden Listen anpassen
                        for (int i = 0; i < adjazenzliste.Count; i++)
                        {
                            adjazenzliste[i] = adjazenzliste[i].Where(index => index != inr)
                                                               .Select(index => index > inr ? index - 1 : index)
                                                               .ToList();
                        }
                        RebuildBridgeListFromAdjacency();
                        if (animationTimer != null) animationTimer.Stop();
                        eulerWeg = null;
                        teilkreise.Clear();
                        currentPathIndex = 0;
                        brueckenFarbwechselAktiv = false;
                        brueckenFarbwechselSchritt = 0;

                        // Panel neu zeichnen
                        panel1.Invalidate();

                        // Index zurücksetzen, damit keine weitere Aktion ausgeführt wird
                        return;
                    }

                }

                else if (e.Button == MouseButtons.Left && bagger)
                {
                    // Insel löschen
                    // 1. Insel aus Liste entfernen
                    Inseln.RemoveAt(inr);

                    // 2. Adjazenzliste anpassen:
                    // Entferne alle Verbindungen der Insel
                    adjazenzliste.RemoveAt(inr);

                    // 3. Indizes in den verbleibenden Listen anpassen
                    for (int i = 0; i < adjazenzliste.Count; i++)
                    {
                        adjazenzliste[i] = adjazenzliste[i].Where(index => index != inr)
                                                           .Select(index => index > inr ? index - 1 : index)
                                                           .ToList();
                    }
                    RebuildBridgeListFromAdjacency();
                    if (animationTimer != null) animationTimer.Stop();
                    eulerWeg = null;
                    teilkreise.Clear();
                    currentPathIndex = 0;
                    brueckenFarbwechselAktiv = false;
                    brueckenFarbwechselSchritt = 0;

                    // Panel neu zeichnen
                    panel1.Invalidate();

                    // Index zurücksetzen, damit keine weitere Aktion ausgeführt wird
                    return;
                    //return;
                }
                else if (e.Button == MouseButtons.Middle && bagger)
                {
                    // Insel löschen
                    // 1. Insel aus Liste entfernen
                    Inseln.RemoveAt(inr);

                    // 2. Adjazenzliste anpassen:
                    // Entferne alle Verbindungen der Insel
                    adjazenzliste.RemoveAt(inr);

                    // 3. Indizes in den verbleibenden Listen anpassen
                    for (int i = 0; i < adjazenzliste.Count; i++)
                    {// Alle Verbindungen zu der gelöschten Insel entfernen und Indizes anpassen
                        adjazenzliste[i] = adjazenzliste[i].Where(index => index != inr)
                                                           .Select(index => index > inr ? index - 1 : index)
                                                           .ToList();
                    }
                    RebuildBridgeListFromAdjacency();
                    if (animationTimer != null) animationTimer.Stop();
                    eulerWeg = null;
                    teilkreise.Clear();
                    currentPathIndex = 0;
                    brueckenFarbwechselAktiv = false;
                    brueckenFarbwechselSchritt = 0;

                    // Panel neu zeichnen
                    panel1.Invalidate();

                    // Index zurücksetzen, damit keine weitere Aktion ausgeführt wird
                    return;
                    //return;
                }


            }
            if (InselGetroffen != null)// Insel getroffen, aber kein spezieller Klick -> Drag starten
            {
                if (draggedIslandIndex != -1 && e.Button == MouseButtons.Left)
                {
                    Point newPos = new Point(
                        Inseln[draggedIslandIndex].Position.X + (e.X - dragStartPosition.X),
                        Inseln[draggedIslandIndex].Position.Y + (e.Y - dragStartPosition.Y)
                    );

                    Inseln[draggedIslandIndex].Position = newPos;
                    dragStartPosition = e.Location;
                    panel1.Invalidate();
                }

                //panel1.Cursor = GetInselIndexAt(e.Location) != -1 ? Cursors.Hand : Cursors.Default;
            }
            else if (ueberschneidung)
            {
                if (!bagger)
                {
                    MessageBox.Show("Zu nah an anderer Insel!");
                    return;

                }

            }
            else
            {
                if (e.Button == MouseButtons.Left)
                {
                    // Linksklick auf freie Fläche -> neue Insel erstellen
                    if (!bagger)
                    {
                        if (!AutoColorPicker)
                        {

                            if (Colorpicker.ShowDialog() != DialogResult.OK) return;
                            Kreiscolor = Colorpicker.Color;
                        }
                        else
                        {
                            Kreiscolor = GetRandomColor();
                        }

                        nameChar = (char)('A' + (nextAutoIndex % 26));// Automatische Namensvergabe von A-Z
                        nextAutoIndex++;

                        Inseln.Add(new Insel(e.Location, Kreiscolor, nameChar));
                        adjazenzliste.Add(new List<int>());
                        if (animationTimer != null) animationTimer.Stop();
                        eulerWeg = null;
                        teilkreise.Clear();
                        currentPathIndex = 0;
                        brueckenFarbwechselAktiv = false;
                        brueckenFarbwechselSchritt = 0;

                    }
                    else
                    {


                    }

                }
                else
                {
                    // Linksklick auf freie Fläche -> automatische A..Z-Vergabe

                    //Inseln.Add(new Insel(e.Location, Kreiscolor, nameChar));
                }


            }



        }
        // Invertierte Farbe (einfaches RGB-Inverse)
        private Color InvertColor(Color c)
        {
            return Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B);
        }

        // Kontrastfarbe (wählt Schwarz oder Weiß basierend auf Helligkeit)
        private Color GetContrastingColor(Color c)
        {
            // relative Luminanz-Approximation
            double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        private char AskNameChar(char defaultChar = 'A')
        {
            string s = Microsoft.VisualBasic.Interaction.InputBox("Buchstabe für den Punkt(Zeichen):", "Punkt benennen", defaultChar.ToString());
            if (string.IsNullOrWhiteSpace(s)) return '\0'; // Abbruchsignal
            return s[0]; // erstes Zeichen verwenden
        }

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            // Prüfen, ob die RECHTE Maustaste gedrückt wurde
            draggedIslandIndex = GetInselIndexAt(e.Location);
            if (draggedIslandIndex != -1 && e.Button == MouseButtons.Left && !bagger)// Linksklick auf eine Insel
            {
                dragStartPosition = e.Location;// Drag starten

                return;
            }

            // Position speichern
            //Inseln.Add(e.Location);
            HandleMouseInput(e);
            // Panel anweisen, sich neu zu zeichnen (löst Paint-Ereignis aus)
            panel1.Invalidate();

        }
        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            // Drag aktiv?
            if (draggedIslandIndex != -1 && e.Button == MouseButtons.Left && !bagger)
            {
                Point newPos = new Point(
                    Inseln[draggedIslandIndex].Position.X + (e.X - dragStartPosition.X),
                    Inseln[draggedIslandIndex].Position.Y + (e.Y - dragStartPosition.Y)
                );
                // Prüfe ob neue Position innerhalb des Panels
                if (newPos.X - kreisRadius < 0 || newPos.X + kreisRadius > panel1.Width ||
                    newPos.Y - kreisRadius < 0 || newPos.Y + kreisRadius > panel1.Height)
                {
                    // MessageBox.Show("Insel kann nicht außerhalb des Panels platziert werden!");
                    draggedIslandIndex = -1; // Drag abbrechen
                    return;
                }
                // Prüfe ob neue Position zu nah bei anderen Inseln
                int Abstand = 60;
                bool ueberschneidung = Inseln.Any(p =>// Alle Inseln durchgehen
                {
                    if (p == Inseln[draggedIslandIndex]) return false; // Ignore self

                    double dx = p.Position.X - newPos.X;
                    double dy = p.Position.Y - newPos.Y;
                    return (dx * dx + dy * dy) < (Abstand * Abstand);
                });

                if (ueberschneidung)
                {
                    // MessageBox.Show("Zu nah an anderer Insel!");
                    draggedIslandIndex = -1; // Drag abbrechen
                    return;
                }
                Inseln[draggedIslandIndex].Position = newPos;
                dragStartPosition = e.Location;
                panel1.Invalidate();
            }

            // Cursor ändern wenn über Insel
            //panel1.Cursor = GetInselIndexAt(e.Location) != -1 ? Cursors.Hand : Cursors.Default;
        }

        // Alle Kreise löschen
        private void btnClearCircles_Click(object sender, EventArgs e)
        {
            if (animationTimer != null) animationTimer.Stop();
            eulerWeg = null;
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;
            Inseln.Clear();
            Bruecken.Clear();
            adjazenzliste.Clear();
            startNr = -1;
            nextAutoIndex = 0;
            panel1.Invalidate();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            // Dateidialog konfigurieren
            SaveFile.Filter = "Text file (*.txt)|*.txt|XML file (*.xml)|*.xml|PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All files (*.*)|*.*";
            SaveFile.FileName = "Graph.txt"; // Standarddateiname
            SaveFile.OverwritePrompt = true;
            SaveFile.Title = "Karte speichern";
            SaveFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

            if (SaveFile.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(SaveFile.FileName)) return;
            Bitmap bmp = null;
            try
            {

                string extension = Path.GetExtension(SaveFile.FileName).ToLowerInvariant();
                // Panel als Bild rendern
                bmp = new Bitmap(panel1.Width, panel1.Height);
                panel1.DrawToBitmap(bmp, new Rectangle(0, 0, panel1.Width, panel1.Height));

                // Dateiendung extrahieren
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    bmp = new Bitmap(panel1.Width, panel1.Height);
                    panel1.DrawToBitmap(bmp, new Rectangle(0, 0, panel1.Width, panel1.Height));
                }


                // Je nach Dateityp speichern
                switch (extension)
                {
                    case ".xml":
                        // XML-Daten des Panels speichern (hier müsstest du eigene Logik implementieren)
                        SavePanelAsXml(SaveFile.FileName);
                        break;

                    case ".txt":
                        // Text-Daten des Panels speichern
                        SaveGraph(SaveFile.FileName);
                        break;

                    case ".png":
                        bmp.Save(SaveFile.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        break;

                    case ".jpg":
                    case ".jpeg":
                        bmp.Save(SaveFile.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;

                    default:
                        // Standardmäßig als PNG speichern
                        //  string newFileName = Path.ChangeExtension(SaveFile.FileName, ".xml");
                        SaveGraph(SaveFile.FileName);
                        //bmp.Save(newFileName, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                }

                //bmp.Dispose(); // Ressourcen freigeben
                MessageBox.Show("Panel erfolgreich gespeichert!", "Info",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern: " + ex.Message, "Fehler",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
            }
            panel1.Invalidate();


        }

        // Speichern als XML
        private void SavePanelAsXml(string path)
        {
            var doc = new XDocument(
        new XElement("Graph",
            new XElement("Inseln",
                from ins in Inseln
                select new XElement("Inseln",
                    new XElement("X", ins.Position.X),
                    new XElement("Y", ins.Position.Y),
                    new XElement("R", ins.Farbe.R),
                    new XElement("G", ins.Farbe.G),
                    new XElement("B", ins.Farbe.B),
                    new XElement("Name", ins.Name)
                )
            ),
            new XElement("Brücken",
                from br in Bruecken
                select new XElement("Bridge",
                    new XElement("A", br.InselA),
                    new XElement("B", br.InselB),
                    new XElement("R", br.Farbe.R),
                    new XElement("G", br.Farbe.G),
                    new XElement("Blue", br.Farbe.B),
                    new XElement("EdgeName", br.Name)
                )
            )
        )
    );

            doc.Save(path);

        }

        // Beispielmethode zum Speichern als Text
        private void SaveGraph(string path)
        {
            using (var w = new StreamWriter(path))
            {
                w.WriteLine("#INSELN");
                foreach (var ins in Inseln)
                {
                    w.WriteLine($"{ins.Position.X};{ins.Position.Y};{ins.Farbe.R};{ins.Farbe.G};{ins.Farbe.B};{ins.Name}");
                }
                w.WriteLine("#BRUECKEN");
                foreach (var br in Bruecken)
                {
                    w.WriteLine($"{br.InselA};{br.InselB};{br.Farbe.R};{br.Farbe.G};{br.Farbe.B};{br.Name}");
                }
            }

        }



        private void fontDialog1_Apply(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!Eulerkreis()) return; // Prüfe zuerst die Bedingungen
            eulerWeg = Hierholzer();

            if (eulerWeg == null || eulerWeg.Count == 0)
            {
                MessageBox.Show("Fehler bei der Berechnung des Eulerkreises!");
                return;
            }

            var lines = new List<string>();
            lines.Add("Brücken:");
            for (int i = 0; i < teilkreise.Count; i++)
            {
                string kantenText = FormatiereKantenfolge(teilkreise[i].Inselfolge);
                string farbName = string.IsNullOrWhiteSpace(teilkreise[i].FarbName)
                    ? $"RGB({teilkreise[i].Farbe.R},{teilkreise[i].Farbe.G},{teilkreise[i].Farbe.B})"
                    : teilkreise[i].FarbName;
                lines.Add($"Teilkreis {i + 1}: {kantenText} => {farbName}");



            }
            lines.Add("");
            lines.Add($"Eulerpfad: {FormatiereKantenfolge(eulerWeg)}");
            lines.Add("");
            lines.Add("Inseln:"); 
            for (int i = 0; i < teilkreise.Count; i++)
            {
                lines.Add($"Teilkreis {i + 1}: {FormatiereKnotenfolge(teilkreise[i].Inselfolge)}");
            }
            if (lines.Count > 0) lines.Add(string.Empty);

            lines.Add($"Eulerpfad: {FormatiereKnotenfolge(eulerWeg)}");
            textBox1.Text = string.Join(Environment.NewLine, lines);

            StarteBrueckenFarbwechselZuTeilkreisen();
            // --- NEU: Animation starten ---
            currentPathIndex = 0;
            if (animationTimer != null) animationTimer.Start();
            panel1.Invalidate();

        }
        private List<int> Hierholzer()
        {
            var adjazenzKopie = adjazenzliste.Select(neighbors => new List<int>(neighbors)).ToList();
            teilkreise.Clear();

            int start = -1;
            for (int i = 0; i < adjazenzKopie.Count; i++)
            {
                if (adjazenzKopie[i].Count > 0)
                {
                    start = i;
                    break;
                }
            }

            if (start == -1) return new List<int>();

            var eulerkreis = BaueEinfachenKreis(start, adjazenzKopie);
            if (eulerkreis.Count < 2 || eulerkreis[eulerkreis.Count - 1] != eulerkreis[0])
            {
                teilkreise.Clear();
                return new List<int>();
            }

            teilkreise.Add(new Teilkreis
            {
                StartInsl = start,
                Inselfolge = new List<int>(eulerkreis),
                Farbe = GetTeilkreisFarbe(teilkreise.Count),
                FarbName = GetTeilkreisFarbName(teilkreise.Count)
            });

            while (true)
            {
                int spliceIndex = -1;
                for (int i = 0; i < eulerkreis.Count - 1; i++)
                {
                    int knoten = eulerkreis[i];
                    if (knoten >= 0 && knoten < adjazenzKopie.Count && adjazenzKopie[knoten].Count > 0)
                    {
                        spliceIndex = i;
                        break;
                    }
                }

                if (spliceIndex == -1) break;

                int teilStart = eulerkreis[spliceIndex];
                var teilkreis = BaueEinfachenKreis(teilStart, adjazenzKopie);
                if (teilkreis.Count < 2 || teilkreis[teilkreis.Count - 1] != teilkreis[0]) break;

                teilkreise.Add(new Teilkreis
                {
                    StartInsl = teilStart,
                    Inselfolge = new List<int>(teilkreis),
                    Farbe = GetTeilkreisFarbe(teilkreise.Count),
                    FarbName = GetTeilkreisFarbName(teilkreise.Count)
                });

                eulerkreis = FuegeTeilkreisEin(eulerkreis, teilkreis, spliceIndex);
            }

            return eulerkreis;
        }
        private void button6_Click(object sender, EventArgs e)// Alle Inseln und Brücken löschen
        {
            if (animationTimer != null) animationTimer.Stop(); // Timer stoppen
            eulerWeg = null; // Pfad löschen
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;

            Inseln.Clear();
            Bruecken.Clear();
            adjazenzliste.Clear();
            nextAutoIndex = 0;
            startNr = -1;
            panel1.Invalidate();
        }

        private void LoadGraphFromXml(string path)
        {
            // XML-Struktur:
            var doc = XDocument.Load(path);
            var islands = doc.Root.Element("Inseln")?.Elements("Inseln");
            var bridges = doc.Root.Element("Brücken")?.Elements("Bridge"); // ← Bridge (Singular!)

            Inseln.Clear();
            Bruecken.Clear();
            adjazenzliste.Clear();
            if (animationTimer != null) animationTimer.Stop();
            eulerWeg = null;
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;
            startNr = -1;

            if (islands != null)
            {
                foreach (var x in islands)
                {// Werte aus XML extrahieren
                    int px = (int)x.Element("X");
                    int py = (int)x.Element("Y");
                    int r = (int)x.Element("R");
                    int g = (int)x.Element("G");
                    int b = (int)x.Element("B");
                    char name = x.Element("Name")?.Value.FirstOrDefault() ?? ' ';
                    Inseln.Add(new Insel(new Point(px, py), Color.FromArgb(r, g, b), name));
                    adjazenzliste.Add(new List<int>());
                }
            }

            if (bridges != null)
            {
                foreach (var bridgeElement in bridges)
                {
                    string aStr = bridgeElement.Element("A")?.Value;
                    string bStr = bridgeElement.Element("B")?.Value;

                    if (int.TryParse(aStr, out int aIdx) && int.TryParse(bStr, out int bIdx))
                    {
                        if (aIdx >= 0 && aIdx < adjazenzliste.Count && bIdx >= 0 && bIdx < adjazenzliste.Count)
                        {
                            BrueckeBauen(aIdx, bIdx);

                            var br = FindeBruecke(aIdx, bIdx);
                            if (br != null)
                            {
                                int r = 0;
                                int g = 0;
                                int blue = 0;
                                bool hasColor =
                                    int.TryParse(bridgeElement.Element("R")?.Value, out r) &&
                                    int.TryParse(bridgeElement.Element("G")?.Value, out g) &&
                                    int.TryParse(bridgeElement.Element("Blue")?.Value, out blue);

                                if (hasColor)
                                {
                                    var loadedColor = Color.FromArgb(
                                        Math.Max(0, Math.Min(255, r)),
                                        Math.Max(0, Math.Min(255, g)),
                                        Math.Max(0, Math.Min(255, blue))
                                    );
                                    br.Farbe = loadedColor;
                                    br.StartFarbe = loadedColor;
                                    br.ZielFarbe = loadedColor;
                                }

                                string nameRaw = bridgeElement.Element("EdgeName")?.Value ?? bridgeElement.Element("Name")?.Value;
                                if (!string.IsNullOrWhiteSpace(nameRaw))
                                {
                                    br.Name = nameRaw[0];
                                }
                            }
                        }
                    }
                }
            }

            panel1.Invalidate();
        }
        /// <summary>
        /// Lädt das einfache Textformat:
        /// #INSELN
        /// x;y;R;G;B;Name
        /// ...
        /// #BRUECKEN
        /// a;b;r;g;blue;name   (Farbe/Name optional)
        /// </summary>
        private void LoadGraphFromText(string path)//
        {
            var lines = File.ReadAllLines(path);
            Inseln.Clear();
            Bruecken.Clear();
            adjazenzliste.Clear();
            if (animationTimer != null) animationTimer.Stop();
            eulerWeg = null;
            teilkreise.Clear();
            currentPathIndex = 0;
            brueckenFarbwechselAktiv = false;
            brueckenFarbwechselSchritt = 0;
            startNr = -1;

            bool readingIslands = false;
            bool readingBridges = false;

            for (int ln = 0; ln < lines.Length; ln++)
            {
                var raw = lines[ln];
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();

                if (line.StartsWith("#INSELN", StringComparison.OrdinalIgnoreCase))
                {
                    readingIslands = true;
                    readingBridges = false;
                    continue;
                }
                if (line.StartsWith("#BRUECKEN", StringComparison.OrdinalIgnoreCase))
                {
                    readingBridges = true;
                    readingIslands = false;
                    continue;
                }

                if (readingIslands)
                {
                    // Format: x;y;R;G;B;Name
                    var parts = line.Split(';');
                    if (parts.Length < 6) continue; // ungültige Zeile überspringen
                                                    // Werte parsen
                    if (!int.TryParse(parts[0], out int x)) continue;
                    if (!int.TryParse(parts[1], out int y)) continue;
                    if (!int.TryParse(parts[2], out int r)) continue;
                    if (!int.TryParse(parts[3], out int g)) continue;
                    if (!int.TryParse(parts[4], out int b)) continue;

                    char name = parts[5].Length > 0 ? parts[5][0] : ' ';
                    var color = Color.FromArgb(
                        Math.Max(0, Math.Min(255, r)),
                        Math.Max(0, Math.Min(255, g)),
                        Math.Max(0, Math.Min(255, b))
                    );

                    Inseln.Add(new Insel(new Point(x, y), color, name));
                    adjazenzliste.Add(new List<int>());
                }
                else if (readingBridges)
                {
                    // Format: a;b;r;g;blue;name  (r,g,blue,name optional)
                    var parts = line.Split(';');
                    if (parts.Length < 2) continue;
                    if (int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b))
                    {
                        // nur gültige Indizes akzeptieren
                        if (a >= 0 && a < adjazenzliste.Count && b >= 0 && b < adjazenzliste.Count)
                        {
                            // Brücke bauen (prüft Duplikate)
                            BrueckeBauen(a, b);

                            var br = FindeBruecke(a, b);
                            if (br != null)
                            {
                                int r = 0;
                                int g = 0;
                                int blue = 0;
                                if (parts.Length >= 5 &&
                                    int.TryParse(parts[2], out r) &&
                                    int.TryParse(parts[3], out g) &&
                                    int.TryParse(parts[4], out blue))
                                {
                                    var loadedColor = Color.FromArgb(
                                        Math.Max(0, Math.Min(255, r)),
                                        Math.Max(0, Math.Min(255, g)),
                                        Math.Max(0, Math.Min(255, blue))
                                    );
                                    br.Farbe = loadedColor;
                                    br.StartFarbe = loadedColor;
                                    br.ZielFarbe = loadedColor;
                                }

                                if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]))
                                {
                                    br.Name = parts[5][0];
                                }
                            }
                        }
                    }
                }
            }

            // Optional: nextAutoIndex anpassen, damit neue Inseln fortlaufend benannt werden
            nextAutoIndex = Inseln.Count;

            panel1.Invalidate();
        }

        private void button5_Click(object sender, EventArgs e)//BAAAAAAGGEEEEEEERRRRRRR
        {
            bagger = !bagger;
            if (bagger)
            {
                // Bitmap aus Ressourcen laden
                Bitmap bm = Properties.Resources.baggerico;  // Ersetze "Bild1" durch den Ressourcennamen

                // Größe des Bitmaps anpassen (optional, falls nicht schon 16x16)
                Bitmap resizedBm = new Bitmap(bm, new Size(128, 128));

                // Cursor aus dem Bitmap-Icon erstellen
                Cursor customCursor = new Cursor(resizedBm.GetHicon());

                // Cursor auf TextBox setzen
                panel1.Cursor = customCursor;

                button4.Enabled = false;
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button6.Enabled = false;
                button5.ForeColor = Color.Black;
                button5.Font = new Font(button5.Font.FontFamily, 12f, FontStyle.Bold);
            }
            else
            {
                button5.ForeColor = SystemColors.ControlText;
                button5.Font = new Font(button5.Font.FontFamily, 7.8f, FontStyle.Regular); // kleiner + normal
                button4.Enabled = true;
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button6.Enabled = true;
                // Cursor zurücksetzen
                panel1.Cursor = Cursors.Default;

            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Eulerkreis();
        }
        bool Eulerkreis()
        {

            int kantenAnzahl = adjazenzliste.Sum(nachbarn => nachbarn.Count) / 2;
            if (kantenAnzahl == 0)
            {
                string msg = "Keine Brücken vorhanden. Ein Eulerkreis kann nicht gebildet werden.";
                textBox1.Text = msg;
                MessageBox.Show(msg);
                button1.BackColor = Color.OrangeRed;
                return false;
            }
            if (!EinsameInsel())
            {
                string msg = "Der Graph ist nicht zusammenhängend. Ein Eulerkreis ist nicht möglich.";
                textBox1.Text = msg;
                MessageBox.Show(msg);
                button1.BackColor = Color.OrangeRed;
                return false;
            }// Prüfen ob alle Inseln verbunden sind

            for(int i = 0; i < adjazenzliste.Count; i++)
            {
                if (Grad(i) % 2 != 0)//kein Eulerkreis möglich wenn es Inseln mit ungeradem Grad gibt
                {
                    string msg = "Es gibt Inseln mit ungeradem Grad. Ein Eulerkreis ist nicht möglich.";
                    textBox1.Text = msg;
                    MessageBox.Show(msg);
                    button1.BackColor = Color.OrangeRed;
                    return false;
                }
            }
            button1.BackColor = Color.Lime;
            return true;

        }
        private int Grad(int i)
        {
            if (i < 0 ||i >= adjazenzliste.Count) return 0;// keine Inseln oder error
            return adjazenzliste[i].Count;
        }
        bool EinsameInsel()
        {
            var verbundenenI = Enumerable.Range(0, Inseln.Count).Where(i => Grad(i) > 0).ToList();
            if(verbundenenI.Count == 0) return false;// keine Brücken daher kein Eulerkreis


            int start = verbundenenI[0];
            var Besucht = new HashSet<int>();//welche Inseln wurden schon besucht
            var Warteschlange = new Queue<int>();//die Inseln die noch geprüft werden müssen

            //Breitensuche
            Warteschlange.Enqueue(start);
            Besucht.Add(start);

            while(Warteschlange.Count > 0)
            {
                int current = Warteschlange.Dequeue();
                foreach(int NachbarInsl in adjazenzliste[current])
                {
                    if (!Besucht.Contains(NachbarInsl))
                    {
                        Besucht.Add(NachbarInsl);
                        Warteschlange.Enqueue(NachbarInsl);
                    }
                }
            }
            return verbundenenI.All(i => Besucht.Contains(i)); // Alle verbundenen Inseln müssen besucht worden sein
        }

        private void ZeichneTeilkreise(Graphics g)
        {
            if (teilkreise == null || teilkreise.Count == 0) return;

            foreach (var teilkreis in teilkreise)
            {
                if (teilkreis == null || teilkreis.Inselfolge == null || teilkreis.Inselfolge.Count < 2) continue;

                var farbe = Color.FromArgb(170, teilkreis.Farbe);
                using (var pen = new Pen(farbe, 8))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    for (int i = 0; i < teilkreis.Inselfolge.Count - 1; i++)
                    {
                        int from = teilkreis.Inselfolge[i];
                        int to = teilkreis.Inselfolge[i + 1];

                        if (from < 0 || from >= Inseln.Count || to < 0 || to >= Inseln.Count) continue;
                        g.DrawLine(pen, Inseln[from].Position, Inseln[to].Position);
                    }
                }
            }
        }

        private Color GetTeilkreisFarbe(int index)
        {
            if (teilkreisPalette == null || teilkreisPalette.Length == 0) return Color.DeepPink;
            return teilkreisPalette[index % teilkreisPalette.Length];
        }

        private string GetTeilkreisFarbName(int index)
        {
            if (teilkreisFarbNamen == null || teilkreisFarbNamen.Length == 0) return string.Empty;
            return teilkreisFarbNamen[index % teilkreisFarbNamen.Length];
        }

        private List<int> BaueEinfachenKreis(int start, List<List<int>> adjazenzKopie)
        {
            var kreis = new List<int>();
            if (start < 0 || start >= adjazenzKopie.Count) return kreis;

            kreis.Add(start);
            int current = start;
            int maxSchritte = adjazenzKopie.Sum(n => n.Count) + 1;

            for (int schritt = 0; schritt < maxSchritte; schritt++)
            {
                if (adjazenzKopie[current].Count == 0) break;

                int next = adjazenzKopie[current][0];
                adjazenzKopie[current].Remove(next);
                adjazenzKopie[next].Remove(current);
                current = next;
                kreis.Add(current);

                if (current == start) break;
            }

            return kreis;
        }

        private List<int> FuegeTeilkreisEin(List<int> hauptkreis, List<int> teilkreis, int index)
        {
            var merged = new List<int>();
            if (hauptkreis == null || teilkreis == null) return merged;
            if (index < 0 || index >= hauptkreis.Count) return new List<int>(hauptkreis);

            for (int i = 0; i < index; i++) merged.Add(hauptkreis[i]);
            merged.AddRange(teilkreis);
            for (int i = index + 1; i < hauptkreis.Count; i++) merged.Add(hauptkreis[i]);

            return merged;
        }

        private string FormatiereKnotenfolge(List<int> folge)
        {   
            if (folge == null || folge.Count == 0) return string.Empty;

            return string.Join(",", folge.Select(i =>
                i >= 0 && i < Inseln.Count ? Inseln[i].Name.ToString() : "?"));
        }

        private string FormatiereKantenfolge(List<int> inselfolge)
        {
            if (inselfolge == null || inselfolge.Count < 2) return string.Empty;

            var kantenNamen = new List<string>();
            for (int i = 0; i < inselfolge.Count - 1; i++)
            {
                var br = FindeBruecke(inselfolge[i], inselfolge[i + 1]);
                kantenNamen.Add(br != null ? br.Name.ToString() : "?");
            }

            return string.Join(",", kantenNamen);
        }

        private Color ErmittleBrueckenStartfarbe(int a, int b)
        {
            if (a < 0 || a >= Inseln.Count || b < 0 || b >= Inseln.Count) return Color.Red;

            var c1 = Inseln[a].Farbe;
            var c2 = Inseln[b].Farbe;
            return Color.FromArgb((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2);
        }

        private string BaueKantenKey(int a, int b)
        {
            int min = Math.Min(a, b);
            int max = Math.Max(a, b);
            return $"{min};{max}";
        }

        private Bruecke FindeBruecke(int a, int b)
        {
            return Bruecken.FirstOrDefault(br =>
                (br.InselA == a && br.InselB == b) ||
                (br.InselA == b && br.InselB == a));
        }

        private void StarteBrueckenFarbwechselZuTeilkreisen()
        {
            if (Bruecken.Count == 0)
            {
                brueckenFarbwechselAktiv = false;
                brueckenFarbwechselSchritt = 0;
                return;
            }

            var zielFarben = new Dictionary<string, Color>();
            foreach (var teilkreis in teilkreise)
            {
                if (teilkreis == null || teilkreis.Inselfolge == null || teilkreis.Inselfolge.Count < 2) continue;

                for (int i = 0; i < teilkreis.Inselfolge.Count - 1; i++)
                {
                    int a = teilkreis.Inselfolge[i];
                    int b = teilkreis.Inselfolge[i + 1];
                    string key = BaueKantenKey(a, b);
                    if (!zielFarben.ContainsKey(key)) zielFarben[key] = teilkreis.Farbe;
                }
            }

            foreach (var br in Bruecken)
            {
                string key = BaueKantenKey(br.InselA, br.InselB);
                br.StartFarbe = br.Farbe;
                br.ZielFarbe = zielFarben.ContainsKey(key) ? zielFarben[key] : br.Farbe;
            }

            brueckenFarbwechselSchritt = 0;
            brueckenFarbwechselAktiv = true;
        }

        private Color InterpoliereFarbe(Color start, Color ziel, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            int r = (int)Math.Round(start.R + (ziel.R - start.R) * t);
            int g = (int)Math.Round(start.G + (ziel.G - start.G) * t);
            int b = (int)Math.Round(start.B + (ziel.B - start.B) * t);
            return Color.FromArgb(r, g, b);
        }

        // Neue Methode zum Visualisieren des Pfads
        private void VisualisierePfad(Graphics g)
        {
            // Zeichnet nur die aktuell aktive Kante, damit die Brückenfarbe sichtbar bleibt.
            if (currentPathIndex < eulerWeg.Count - 1)
            {
                using (var activePen = new Pen(Color.LimeGreen, 6))
                {
                    activePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    activePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    int from = eulerWeg[currentPathIndex];
                    int to = eulerWeg[currentPathIndex + 1];
                    if (from < 0 || from >= Inseln.Count || to < 0 || to >= Inseln.Count) return;

                    Point startPoint = Inseln[from].Position;
                    Point endPoint = Inseln[to].Position;

                    g.DrawLine(activePen, startPoint, endPoint);
                }
            }
        }

        // Das Event, das bei jedem Tick des Timers ausgelöst wird
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            bool hatSichEtwasGeaendert = false;

            if (brueckenFarbwechselAktiv)
            {
                brueckenFarbwechselSchritt++;
                float t = brueckenFarbwechselSchritt / (float)BrueckenFarbwechselSchritte;
                if (t > 1f) t = 1f;

                foreach (var br in Bruecken)
                {
                    br.Farbe = InterpoliereFarbe(br.StartFarbe, br.ZielFarbe, t);
                }

                hatSichEtwasGeaendert = true;
                if (t >= 1f) brueckenFarbwechselAktiv = false;
            }

            if (eulerWeg != null && currentPathIndex < eulerWeg.Count - 1)
            {
                currentPathIndex++;
                hatSichEtwasGeaendert = true;
            }

            if (hatSichEtwasGeaendert)
            {
                panel1.Invalidate(); // Löst das Paint-Event aus und zeichnet den nächsten Schritt
            }

            bool eulerAnimationFertig = (eulerWeg == null || currentPathIndex >= eulerWeg.Count - 1);
            if (eulerAnimationFertig && !brueckenFarbwechselAktiv)
            {
                animationTimer.Stop(); // Animation am Ende stoppen
            }
        }

        private void RebuildBridgeListFromAdjacency()
        {
            var alteBruecken = Bruecken.ToDictionary(
                br => BaueKantenKey(br.InselA, br.InselB),
                br => br);

            Bruecken.Clear();
            for (int i = 0; i < adjazenzliste.Count; i++)
            {
                foreach (int j in adjazenzliste[i])
                {
                    if (j <= i) continue;
                    string key = BaueKantenKey(i, j);
                    char brName = (char)('a' + (Bruecken.Count % 26));
                    var startFarbe = ErmittleBrueckenStartfarbe(i, j);

                    Bruecke alteBruecke;
                    if (alteBruecken.TryGetValue(key, out alteBruecke))
                    {
                        brName = alteBruecke.Name;
                        startFarbe = alteBruecke.Farbe;
                    }

                    Bruecken.Add(new Bruecke(i, j, startFarbe, brName)
                    {
                        StartFarbe = startFarbe,
                        ZielFarbe = startFarbe
                    });
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public class Insel// Repräsentiert eine Insel mit Position, Farbe und Name
    {
        public Point Position { get; set; }
        public Color Farbe { get; set; }
        public char Name { get; set; } // neu

        public Insel(Point pos, Color farbe, char name = ' ')
        {
            Position = pos;
            Farbe = farbe;
            Name = name; // Standardname
        }
    }
    class Teilkreis
    {
        public int StartInsl { get; set; }
        public List<int> BrueckenBenuetz { get; set; } = new List<int>();
        public List<int> Inselfolge { get; set; } = new List<int>();
        public Color Farbe { get; set; } = Color.Red;
        public string FarbName { get; set; } = "";
    }
    public class Bruecke
    {
        public int InselA { get; set; }
        public int InselB { get; set; }
        public char Name { get; set; }
        public Color Farbe { get; set; }
        public Color StartFarbe { get; set; }
        public Color ZielFarbe { get; set; }
        public bool Benutzt { get; set; }

        public Bruecke(int a, int b, Color farbe, char name = ' ', bool benutzt = false)
        {
            InselA = a;
            InselB = b;
            Farbe = farbe;
            StartFarbe = farbe;
            ZielFarbe = farbe;
            Name = name;
            Benutzt = benutzt;
        }
    }
}
