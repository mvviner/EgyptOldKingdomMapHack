using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EgyptOldKingdomMapHack {
    public partial class Form1 : Form {
        private string CurrentFilename = null;
        private EgyptSave CurrentSave = null;
        private string DirectoryPath;
        private Timer Timer = new Timer();
        public Form1() {
            InitializeComponent();
            DirectoryPath = Environment.CurrentDirectory;
            if (!File.Exists(Path.Combine(DirectoryPath, "save_map.json")))
                DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Clarus Victoria\Egypt Old-Kingdom\saves");
            try {
                ShowLatestFile();
                Timer.Tick += (sender, e) => ShowLatestFile();
                FileSystemWatcher fsw = new FileSystemWatcher(DirectoryPath, "*.json") { SynchronizingObject = this };
                fsw.Created += (sender, e) => StartTimer();
                fsw.Changed += (sender, e) => StartTimer();
                fsw.Deleted += (sender, e) => StartTimer();
                fsw.EnableRaisingEvents = true;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }
        private void StartTimer() {
            Timer.Interval = 1000;
            Timer.Enabled = true;
        }
        private void PerformBackup() {
            string AutosaveFileName = Path.Combine(DirectoryPath, "autosave.json");
            if (File.Exists(AutosaveFileName)) {
                string NewName = Path.Combine(DirectoryPath, "save" + Directory.GetLastWriteTime(AutosaveFileName).ToString("yyyyMMddHHmmss") + ".json");
                if (!File.Exists(NewName))
                    File.Move(AutosaveFileName, NewName);
            }
            string BackupDirectoryPath = Path.Combine(DirectoryPath, "backup");
            if (!Directory.Exists(BackupDirectoryPath))
                Directory.CreateDirectory(BackupDirectoryPath);
            List<string> SaveFiles = Directory.GetFiles(DirectoryPath, "*.json").Select(s => Path.GetFileName(s)).ToList();
            List<string> BackupSaveFiles = Directory.GetFiles(BackupDirectoryPath, "*.json").Select(s => Path.GetFileName(s)).ToList();
            foreach (string s in SaveFiles.Except(BackupSaveFiles, StringComparer.InvariantCultureIgnoreCase))
                File.Copy(Path.Combine(DirectoryPath, s), Path.Combine(BackupDirectoryPath, s));
            foreach (string s in BackupSaveFiles.Except(SaveFiles, StringComparer.InvariantCultureIgnoreCase))
                File.Copy(Path.Combine(BackupDirectoryPath, s), Path.Combine(DirectoryPath, s));
        }
        private void ShowLatestFile() {
            try {
                PerformBackup();
                bool update = false;
                foreach (string s in Directory.GetFiles(DirectoryPath, "*.json")) {
                    string s1 = Path.GetFileNameWithoutExtension(s);
                    s1 = s1.IndexOf("autosave", StringComparison.CurrentCultureIgnoreCase) >= 0 ? Directory.GetLastWriteTime(s).ToString("yyyyMMddHHmmss") + s1 : s1;
                    if (s1.IndexOf("save_map", StringComparison.CurrentCultureIgnoreCase) < 0 && (CurrentFilename == null || CurrentFilename.CompareTo(s1) < 0)) {
                        CurrentFilename = s1;
                        update = true;
                    }
                }
                if (!update) return;
                string Filename = Path.Combine(DirectoryPath, (CurrentFilename.IndexOf("autosave", StringComparison.CurrentCultureIgnoreCase) >= 0 ? CurrentFilename.Substring(14) : CurrentFilename) + ".json");
                string json = File.ReadAllText(Filename);
                int pos = json.IndexOf("}");
                json = json.Substring(pos + 1);
                CurrentSave = JsonConvert.DeserializeObject<EgyptSave>(json);
                Refresh();
            } catch (IOException) {
                StartTimer();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            Refresh();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (CurrentSave == null) return;
            TurnLabel.Text = "Turn: " + CurrentSave.Turn;
            int TotalTurnsToExplore = 0;
            int WorkersScouting = 0;
            if (!ShowEgyptMapCheckBox.Checked) {
                float size = Math.Max(Math.Min(ClientSize.Width - 20, ClientSize.Height - 80), 160);
                RectangleF rect = new RectangleF(10, 60, size, size);
                DrawMap(e.Graphics, rect, CurrentSave.City, ref TotalTurnsToExplore, ref WorkersScouting);
            } else {
                float sizev = Math.Min(ClientSize.Width - 20, (ClientSize.Height - 80) / 3);
                float sizeh = Math.Min((ClientSize.Width - 30) / 2, (ClientSize.Height - 20) / 2);
                bool vertical = sizev > sizeh;
                float size = Math.Max(Math.Max(sizeh, sizev), 160);
                RectangleF rect1 = new RectangleF(10, 60, size, size);
                RectangleF rect2 = new RectangleF(vertical ? 10 : 20 + size, vertical ? rect1.Bottom + 10 : Math.Max(10, 80 - size / 2), size, size * 2);
                DrawMap(e.Graphics, rect1, CurrentSave.City, ref TotalTurnsToExplore, ref WorkersScouting);
                DrawMap(e.Graphics, rect2, CurrentSave.Egypt, ref TotalTurnsToExplore, ref WorkersScouting);
            }
            //int cnt = 0;
            //foreach (Event ev in Schedule.EventList.Where(ev => ev.Turn > CurrentSave.Turn || ev.EndTurn > CurrentSave.Turn)) {
            //    Label l = cnt == 0 ? Event1Label : cnt == 1 ? Event2Label : Event3Label;
            //    l.Text = ev.Text;
            //    //l.ForeColor = ev.Turn == CurrentSave.Turn + 1 ? Color.Red : Color.Black;
            //    if (++cnt == 3) break;
            //}
            //if (cnt == 0) Event1Label.Text = "";
            //if (cnt <= 1) Event2Label.Text = "";
            //if (cnt <= 2) Event3Label.Text = "";
        }
        private void DrawMap(Graphics g, RectangleF rect, Map Map, ref int TotalTurnsToExplore, ref int WorkersScouting) {
            List<string> tasks = new List<string>();
            PointF center = new PointF(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            float coef = rect.Width / 26;
            g.DrawRectangle(Pens.Black, rect.Left, rect.Top, rect.Width, rect.Height);
            foreach (var cell in Map.Cells) {
                bool exploring = false;
                //string text = (cell.LandscapeId == "h_highlands" ? "h" : cell.LandscapeId == "h_wadi" ? "w" : cell.LandscapeId == "h_hills" ? "b" : cell.LandscapeId == "h_river" ? "r" : cell.LandscapeId == "h_floodplain" ? "f" : cell.LandscapeId == "h_shore" ? "s" : "");
                string text = (cell.LandscapeId == "h_hills" ? "h"
                    : cell.LandscapeId == "h_river" ? "r"
                    : cell.LandscapeId == "h_floodplain" && Map == CurrentSave.Egypt ? "r"
                    : cell.LandscapeId == "landscape_sea" ? "s" : "");
                foreach (CellTask ct in cell.Tasks) {
                    if (ct.Name == "task_explore") {
                        exploring = true;
                        int TurnsToExplore = ct.Complexity - ct.Progress;
                        TotalTurnsToExplore += TurnsToExplore;
                        WorkersScouting += cell.WorkersCount;
                        text += TurnsToExplore.ToString();
                    } else if (ct.Name == "task_abandonedvillage_name")
                        text += "";
                    else if (ct.Name == "task_acacia_name")
                        text += "P";
                    else if (ct.Name == "task_papyrus_name")
                        text += "";
                    else if (ct.Name == "task_flint_name")
                        text += "";
                    else if (ct.Name == "task_land_of_ancestors_name")
                        text += "";
                    else if (ct.Name == "task_thoth_land_name")
                        text += "";
                    else if (ct.Name == "task_fertileland_name")
                        text += "B";
                    else if (ct.Name == "task_goodclay_name")
                        text += "";
                    else if (ct.Name == "task_wildcereals_name")
                        text += "";
                    else if (ct.Name == "task_fertileglade_name")
                        text += "F";
                    else if (ct.Name == "task_savages_name")
                        text += "F";
                    else if (ct.Name == "task_antelope_name")
                        text += "F";
                    else if (ct.Name == "task_urus_name")
                        text += "F";
                    else if (ct.Name == "task_birds_name")
                        text += "F";
                    else if (ct.Name == "task_fish_name")
                        text += "F";
                    else if (ct.Name == "task_lion_name")
                        text += "A";
                    else if (ct.Name == "task_crocodile_name")
                        text += "A";
                    else if (ct.Name == "task_hippo_name")
                        text += "A";
                    else if (ct.Name == "task_hyenas_name")
                        text += "A";
                    else if (ct.Name == "task_create_society_name") {
                        Society s = CurrentSave.GetSociety(ct.SocietyId);
                        if (s != null && s.NonColonized)
                            text += "C";
                        else if (Map == CurrentSave.City)
                            text += "T";
                        //else if (s != null && s.PlayerRelationship > 66)
                        //    text += "^";
                        //else if (s != null && s.PlayerRelationship > 33)
                        //    text += "=";
                        //else if (s != null)
                        //    text += "v";
                    } else if (ct.Name == "task_egypt_sea_fish_name")
                        text += "F";
                    else if (ct.Name == "task_sand_land_name")
                        text += "S";
                    else if (ct.Name == "task_cursedplace_name")
                        text += "S";
                    else if (ct.Name == "task_wildgrove_name")
                        text += "S";
                    else if (ct.Name == "task_swamp_name")
                        text += "S";
                    else if (ct.Name == "task_deepquagmire_name")
                        text += "S";
                    else if (ct.Name == "task_egypt_cursed_lands_name")
                        text += "S";
                    else if (ct.Name == "task_travels_name")
                        text += "R";
                    else if (ct.IsStrategicResource && ct.Prices.production + ct.Prices.luxury * 2 >= 3)
                        text += "B";
                    else {
                        if (!tasks.Contains(ct.Name))
                            tasks.Add(ct.Name);
                    }
                }
                if (text == "") text = "-";
                var Borders = cell.Borders.Select(p => new PointF(center.X + (cell.X + p.x) * coef, center.Y - (cell.Y + p.y) * coef)).ToArray();
                var size = g.MeasureString(text, Font);
                if (cell.IsExplored)
                    g.FillEllipse(Brushes.LightGreen, center.X + cell.X * coef - coef, center.Y - cell.Y * coef - coef, coef * 2, coef * 2);
                if (!cell.IsExplored && cell.Tasks.Count > 0 && exploring)
                    g.FillEllipse(Brushes.LightCyan, center.X + cell.X * coef - coef, center.Y - cell.Y * coef - coef, coef * 2, coef * 2);
                bool GoodSpot = text.Contains("F") || text.Contains("C") || text.Contains("R");
                g.DrawString(text, Font, GoodSpot ? Brushes.Blue : Brushes.Black, center.X + cell.X * coef - size.Width / 2, center.Y - cell.Y * coef - size.Height / 2);
            }
            string TaskPrompt = "";
            foreach (string TaskName in tasks)
                TaskPrompt += TaskName + "\r\n";
            //MessageBox.Show(TaskPrompt);
        }
        private void ShowEgyptMapCheckBox_CheckedChanged(object sender, EventArgs e) {
            Refresh();
        }
    }
    public class Event {
        public int Turn { get; set; }
        public int EndTurn { get; set; }
        public string Text { get; set; }
        public Event(int Turn, int EndTurn, string Text) { this.Turn = Turn; this.EndTurn = EndTurn; this.Text = Text; }
    }
    public static class Schedule {
        private static string ScheduleString = @"
38-42 - 1st trial (assimilate until 37; or food 13 + hammers 76)
40 - +1 pop
66-69 - 2nd trial (favor 50+)
76 - start of wet period
108 - 3rd trial - war against Narmer (power 500+)
119 - Upper Egypt joins
120-122 - build temple + palace (food 30+, hammers 250+)
119-169 - I dynasty - 8 tombs
151 - start of arid period
153 - 4th trial (favor 32, hammers 26, luxuries 18)
169-204 - II dynasty - 5 tombs + building
184-193 - 5th trial - Civil war (hammers 200+, power 1500+)
193 - win the war, start of wet period
204-223 - III dynasty - 4 tombs
223-242 - 6th trial - 10 tombs + 8 dwellings
223-252 - IV dynasty
252-271 - V dynasty - 9 tombs
265 - 7th trial (food 570+, hammers 570+, luxury 390+)
271-292 - VI dynasty - 4 tombs 
289 - 8th trial (favor 2000)";
        public static List<Event> EventList = new List<Event>();
        static Schedule() {
            string[] strings = ScheduleString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in strings) {
                int pos = s.IndexOf(" - ");
                if (pos > 0) {
                    int pos2 = s.Substring(0, pos).IndexOf("-");
                    int turn = 0, endTurn = 0;
                    if (pos2 > 0) {
                        int.TryParse(s.Substring(0, pos2), out turn);
                        int.TryParse(s.Substring(pos2 + 1, pos - pos2 - 1), out endTurn);
                    } else int.TryParse(s.Substring(0, pos), out turn);
                    if (turn > 0)
                        EventList.Add(new Event(turn, endTurn, s));
                }
            }
        }
    }
    public class Cell {
        public string LandscapeId { get; set; }
        public bool IsExplored { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int WorkersCount { get; set; }
        public List<CellTask> Tasks { get; set; }
        public List<BorderPoint> Borders { get; set; }
        public override string ToString() => $"{LandscapeId}";
    }
    public class AllEventsNode {
        public AllEventsMapNode map { get; set; }
    }
    public class AllEventsMapNode {
        public AllEventsMapGlobalsNode globals { get; set; }
    }
    public class AllEventsMapGlobalsNode {
        public JObject Globals { get; set; }
        //public Dictionary<int, int> Plundered { get; set; } = new Dictionary<int, int>();
        //public float explorationComplexityMod { get; set; } = 1;
        //[OnDeserialized]
        //private void OnDeserialized(StreamingContext context) {
        //    foreach (var prop in Globals.Properties())
        //        if (prop.Name.StartsWith("plundered_")) {
        //            int key = int.Parse(prop.Name.Substring(10));
        //            int value = (int)(double)((JValue)prop.Value["value"]).Value;
        //            Plundered.Add(key, value);
        //        } else if (prop.Name == "explorationComplexityMod")
        //            explorationComplexityMod = (float)(double)((JValue)prop.Value["value"]).Value;
        //}
    }
    public class ResourcePrice {
        public float production { get; set; }
        public float luxury { get; set; }
    }
    public class CellTask {
        public int Capacity { get; set; }
        public int Complexity { get; set; }
        public int Progress { get; set; }
        public string Name { get; set; }
        public string SocietyId { get; set; }
        public bool IsStrategicResource { get; set; }
        public ResourcePrice Prices { get; set; }
        public override string ToString() => $"{Name}";
    }
    public class BorderPoint {
        public float x { get; set; }
        public float y { get; set; }
        public override string ToString() => $"{x}, {y}";
    }
    public class CultResource {
        public string ID { get; set; }
        public string IncomeResourceId { get; set; }
    }
    public class Map {
        public List<Cell> Cells { get; set; }
    }
    public class Relationship {
        public string TargetSocietyId { get; set; }
        public float Value { get; set; }
    }
    public class Society {
        public string Id { get; set; }
        public bool NonColonized { get; set; }
        public List<Relationship> Relationships { get; set; }
        private float? _PlayerRelationship = null;
        public float PlayerRelationship {
            get {
                if (_PlayerRelationship == null) {
                    Relationship r = Relationships.FirstOrDefault(r1 => r1.TargetSocietyId == "player");
                    _PlayerRelationship = r == null ? -100 : r.Value;
                }
                return (float)_PlayerRelationship;
            }
        }
    }
    public class EgyptSave {
        public int Turn { get; set; }
        public Map City { get; set; }
        public Map Egypt { get; set; }
        public List<CultResource> CultResources { get; set; }
        public List<Society> Societies { get; set; }
        public AllEventsNode AllEvents { get; set; }
        public Society GetSociety(string SocietyId) => Societies.FirstOrDefault(s => s.Id == SocietyId);
    }
}
