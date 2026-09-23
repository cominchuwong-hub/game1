using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;

namespace EmojiTowerDefense
{
    public class GameMode { public string Id { get; set; } public string Name { get; set; } public string Icon { get; set; } public string Desc { get; set; } }
    public class Character { public string Emoji { get; set; } public string Name { get; set; } public int Cost { get; set; } public int Dmg { get; set; } public int Range { get; set; } public double Speed { get; set; } public string Desc { get; set; } }
    public class EnemyType { public string Emoji { get; set; } public string Name { get; set; } public int Hp { get; set; } public double Speed { get; set; } public int Reward { get; set; } }
    public class ShopItem { public string Id { get; set; } public string Name { get; set; } public string Desc { get; set; } public int Price { get; set; } public Action Effect { get; set; } }
    public class RedeemCode { public int Gold { get; set; } public int Token { get; set; } public int Lives { get; set; } public string Msg { get; set; } }
    public class MapPathPoint { public int X { get; set; } public int Y { get; set; } }
    public class MapData { public List<MapPathPoint> Path { get; set; } public MapPathPoint Start { get; set; } public MapPathPoint End { get; set; } }
    public class PlacedTower { public int CharIndex { get; set; } public int TileX { get; set; } public int TileY { get; set; } public double Cooldown { get; set; } public int Level { get; set; } }
    public class Enemy { public EnemyType Type { get; set; } public int Hp { get; set; } public int MaxHp { get; set; } public double Speed { get; set; } public int Reward { get; set; } public bool Alive { get; set; } public int PathIndex { get; set; } public double X { get; set; } public double Y { get; set; } public string Emoji { get; set; } public double SlowTimer { get; set; } public bool IsBoss { get; set; } }
    public class Projectile { public double X { get; set; } public double Y { get; set; } public Enemy Target { get; set; } public string Emoji { get; set; } public bool Active { get; set; } = true; }
    public class SaveData { public string Username { get; set; } public string Password { get; set; } public int Tokens { get; set; } public int Lives { get; set; } public int MaxLives { get; set; } public int Gold { get; set; } public List<int> OwnedCharIds { get; set; } public List<int> SelectedCharIds { get; set; } public List<string> PurchasedItems { get; set; } public int Speed { get; set; } = 1; public int Difficulty { get; set; } = 1; public bool Autowave { get; set; } = false; }

    public partial class GameForm : Form
    {
        // ================================================================
        // HTML COLOR PALETTE (exact match)
        // ================================================================
        static readonly Color BG_GRAD_TOP = Color.FromArgb(11, 14, 44);    // #0b0e2c
        static readonly Color BG_GRAD_MID = Color.FromArgb(48, 23, 78);    // #30174e
        static readonly Color BG_GRAD_BOT = Color.FromArgb(18, 19, 42);    // #12132a
        static readonly Color GLASS_BG = Color.FromArgb(18, 255, 255, 255); // rgba(255,255,255,0.07)
        static readonly Color GLASS_BORDER = Color.FromArgb(45, 255, 255, 255); // rgba(255,255,255,0.18)
        static readonly Color PINK = Color.FromArgb(255, 92, 139);          // #ff5c8b
        static readonly Color PINK_LIGHT = Color.FromArgb(255, 163, 120);  // #ffa37a
        static readonly Color CYAN = Color.FromArgb(64, 255, 240);
        static readonly Color GREEN = Color.FromArgb(62, 230, 126);         // #3ee67e
        static readonly Color GREEN_LIGHT = Color.FromArgb(94, 255, 204);  // #5effcc
        static readonly Color ORANGE = Color.FromArgb(255, 185, 73);       // #ffb949
        static readonly Color PURPLE = Color.FromArgb(163, 104, 255);
        static readonly Color PURPLE_SHADOW = Color.FromArgb(92, 51, 190); // #5c33be
        static readonly Color MAP_BG = Color.FromArgb(22, 22, 39);         // #161627
        static readonly Color TEXT_MAIN = Color.FromArgb(245, 248, 255);
        static readonly Color TEXT_DIM = Color.FromArgb(170, 185, 220);
        static readonly Color TEXT_GRAY = Color.FromArgb(124, 138, 180);
        static readonly Color INPUT_BG = Color.FromArgb(24, 31, 58); // solid backing for text inputs
        static readonly Color INPUT_BORDER = Color.FromArgb(108, 130, 255); // vibrant cyan/purple border
        static readonly Color OVERLAY_BG = Color.FromArgb(204, 0, 0, 0);   // rgba(0,0,0,0.8)

        // ================================================================
        // DATA
        // ================================================================
        private readonly List<GameMode> GAME_MODES = new List<GameMode> {
            new GameMode { Id = "easy", Name = "ง่าย", Icon = "🟢", Desc = "ศัตรูอ่อน 20% ทองเพิ่ม" },
            new GameMode { Id = "normal", Name = "ปกติ", Icon = "🟡", Desc = "ความยากมาตรฐาน" },
            new GameMode { Id = "hard", Name = "นรก", Icon = "🔴", Desc = "ศัตรู 130% + เดินเร็ว" },
            new GameMode { Id = "endless", Name = "ไม่จำกัด", Icon = "🔥", Desc = "เล่นไม่รู้จบ ศัตรูโหดขึ้น" }
        };
        private readonly List<Character> CHARACTERS = new List<Character> {
            new Character { Emoji = "🐱", Name = "แมวส้ม", Cost = 100, Dmg = 15, Range = 120, Speed = 1.0, Desc = "โจมตีเร็วปานกลาง" },
            new Character { Emoji = "🐶", Name = "หมาโบ้", Cost = 150, Dmg = 35, Range = 100, Speed = 1.5, Desc = "ดาเมจหนักหน่วง" },
            new Character { Emoji = "🦊", Name = "จิ้งจอกสไน", Cost = 250, Dmg = 50, Range = 220, Speed = 2.5, Desc = "ระยะไกล ดาเมจสูง" },
            new Character { Emoji = "🐰", Name = "กระต่ายสปีด", Cost = 180, Dmg = 10, Range = 90, Speed = 0.4, Desc = "ยิงรัวเร็วมาก" },
            new Character { Emoji = "🐻", Name = "หมีน้ำแข็ง", Cost = 300, Dmg = 20, Range = 110, Speed = 1.8, Desc = "ทำให้ศัตรูช้าลง 50%" },
            new Character { Emoji = "🦁", Name = "สิงโตทอง", Cost = 500, Dmg = 120, Range = 150, Speed = 2.0, Desc = "ราชาแห่งดาเมจ" }
        };
        private readonly List<EnemyType> ENEMY_TYPES = new List<EnemyType> {
            new EnemyType { Emoji = "🐭", Name = "หนูจี๊ด", Hp = 30, Speed = 2.2, Reward = 15 },
            new EnemyType { Emoji = "🐹", Name = "แฮมสเตอร์", Hp = 50, Speed = 1.8, Reward = 20 },
            new EnemyType { Emoji = "🐰", Name = "กระต่ายป่า", Hp = 80, Speed = 2.5, Reward = 25 },
            new EnemyType { Emoji = "🦊", Name = "จิ้งจอกเจ้าเล่ห์", Hp = 120, Speed = 2.0, Reward = 35 },
            new EnemyType { Emoji = "🐗", Name = "หมูป่าคลั่ง", Hp = 250, Speed = 1.4, Reward = 50 },
            new EnemyType { Emoji = "🦁", Name = "สิงโตดุร้าย", Hp = 500, Speed = 1.2, Reward = 80 },
            new EnemyType { Emoji = "🐉", Name = "มังกรยักษ์", Hp = 1500, Speed = 0.8, Reward = 200 }
        };
        private readonly Dictionary<string, RedeemCode> REDEEM_CODES = new Dictionary<string, RedeemCode>(StringComparer.OrdinalIgnoreCase) {
            { "FREEGOLD", new RedeemCode { Gold = 500, Token = 0, Lives = 0, Msg = "🎁 ได้รับทอง 500 ทอง!" } },
            { "FREETOKEN", new RedeemCode { Gold = 0, Token = 100, Lives = 0, Msg = "🎁 โทเค่นฟรี 100!" } },
            { "GODMODE", new RedeemCode { Gold = 9999, Token = 999, Lives = 99, Msg = "👑 โหมดพระเจ้า!" } }
        };

        private List<ShopItem> SHOP_ITEMS;
        private int tokens = 150, lives = 20, maxLives = 20, gold = 300, kills = 0, wave = 1;
        private string selectedMode = "normal";
        private List<int> selectedChars = new List<int> { 0, 1, 2 };
        private bool isPlaying = false, isGameOver = false, autoWave = false;
        private int gameSpeed = 1, difficulty = 1;
        private double damageMultiplier = 1.0, speedMultiplier = 1.0;
        private List<PlacedTower> placedTowers = new List<PlacedTower>();
        private List<Enemy> enemies = new List<Enemy>();
        private List<Projectile> projectiles = new List<Projectile>();
        private HashSet<string> purchasedItems = new HashSet<string>();
        private int activePlacingCharIndex = -1;
        private PlacedTower selectedTowerForUpgrade = null;

        private string machineId = "";
        private Dictionary<string, SaveData> saves = new Dictionary<string, SaveData>();
        private SaveData currentSave = null;
        private const string SAVES_FILE = "td_saves.json";
        private const string RESTORE_SERVER = "http://localhost:3000";

        private readonly MapData mapData = new MapData {
            Start = new MapPathPoint { X = 0, Y = 250 }, End = new MapPathPoint { X = 1000, Y = 250 },
            Path = new List<MapPathPoint> {
                new MapPathPoint { X = 0, Y = 250 }, new MapPathPoint { X = 150, Y = 250 },
                new MapPathPoint { X = 150, Y = 100 }, new MapPathPoint { X = 350, Y = 100 },
                new MapPathPoint { X = 350, Y = 400 }, new MapPathPoint { X = 550, Y = 400 },
                new MapPathPoint { X = 550, Y = 150 }, new MapPathPoint { X = 750, Y = 150 },
                new MapPathPoint { X = 750, Y = 350 }, new MapPathPoint { X = 900, Y = 350 },
                new MapPathPoint { X = 900, Y = 250 }, new MapPathPoint { X = 1000, Y = 250 }
            }
        };

        private Timer gameLoopTimer = new Timer();
        private int waveSpawnCount = 0, waveSpawnMax = 0, waveSpawnCooldown = 0;
        private bool isSpawningWave = false;

        private Panel pnlLogin, pnlMenu, pnlGame, pnlShop, pnlSettings, pnlGameOver;
        private Label lblTokens, lblLives, lblGold, lblKills, lblWave, lblWaveStatus;
        private FlowLayoutPanel flpModeGrid, flpCharGrid, flpSelectedCharsInGame, flpShopItems;
        private PictureBox pbMap;
        private Panel pnlContainerMain; // main glass container

        // ================================================================
        // CONSTRUCTOR
        // ================================================================
        public GameForm()
        {
            try
            {
                machineId = GetMachineId(); LoadSaves(); SHOP_ITEMS = CreateShopItems();
                InitializeUI();
                this.Shown += (s, e) =>
                {
                    ShowLogin();
                    this.Refresh();
                };
                ShowLogin(); // Always require login/password
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CRASH: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        // ================================================================
        // MACHINE ID / SAVE HELPERS
        // ================================================================
        private string GetMachineId()
        {
            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && !n.Description.Contains("Virtual") && !n.Description.Contains("Loopback")).ToList();
                if (nics.Count > 0) return string.Join("", nics[0].GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
            }
            catch { }
            return Environment.MachineName + "_" + Environment.UserName;
        }
        private string GetSavesPath() { string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EmojiTD"); Directory.CreateDirectory(dir); return Path.Combine(dir, SAVES_FILE); }
        private void LoadSaves() { try { string path = GetSavesPath(); if (File.Exists(path)) { string json = File.ReadAllText(path); var dict = JsonSerializer.Deserialize<Dictionary<string, SaveData>>(json); if (dict != null) saves = dict; } } catch { saves = new Dictionary<string, SaveData>(); } }
        private void SaveSaves() { try { string path = GetSavesPath(); string json = JsonSerializer.Serialize(saves, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(path, json); } catch { } }
        private void LoadSaveData() { if (currentSave == null) return; tokens = currentSave.Tokens; lives = currentSave.Lives; maxLives = currentSave.MaxLives; gold = currentSave.Gold; selectedChars = new List<int>(currentSave.SelectedCharIds ?? new List<int> { 0, 1, 2 }); purchasedItems = new HashSet<string>(currentSave.PurchasedItems ?? new List<string>()); gameSpeed = currentSave.Speed; autoWave = currentSave.Autowave; difficulty = currentSave.Difficulty; damageMultiplier = 1.0; speedMultiplier = 1.0; }
        private void SaveCurrentData() { if (currentSave == null) return; currentSave.Tokens = tokens; currentSave.Lives = lives; currentSave.MaxLives = maxLives; currentSave.Gold = gold; currentSave.SelectedCharIds = new List<int>(selectedChars); currentSave.PurchasedItems = new List<string>(purchasedItems); currentSave.Speed = gameSpeed; currentSave.Autowave = autoWave; currentSave.Difficulty = difficulty; saves[machineId] = currentSave; SaveSaves(); }
        private List<ShopItem> CreateShopItems() => new List<ShopItem> { new ShopItem { Id = "dmg_boost", Name = "💥 เพิ่มพลังโจมตี +25%", Desc = "พลังโจมตีป้อมทั้งหมด +25%", Price = 250, Effect = () => { foreach (var c in CHARACTERS) c.Dmg = (int)(c.Dmg * 1.25); } }, new ShopItem { Id = "range_boost", Name = "👁️ เพิ่มระยะยิง +20%", Desc = "ระยะยิงป้อมทั้งหมด +20%", Price = 300, Effect = () => { foreach (var c in CHARACTERS) c.Range = (int)(c.Range * 1.20); } }, new ShopItem { Id = "speed_boost", Name = "⚡ เพิ่มความเร็ว +15%", Desc = "ความเร็วโจมตีป้อม +15%", Price = 350, Effect = () => { foreach (var c in CHARACTERS) c.Speed = c.Speed * 0.85; } }, new ShopItem { Id = "extra_lives", Name = "❤️ เพิ่มหัวใจ +10", Desc = "เพิ่มพลังชีวิตทันที 10 ดวง", Price = 150, Effect = () => { lives += 10; maxLives += 10; } } };

        // ================================================================
        // DRAW HELPERS
        // ================================================================
        private const int FORM_W = 1060;
        private const int FORM_H = 870;
        private const int CONTAINER_W = 1000;
        private const int CONTAINER_H = 790;
        private const int PANEL_W = 960;
        private const int PANEL_H = 750;
        private const int TILE = 40;
        private const int MAP_W = 920;
        private const int MAP_H = 460;
        private const int GRID_COLS = 25;
        private const int GRID_ROWS = 12;

        private GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, rad, rad, 180, 90); gp.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
            gp.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90); gp.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
            gp.CloseFigure(); return gp;
        }
        private void DrawCard(Graphics g, Rectangle r, int rad, Color fill, Color border)
        {
            using (var gp = RoundedRect(r, rad)) { using (var b = new SolidBrush(fill)) g.FillPath(b, gp); using (var p = new Pen(border, 2)) g.DrawPath(p, gp); }
        }
        private void DrawGlow(Graphics g, float x, float y, float r, Color c, byte a) { using (var b = new SolidBrush(Color.FromArgb(a, c))) g.FillEllipse(b, x - r, y - r, r * 2, r * 2); }

        private void DrawGradientButton(PaintEventArgs e, Rectangle rect, int rad, Color c1, Color c2, float angle, string text, Font font, Color textColor)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var gp = RoundedRect(rect, rad))
            using (var b = new LinearGradientBrush(rect, c1, c2, angle))
                e.Graphics.FillPath(b, gp);
            TextRenderer.DrawText(e.Graphics, text, font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DrawInputBox(Graphics g, Rectangle r, int rad, bool focused)
        {
            using (var gp = RoundedRect(r, rad))
            {
                using (var b = new SolidBrush(INPUT_BG))
                    g.FillPath(b, gp);
                using (var p = new Pen(focused ? PINK : INPUT_BORDER, 1))
                    g.DrawPath(p, gp);
            }
        }

        private static Color SafeInputBackColor()
        {
            return Color.FromArgb(25, 22, 55);
        }

        private static Color SafeTransparentBackground()
        {
            return Color.FromArgb(22, 22, 38);
        }

        // ================================================================
        // UI INIT
        // ================================================================
        private void InitializeUI()
        {
            this.SuspendLayout();
            this.Text = "🐱 Tower Defense - อีโมจิทาวเวอร์ดีเฟ้นส์";
            this.Size = new Size(1060, 870);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;

            // Background gradient panel
            Panel bgGrad = new Panel { Dock = DockStyle.Fill };
            bgGrad.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;
                if (w > 0 && h > 0)
                {
                    using (var b = new LinearGradientBrush(new Rectangle(0, 0, w, h / 2), BG_GRAD_TOP, BG_GRAD_MID, 135f))
                        e.Graphics.FillRectangle(b, 0, 0, w, h / 2);
                    using (var b2 = new LinearGradientBrush(new Rectangle(0, h / 2, w, h / 2), BG_GRAD_MID, BG_GRAD_BOT, 135f))
                        e.Graphics.FillRectangle(b2, 0, h / 2, w, h / 2);
                }
            };
            this.Controls.Add(bgGrad);

            // Main glass container (like HTML #game-container)
            pnlContainerMain = new Panel
            {
                Size = new Size(1000, 790),
                Location = new Point(22, 22),
                BackColor = Color.FromArgb(18, 18, 30)
            };
            pnlContainerMain.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, 1000, 790);
                using (var gp = RoundedRect(r, 20))
                {
                    using (var b = new SolidBrush(GLASS_BG))
                        e.Graphics.FillPath(b, gp);
                    using (var p = new Pen(GLASS_BORDER, 1))
                        e.Graphics.DrawPath(p, gp);
                }
                DrawGlow(e.Graphics, 500, 395, 300, PURPLE_SHADOW, 15);
            };
            bgGrad.Controls.Add(pnlContainerMain);

            // Layer 1 — Login
            pnlLogin = new Panel { Size = new Size(960, 750), Location = new Point(20, 20), BackColor = Color.FromArgb(22, 22, 38), Visible = true };
            pnlContainerMain.Controls.Add(pnlLogin);
            InitLoginScreen();

            // Layer 2 — Menu
            pnlMenu = new Panel { Size = new Size(960, 750), Location = new Point(20, 20), BackColor = Color.FromArgb(22, 22, 38), Visible = false };
            pnlContainerMain.Controls.Add(pnlMenu);
            InitMenuScreen();

            // Layer 3 — Game
            pnlGame = new Panel { Size = new Size(960, 750), Location = new Point(20, 20), BackColor = Color.FromArgb(22, 22, 38), Visible = false };
            pnlContainerMain.Controls.Add(pnlGame);
            InitGameScreen();

            gameLoopTimer.Interval = 30;
            gameLoopTimer.Tick += GameLoopTimer_Tick;
            this.ResumeLayout(false);
        }

        // ================================================================
        // SCREEN: LOGIN — HTML design: logo, title, username, start btn,
        //              restore section, hint box, machine ID
        // ================================================================
        private void InitLoginScreen()
        {
            int cx = 480; // horizontal center for elements (960/2)

            // Logo (h1 style)
            AddLabel(pnlLogin, "🛡️🏰", new Font("Segoe UI Emoji", 48, FontStyle.Bold),
                Color.White, 960, 70, 0, 25, ContentAlignment.MiddleCenter);
            // Title (h2 style) — orange like HTML
            AddLabel(pnlLogin, "🐱 อีโมจิทาวเวอร์ดีเฟ้นส์ 🐱",
                new Font("Segoe UI", 22, FontStyle.Bold),
                ORANGE, 960, 40, 0, 95, ContentAlignment.MiddleCenter);
            // Subtitle — dim text like HTML
            AddLabel(pnlLogin, "สะสมตัวละคร สุ่มหาตัวหายาก ป้องกันฐาน!",
                new Font("Segoe UI", 11), TEXT_DIM, 960, 25, 0, 138, ContentAlignment.MiddleCenter);

            // --- USERNAME FIELD (styled like HTML input) ---
            int fldW = 420, fldX = (960 - fldW) / 2; // centered
            AddLabel(pnlLogin, "👤 ชื่อผู้เล่น", new Font("Segoe UI", 10, FontStyle.Bold),
                Color.FromArgb(204, 204, 204), fldW, 20, fldX, 180, ContentAlignment.MiddleLeft);

            TextBox txtName = new TextBox
            {
                Size = new Size(fldW, 44), Location = new Point(fldX, 205),
                Font = new Font("Segoe UI", 14),
                BackColor = SafeInputBackColor(),
                ForeColor = TEXT_MAIN,
                BorderStyle = BorderStyle.None,
                Text = ""
            };
            txtName.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var gp = RoundedRect(new Rectangle(0, 0, fldW, 44), 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                        e.Graphics.FillPath(b, gp);
                    using (var p = new Pen(INPUT_BORDER, 1))
                        e.Graphics.DrawPath(p, gp);
                }
            };
            pnlLogin.Controls.Add(txtName);

            // --- PASSWORD FIELD (4-digit PIN) ---
            AddLabel(pnlLogin, "🔑 รหัสผ่าน (4 หลัก)", new Font("Segoe UI", 10, FontStyle.Bold),
                Color.FromArgb(204, 204, 204), fldW, 20, fldX, 255, ContentAlignment.MiddleLeft);

            TextBox txtPwd = new TextBox
            {
                Size = new Size(fldW, 44), Location = new Point(fldX, 280),
                Font = new Font("Segoe UI", 14),
                BackColor = SafeInputBackColor(),
                ForeColor = TEXT_MAIN,
                BorderStyle = BorderStyle.None,
                PasswordChar = '*',
                MaxLength = 4,
                Text = ""
            };
            txtPwd.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var gp = RoundedRect(new Rectangle(0, 0, fldW, 44), 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                        e.Graphics.FillPath(b, gp);
                    using (var p = new Pen(INPUT_BORDER, 1))
                        e.Graphics.DrawPath(p, gp);
                }
            };
            pnlLogin.Controls.Add(txtPwd);

            // --- LOGIN BUTTON (HTML .login-btn style: gradient #f093fb → #f5576c) ---
            int btnW = fldW;
            var btnPlay = new Button
            {
                Text = "🔓 เข้าสู่ระบบ",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                BackColor = SafeTransparentBackground(),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(btnW, 52), Location = new Point(fldX, 335),
                Cursor = Cursors.Hand, ForeColor = Color.White
            };
            btnPlay.Paint += (s, e) => DrawGradientButton(e, new Rectangle(0, 0, btnW, 52), 10,
                PINK_LIGHT, PINK, 135f, "🔓 เข้าสู่ระบบ",
                new Font("Segoe UI", 18, FontStyle.Bold), Color.White);
            btnPlay.Click += (s, e) => DoLogin(txtName.Text, txtPwd.Text);
            pnlLogin.Controls.Add(btnPlay);

            // --- REGISTER BUTTON (green gradient) ---
            var btnReg = new Button
            {
                Text = "📝 สมัครสมาชิกใหม่",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = SafeTransparentBackground(),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(btnW, 44), Location = new Point(fldX, 395),
                Cursor = Cursors.Hand, ForeColor = Color.Black
            };
            btnReg.Paint += (s, e) => DrawGradientButton(e, new Rectangle(0, 0, btnW, 44), 10,
                GREEN, GREEN_LIGHT, 135f, "📝 สมัครสมาชิกใหม่",
                new Font("Segoe UI", 12, FontStyle.Bold), Color.Black);
            btnReg.Click += (s, e) => DoRegister(txtName.Text, txtPwd.Text);
            pnlLogin.Controls.Add(btnReg);

            // --- DIVIDER "หรือ" ---
            AddLabel(pnlLogin, "──────  หรือ  ──────",
                new Font("Segoe UI", 10), TEXT_GRAY, 200, 20, (960-200)/2, 450, ContentAlignment.MiddleCenter);

            // --- RESTORE SECTION ---
            AddLabel(pnlLogin, "📋 กู้คืนข้อมูล (สำหรับย้ายข้อมูลข้ามเครื่อง)",
                new Font("Segoe UI", 10, FontStyle.Bold),
                ORANGE, fldW, 20, fldX, 480, ContentAlignment.MiddleCenter);

            // Restore textbox (4-char code)
            TextBox txtRestore = new TextBox
            {
                Size = new Size(fldW, 40), Location = new Point(fldX, 505),
                Font = new Font("Segoe UI", 12),
                BackColor = SafeInputBackColor(),
                ForeColor = Color.FromArgb(102, 102, 102),
                BorderStyle = BorderStyle.None,
                MaxLength = 4,
                Text = "รหัส 4 ตัว..."
            };
            txtRestore.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var gp = RoundedRect(new Rectangle(0, 0, fldW, 40), 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                        e.Graphics.FillPath(b, gp);
                    using (var p = new Pen(INPUT_BORDER, 1))
                        e.Graphics.DrawPath(p, gp);
                }
            };
            txtRestore.GotFocus += (s, e) =>
            {
                if (txtRestore.Text == "รหัส 4 ตัว...")
                { txtRestore.Text = ""; txtRestore.ForeColor = TEXT_MAIN; }
            };
            pnlLogin.Controls.Add(txtRestore);

            // Restore button — same gradient style
            var btnRestore = new Button
            {
                Text = "📥 นำเข้าข้อมูลจากรหัสกู้คืน",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = SafeTransparentBackground(), FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(btnW, 46), Location = new Point(fldX, 555),
                Cursor = Cursors.Hand, ForeColor = Color.White
            };
            btnRestore.Paint += (s, e) => DrawGradientButton(e, new Rectangle(0, 0, btnW, 46), 10,
                PINK_LIGHT, PINK, 135f, "📥 นำเข้าข้อมูลจากรหัสกู้คืน",
                new Font("Segoe UI", 11, FontStyle.Bold), Color.White);
            btnRestore.Click += (s, e) =>
            {
                string input = txtRestore.Text.Trim();
                if (string.IsNullOrWhiteSpace(input) || input == "รหัส 4 ตัว...")
                { MessageBox.Show("❌ กรุณากรอกรหัสกู้คืน", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (input.Length > 20) DoImport(input); else DoRestoreLocal(input);
            };
            pnlLogin.Controls.Add(btnRestore);

            // --- HINT BOX (like HTML .login-hint) ---
            Panel hintBox = new Panel
            {
                Size = new Size(400, 65), Location = new Point((960-400)/2, 620),
                BackColor = SafeTransparentBackground()
            };
            hintBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 400, 65), 10,
                    Color.FromArgb(13, 255, 255, 255), INPUT_BORDER);
            };
            AddLabel(hintBox, "💡 ยังไม่มีบัญชี? ใส่ชื่อ + รหัสผ่าน 4 หลัก แล้วกดสมัคร!\n📋 ใช้รหัสกู้คืน 4 ตัวย้ายข้อมูลข้ามเครื่อง",
                new Font("Segoe UI", 9), TEXT_GRAY, 380, 45, 10, 10, ContentAlignment.MiddleCenter);
            pnlLogin.Controls.Add(hintBox);

            // --- MACHINE ID (bottom) ---
            AddLabel(pnlLogin, $"💻 ID: {machineId.Substring(0, Math.Min(machineId.Length, 10))}",
                new Font("Segoe UI", 8), Color.FromArgb(70, 70, 100), 500, 18, 230, 700, ContentAlignment.MiddleCenter);
        }

        // ================================================================
        // SCREEN: MENU (matches HTML menu screen)
        // ================================================================
        private void InitMenuScreen()
        {
            // Logo
            AddLabel(pnlMenu, "🛡️🏰", new Font("Segoe UI Emoji", 42, FontStyle.Bold),
                Color.White, 960, 60, 0, 5, ContentAlignment.MiddleCenter);
            AddLabel(pnlMenu, "🐱 อีโมจิทาวเวอร์ดีเฟ้นส์ 🐱",
                new Font("Segoe UI", 20, FontStyle.Bold),
                ORANGE, 960, 35, 0, 60, ContentAlignment.MiddleCenter);

            // Player info bar
            lblTokens = AddLabel(pnlMenu, $"👍 {tokens} โทเค่น",
                new Font("Segoe UI", 12, FontStyle.Bold),
                ORANGE, 200, 25, 30, 105, ContentAlignment.MiddleLeft);

            Label lblUser = AddLabel(pnlMenu, $"👤 {currentSave?.Username ?? "ผู้เล่น"}",
                new Font("Segoe UI", 11, FontStyle.Bold),
                TEXT_MAIN, 200, 25, 30, 130, ContentAlignment.MiddleLeft);

            // Export + Clear buttons
            var btnExportM = CreateGradientButton("📋 ส่งออก", new Font("Segoe UI", 9, FontStyle.Bold),
                90, 28, 700, 108, PINK_LIGHT, PINK, Color.White);
            btnExportM.Click += (s, e) => DoExport();
            pnlMenu.Controls.Add(btnExportM);

            var btnClear = CreateGradientButton("🗑️ ล้าง", new Font("Segoe UI", 9, FontStyle.Bold),
                80, 28, 800, 108, Color.FromArgb(40, 30, 30), Color.FromArgb(40, 30, 30), PINK);
            pnlMenu.Controls.Add(btnClear);
            btnClear.Click += (s, e) => ClearMachineData();

            // — MODE SECTION (like HTML .mode-grid) —
            AddLabel(pnlMenu, "🎮 เลือกโหมดเกม", new Font("Segoe UI", 16, FontStyle.Bold),
                Color.FromArgb(204, 204, 204), 400, 30, 280, 155, ContentAlignment.MiddleCenter);

            flpModeGrid = new FlowLayoutPanel
            {
                Size = new Size(800, 120), Location = new Point(80, 192),
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = SafeTransparentBackground()
            };
            pnlMenu.Controls.Add(flpModeGrid);
            RenderModeGrid();

            // — CHARACTER SECTION —
            AddLabel(pnlMenu, "🎯 เลือกตัวละคร (สูงสุด 3 ตัว)",
                new Font("Segoe UI", 15, FontStyle.Bold),
                TEXT_MAIN, 400, 25, 280, 320, ContentAlignment.MiddleCenter);

            flpCharGrid = new FlowLayoutPanel
            {
                Size = new Size(880, 155), Location = new Point(40, 350),
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = SafeTransparentBackground()
            };
            pnlMenu.Controls.Add(flpCharGrid);
            RenderCharGrid();

            // — SUMMON & REDEEM BAR —
            Panel pnlBar = new Panel { Size = new Size(880, 60), Location = new Point(40, 520), BackColor = SafeTransparentBackground() };
            pnlBar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 880, 60), 10,
                    Color.FromArgb(20, 255, 255, 255), INPUT_BORDER);
            };
            pnlMenu.Controls.Add(pnlBar);

            var btnSummon = CreateGradientButton("🔮 สุ่มตัวละคร (50 โทเค่น)", new Font("Segoe UI", 11, FontStyle.Bold),
                230, 42, 15, 9, ORANGE, PINK, Color.White);
            btnSummon.Click += (s, e) => SummonCharacter();
            pnlBar.Controls.Add(btnSummon);

            // Redeem code field
            AddLabel(pnlBar, "โค้ด:", new Font("Segoe UI", 10), TEXT_DIM, 50, 20, 270, 12, ContentAlignment.MiddleLeft);
            TextBox txtCode = new TextBox
            {
                Size = new Size(180, 30), Location = new Point(315, 15),
                Font = new Font("Segoe UI", 11),
                BackColor = SafeInputBackColor(),
                ForeColor = TEXT_MAIN, BorderStyle = BorderStyle.None
            };
            pnlBar.Controls.Add(txtCode);

            var btnCode = CreateGradientButton("🎁 รับของ", new Font("Segoe UI", 10, FontStyle.Bold),
                100, 36, 505, 12, GREEN, GREEN_LIGHT, Color.Black);
            btnCode.Click += (s, e) => RedeemCodeAction(txtCode.Text.Trim());
            pnlBar.Controls.Add(btnCode);

            // Start Game button (big green gradient)
            var btnSG = CreateGradientButton("⚔️ เริ่มเกม!", new Font("Segoe UI", 18, FontStyle.Bold),
                280, 50, 340, 605, GREEN, GREEN_LIGHT, Color.Black);
            btnSG.Click += (s, e) => StartGame();
            pnlMenu.Controls.Add(btnSG);
        }

        // ================================================================
        // SCREEN: GAME
        // ================================================================
        private void InitGameScreen()
        {
            // Header
            Panel pnlHdr = new Panel { Size = new Size(920, 50), Location = new Point(20, 10), BackColor = SafeTransparentBackground() };
            pnlHdr.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 920, 50), 10,
                    Color.FromArgb(20, 255, 255, 255), INPUT_BORDER);
            };
            pnlGame.Controls.Add(pnlHdr);

            // Left buttons (back, shop, settings)
            var btnBM2 = new Button
            {
                Text = "⬅️ กลับ", Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = SafeTransparentBackground(), FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(70, 32), Location = new Point(8, 9),
                Cursor = Cursors.Hand, ForeColor = Color.White
            };
            btnBM2.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
                    e.Graphics.FillPath(b, RoundedRect(new Rectangle(0, 0, 70, 32), 8));
                TextRenderer.DrawText(e.Graphics, "⬅️ กลับ", new Font("Segoe UI", 9, FontStyle.Bold),
                    new Rectangle(0, 0, 70, 32), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnBM2.Click += (s, e) => BackToMenu();
            pnlHdr.Controls.Add(btnBM2);

            var btnShopG = CreateGradientButton("🏪 ร้านค้า", new Font("Segoe UI", 9, FontStyle.Bold),
                80, 32, 85, 9, ORANGE, PINK, Color.White);
            btnShopG.Click += (s, e) => ToggleShop();
            pnlHdr.Controls.Add(btnShopG);

            var btnSet = CreateGradientButton("⚙️", new Font("Segoe UI", 9, FontStyle.Bold),
                40, 32, 172, 9, PURPLE, PURPLE, Color.White);
            btnSet.Click += (s, e) => OpenSettings();
            pnlHdr.Controls.Add(btnSet);

            // Game info (right side)
            lblLives = AddLabel(pnlHdr, "❤️ 20", new Font("Segoe UI", 11, FontStyle.Bold),
                PINK, 70, 25, 540, 12, ContentAlignment.MiddleLeft);
            lblGold = AddLabel(pnlHdr, "💰 300", new Font("Segoe UI", 11, FontStyle.Bold),
                ORANGE, 80, 25, 610, 12, ContentAlignment.MiddleLeft);
            lblKills = AddLabel(pnlHdr, "💀 0", new Font("Segoe UI", 11, FontStyle.Bold),
                GREEN, 60, 25, 690, 12, ContentAlignment.MiddleLeft);
            lblWave = AddLabel(pnlHdr, "🌊 0", new Font("Segoe UI", 11, FontStyle.Bold),
                Color.FromArgb(102, 102, 204), 60, 25, 760, 12, ContentAlignment.MiddleLeft);

            // Map
            pbMap = new PictureBox
            {
                Size = new Size(920, 460), Location = new Point(20, 68),
                BackColor = MAP_BG, BorderStyle = BorderStyle.None
            };
            pbMap.Paint += PbMap_Paint;
            pbMap.MouseClick += PbMap_MouseClick;
            pnlGame.Controls.Add(pbMap);

            // Controls bar
            Panel pnlCtrl = new Panel { Size = new Size(920, 100), Location = new Point(20, 536), BackColor = SafeTransparentBackground() };
            pnlCtrl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 920, 100), 10,
                    Color.FromArgb(20, 255, 255, 255), INPUT_BORDER);
            };
            pnlGame.Controls.Add(pnlCtrl);

            AddLabel(pnlCtrl, "📍 เลือกป้อม:", new Font("Segoe UI", 9, FontStyle.Bold),
                Color.FromArgb(180, 180, 220), 100, 18, 10, 6, ContentAlignment.MiddleLeft);

            flpSelectedCharsInGame = new FlowLayoutPanel
            {
                Size = new Size(420, 70), Location = new Point(8, 26),
                BackColor = SafeTransparentBackground()
            };
            pnlCtrl.Controls.Add(flpSelectedCharsInGame);

            // Wave control section
            lblWaveStatus = AddLabel(pnlCtrl, "🌊 กด \"ส่งคลื่นถัดไป\" เพื่อเริ่ม!",
                new Font("Segoe UI", 10, FontStyle.Italic),
                Color.FromArgb(170, 170, 170), 460, 18, 450, 6, ContentAlignment.MiddleLeft);

            var btnNW = CreateGradientButton("🚀 ส่งคลื่นถัดไป", new Font("Segoe UI", 10, FontStyle.Bold),
                150, 36, 452, 28, PINK_LIGHT, PINK, Color.White);
            btnNW.Click += (s, e) => StartNextWave();
            pnlCtrl.Controls.Add(btnNW);

            // Auto-wave checkbox
            CheckBox cbAW = new CheckBox
            {
                Text = "อัตโนมัติ", Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TEXT_MAIN, Location = new Point(460, 72),
                Size = new Size(90, 20), BackColor = SafeTransparentBackground()
            };
            cbAW.CheckedChanged += (s, e) => { autoWave = cbAW.Checked; };
            pnlCtrl.Controls.Add(cbAW);

            // Endless claim button
            var btnEndless = CreateGradientButton("🏆 รับรางวัลและออก", new Font("Segoe UI", 9, FontStyle.Bold),
                150, 28, 620, 28, ORANGE, PINK, Color.White);
            btnEndless.Visible = false;
            btnEndless.Click += (s, e) => { /* endless mode claim */ };
            pnlCtrl.Controls.Add(btnEndless);

            // Shop overlay
            InitShopPanel(pnlGame);
            // Settings overlay
            InitSettingsPanel(pnlGame);
            // Game over overlay
            InitGameOverPanel(pnlGame);
        }

        // ================================================================
        // SUB-PANEL: SHOP
        // ================================================================
        private void InitShopPanel(Panel parent)
        {
            pnlShop = new Panel
            {
                Size = new Size(320, 500), Location = new Point(620, 120),
                BackColor = SafeTransparentBackground(), Visible = false
            };
            pnlShop.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 320, 500), 14,
                    Color.FromArgb(247, 15, 12, 41), Color.FromArgb(26, 255, 255, 255));
                DrawGlow(e.Graphics, 160, 0, 150, PURPLE_SHADOW, 15);
            };
            parent.Controls.Add(pnlShop);
            pnlShop.BringToFront();

            AddLabel(pnlShop, "🏪 ร้านค้า", new Font("Segoe UI", 16, FontStyle.Bold),
                Color.White, 200, 28, 60, 15, ContentAlignment.MiddleCenter);

            var btnCS = new Button
            {
                Text = "✕", Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = SafeTransparentBackground(),
                FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 },
                Size = new Size(30, 28), Location = new Point(280, 15),
                Cursor = Cursors.Hand, ForeColor = Color.White
            };
            btnCS.Click += (s, e) => ToggleShop();
            pnlShop.Controls.Add(btnCS);

            flpShopItems = new FlowLayoutPanel
            {
                Size = new Size(290, 430), Location = new Point(15, 50),
                BackColor = SafeTransparentBackground()
            };
            pnlShop.Controls.Add(flpShopItems);
            RenderShopItems();
        }

        // ================================================================
        // SUB-PANEL: SETTINGS
        // ================================================================
        private void InitSettingsPanel(Panel parent)
        {
            pnlSettings = new Panel { Size = new Size(400, 310), Location = new Point(280, 200), BackColor = SafeTransparentBackground(), Visible = false };
            pnlSettings.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 400, 310), 16,
                    Color.FromArgb(247, 20, 15, 42), Color.FromArgb(40, PURPLE));
                DrawGlow(e.Graphics, 200, 0, 180, PURPLE, 15);
            };
            parent.Controls.Add(pnlSettings);
            pnlSettings.BringToFront();

            AddLabel(pnlSettings, "⚙️ ตั้งค่า", new Font("Segoe UI", 20, FontStyle.Bold),
                TEXT_MAIN, 200, 30, 100, 15, ContentAlignment.MiddleCenter);

            AddLabel(pnlSettings, "⏱ ความเร็ว:", new Font("Segoe UI", 11, FontStyle.Bold),
                TEXT_MAIN, 100, 22, 25, 65, ContentAlignment.MiddleLeft);
            ComboBox cbSp = new ComboBox
            {
                Location = new Point(150, 62), Size = new Size(200, 28),
                Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(25, 22, 55), ForeColor = TEXT_MAIN, FlatStyle = FlatStyle.Flat
            };
            cbSp.Items.AddRange(new object[] { "1x ปกติ", "2x เร็ว", "3x เร็วมาก" });
            cbSp.SelectedIndex = 0;
            cbSp.SelectedIndexChanged += (s, e) => { gameSpeed = cbSp.SelectedIndex + 1; };
            pnlSettings.Controls.Add(cbSp);

            AddLabel(pnlSettings, "💀 ความยาก:", new Font("Segoe UI", 11, FontStyle.Bold),
                TEXT_MAIN, 100, 22, 25, 110, ContentAlignment.MiddleLeft);
            ComboBox cbDf = new ComboBox
            {
                Location = new Point(150, 107), Size = new Size(200, 28),
                Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(25, 22, 55), ForeColor = TEXT_MAIN, FlatStyle = FlatStyle.Flat
            };
            cbDf.Items.AddRange(new object[] { "ง่าย", "ปกติ", "ยาก" });
            cbDf.SelectedIndex = 1;
            cbDf.SelectedIndexChanged += (s, e) => { difficulty = cbDf.SelectedIndex + 1; };
            pnlSettings.Controls.Add(cbDf);

            var btnCS2 = CreateGradientButton("✅ ตกลง", new Font("Segoe UI", 12, FontStyle.Bold),
                160, 42, 120, 190, PINK_LIGHT, PINK, Color.White);
            btnCS2.Click += (s, e) => { pnlSettings.Visible = false; SaveCurrentData(); };
            pnlSettings.Controls.Add(btnCS2);
        }

        // ================================================================
        // SUB-PANEL: GAME OVER
        // ================================================================
        private void InitGameOverPanel(Panel parent)
        {
            pnlGameOver = new Panel
            {
                Size = new Size(480, 340), Location = new Point(240, 180),
                BackColor = SafeTransparentBackground(), Visible = false
            };
            pnlGameOver.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawCard(e.Graphics, new Rectangle(0, 0, 480, 340), 18,
                    Color.FromArgb(247, 26, 26, 46), Color.FromArgb(26, 255, 255, 255));
                DrawGlow(e.Graphics, 240, 170, 150, PINK, 15);
            };
            parent.Controls.Add(pnlGameOver);
            pnlGameOver.BringToFront();
        }

        // ================================================================
        // RENDERERS
        // ================================================================
        private void RenderModeGrid()
        {
            flpModeGrid.Controls.Clear();
            foreach (var mode in GAME_MODES)
            {
                bool sel = selectedMode == mode.Id;
                var card = new Panel { Size = new Size(170, 100), BackColor = SafeTransparentBackground(), Cursor = Cursors.Hand, Margin = new Padding(8) };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Color fill = sel ? Color.FromArgb(51, 245, 87, 108) : Color.FromArgb(20, 255, 255, 255);
                    Color border = sel ? PINK : INPUT_BORDER;
                    DrawCard(e.Graphics, new Rectangle(0, 0, 170, 100), 12, fill, border);
                    if (sel) DrawGlow(e.Graphics, 85, 50, 40, PINK, 20);
                };
                AddLabel(card, mode.Icon, new Font("Segoe UI Emoji", 28),
                    Color.White, 160, 40, 5, 5, ContentAlignment.MiddleCenter, false);
                AddLabel(card, mode.Name, new Font("Segoe UI", 12, FontStyle.Bold),
                    sel ? PINK : TEXT_MAIN, 160, 22, 5, 46, ContentAlignment.MiddleCenter, false);
                AddLabel(card, mode.Desc, new Font("Segoe UI", 8),
                    TEXT_DIM, 160, 28, 5, 70, ContentAlignment.MiddleCenter, false);
                card.Click += (s, e) => { selectedMode = mode.Id; RenderModeGrid(); };
                flpModeGrid.Controls.Add(card);
            }
        }

        private void RenderCharGrid()
        {
            flpCharGrid.Controls.Clear();
            for (int i = 0; i < CHARACTERS.Count; i++)
            {
                int idx = i; var ch = CHARACTERS[i]; bool sel = selectedChars.Contains(idx);
                var card = new Panel { Size = new Size(130, 140), BackColor = SafeTransparentBackground(), Cursor = Cursors.Hand, Margin = new Padding(5) };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Color fill = sel ? Color.FromArgb(51, 67, 233, 123) : Color.FromArgb(20, 255, 255, 255);
                    Color border = sel ? GREEN : INPUT_BORDER;
                    DrawCard(e.Graphics, new Rectangle(0, 0, 130, 140), 10, fill, border);
                    if (sel) DrawGlow(e.Graphics, 65, 70, 35, GREEN, 20);
                };
                AddLabel(card, ch.Emoji, new Font("Segoe UI Emoji", 30),
                    Color.White, 120, 48, 5, 4, ContentAlignment.MiddleCenter, false);
                AddLabel(card, ch.Name, new Font("Segoe UI", 9, FontStyle.Bold),
                    sel ? GREEN : TEXT_MAIN, 120, 18, 5, 52, ContentAlignment.MiddleCenter, false);
                AddLabel(card, $"⚔️{ch.Dmg}  👁️{ch.Range}", new Font("Segoe UI", 8),
                    TEXT_DIM, 120, 16, 5, 72, ContentAlignment.MiddleCenter, false);
                AddLabel(card, $"💰 {ch.Cost}", new Font("Segoe UI", 9, FontStyle.Bold),
                    ORANGE, 120, 18, 5, 90, ContentAlignment.MiddleCenter, false);
                AddLabel(card, $"⚡ {ch.Speed:F1}s", new Font("Segoe UI", 7),
                    Color.FromArgb(130, 130, 180), 120, 14, 5, 110, ContentAlignment.MiddleCenter, false);
                card.Click += (s, e) =>
                {
                    if (sel) selectedChars.Remove(idx);
                    else { if (selectedChars.Count >= 3) { MessageBox.Show("เลือกได้สูงสุด 3 ตัว!", "จำกัด", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } selectedChars.Add(idx); }
                    RenderCharGrid();
                };
                flpCharGrid.Controls.Add(card);
            }
        }

        private void RenderSelectedCharsInGame()
        {
            flpSelectedCharsInGame.Controls.Clear();
            foreach (int idx in selectedChars)
            {
                var ch = CHARACTERS[idx]; bool placing = activePlacingCharIndex == idx;
                var btn = new Panel { Size = new Size(120, 62), BackColor = SafeTransparentBackground(), Cursor = Cursors.Hand, Margin = new Padding(3) };
                btn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Color fill = placing ? Color.FromArgb(51, 245, 87, 108) : Color.FromArgb(15, 255, 255, 255);
                    Color border = placing ? PINK : INPUT_BORDER;
                    DrawCard(e.Graphics, new Rectangle(0, 0, 120, 62), 8, fill, border);
                    if (placing) DrawGlow(e.Graphics, 60, 31, 25, PINK, 25);
                };
                AddLabel(btn, ch.Emoji, new Font("Segoe UI Emoji", 16),
                    Color.White, 30, 28, 3, 2, ContentAlignment.MiddleCenter, false);
                AddLabel(btn, ch.Name, new Font("Segoe UI", 8, FontStyle.Bold),
                    placing ? PINK : TEXT_MAIN, 80, 16, 34, 2, ContentAlignment.MiddleLeft, false);
                AddLabel(btn, $"💰 {ch.Cost}", new Font("Segoe UI", 8, FontStyle.Bold),
                    ORANGE, 80, 14, 34, 18, ContentAlignment.MiddleLeft, false);
                AddLabel(btn, $"⚔️{ch.Dmg}", new Font("Segoe UI", 7),
                    TEXT_DIM, 50, 14, 3, 36, ContentAlignment.MiddleLeft, false);
                AddLabel(btn, $"👁️{ch.Range}", new Font("Segoe UI", 7),
                    TEXT_DIM, 50, 14, 50, 36, ContentAlignment.MiddleLeft, false);
                btn.Click += (s, e) =>
                {
                    if (gold < ch.Cost) { lblWaveStatus.Text = $"❌ ต้องการ {ch.Cost} ทอง"; return; }
                    activePlacingCharIndex = placing ? -1 : idx;
                    RenderSelectedCharsInGame();
                };
                flpSelectedCharsInGame.Controls.Add(btn);
            }
        }

        private void RenderShopItems()
        {
            flpShopItems.Controls.Clear();
            foreach (var item in SHOP_ITEMS)
            {
                bool bought = purchasedItems.Contains(item.Id);
                var card = new Panel { Size = new Size(270, 80), BackColor = SafeTransparentBackground(), Margin = new Padding(4), Cursor = Cursors.Hand };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    DrawCard(e.Graphics, new Rectangle(0, 0, 270, 80), 8,
                        Color.FromArgb(15, 255, 255, 255), Color.FromArgb(26, bought ? Color.Gray : ORANGE));
                };
                AddLabel(card, item.Name, new Font("Segoe UI", 10, FontStyle.Bold),
                    bought ? Color.Gray : TEXT_MAIN, 180, 20, 8, 6, ContentAlignment.MiddleLeft);
                AddLabel(card, item.Desc, new Font("Segoe UI", 8),
                    Color.FromArgb(130, 130, 180), 180, 40, 8, 28, ContentAlignment.MiddleLeft);

                var bt = new Button
                {
                    Text = bought ? "✅" : $"💰{item.Price}",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = SafeTransparentBackground(), FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    Size = new Size(70, 36), Location = new Point(192, 22),
                    Cursor = Cursors.Hand,
                    ForeColor = bought ? Color.Gray : Color.Black,
                    Enabled = !bought
                };
                bt.Paint += (s, e) =>
                {
                    if (bt.Enabled)
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var b = new LinearGradientBrush(new Rectangle(0, 0, 70, 36), ORANGE, PINK, 135f))
                            e.Graphics.FillPath(b, RoundedRect(new Rectangle(0, 0, 70, 36), 8));
                    }
                    TextRenderer.DrawText(e.Graphics, bt.Text, new Font("Segoe UI", 10, FontStyle.Bold),
                        new Rectangle(0, 0, 70, 36),
                        bt.Enabled ? Color.Black : Color.Gray,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
                bt.Click += (s, e) => BuyShopItem(item);
                card.Controls.Add(bt);
                flpShopItems.Controls.Add(card);
            }
        }

        // ================================================================
        // UI HELPERS
        // ================================================================
        private Label AddLabel(Control parent, string text, Font font, Color color,
            int w, int h, int x, int y, ContentAlignment align, bool enabled = true)
        {
            var lbl = new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                TextAlign = align,
                Size = new Size(w, h),
                Location = new Point(x, y),
                BackColor = SafeTransparentBackground(),
                Enabled = enabled
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private Button CreateGradientButton(string text, Font font, int w, int h,
            int x, int y, Color c1, Color c2, Color foreColor)
        {
            var btn = new Button
            {
                Text = text,
                Font = font,
                BackColor = SafeTransparentBackground(),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(w, h),
                Location = new Point(x, y),
                Cursor = Cursors.Hand,
                ForeColor = foreColor
            };
            btn.Paint += (s, e) => DrawGradientButton(e, new Rectangle(0, 0, w, h), 8,
                c1, c2, 135f, text, font, foreColor);
            return btn;
        }

        // ================================================================
        // NAVIGATION
        // ================================================================
        private void SetActiveScreen(Control activeScreen)
        {
            if (activeScreen == null) return;
            foreach (Control c in pnlContainerMain.Controls)
            {
                c.Visible = c == activeScreen;
                if (c.Visible)
                    c.BringToFront();
            }
            pnlContainerMain.Refresh();
            activeScreen.BringToFront();
        }

        private void ShowLogin() { SetActiveScreen(pnlLogin); }
        private void ShowMenu() { SetActiveScreen(pnlMenu); }

        private void DoLogin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) { MessageBox.Show("❌ กรุณากรอกชื่อผู้เล่น", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(password)) { MessageBox.Show("❌ กรุณากรอกรหัสผ่าน", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!saves.ContainsKey(machineId)) { MessageBox.Show("❌ ไม่พบบัญชีนี้ กรุณาสมัครก่อน", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            var save = saves[machineId];
            if (string.IsNullOrEmpty(save.Password))
            {
                // Old save without password - allow login and set password
                save.Password = password.Trim();
                SaveSaves();
            }
            else if (save.Password != password.Trim())
            {
                MessageBox.Show("❌ รหัสผ่านไม่ถูกต้อง", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            currentSave = save; LoadSaveData(); ShowMenu();
        }

        private void DoRegister(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) { MessageBox.Show("❌ กรุณากรอกชื่อผู้เล่น", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(password) || password.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(password, @"^\d{4}$"))
            { MessageBox.Show("❌ รหัสผ่านต้องเป็นตัวเลข 4 หลักเท่านั้น", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (saves.ContainsKey(machineId)) { MessageBox.Show("❌ มีบัญชีอยู่แล้วบนเครื่องนี้ กรุณาเข้าสู่ระบบ", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            currentSave = new SaveData { Username = username.Trim(), Password = password.Trim(), Tokens = 150, Lives = 20, MaxLives = 20, Gold = 300, OwnedCharIds = new List<int> { 0, 1, 2, 3 }, SelectedCharIds = new List<int> { 0, 1, 2 } };
            saves[machineId] = currentSave; SaveSaves(); LoadSaveData(); ShowMenu();
        }
        private void ClearMachineData()
        {
            if (MessageBox.Show("⚠️ ล้างข้อมูลเครื่องนี้? (กู้คืนได้ถ้ามีรหัส)", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { if (saves.ContainsKey(machineId)) { saves.Remove(machineId); SaveSaves(); } currentSave = null; tokens = 150; lives = 20; maxLives = 20; gold = 300; ShowLogin(); }
        }

        // ================================================================
        // EXPORT / RESTORE
        // ================================================================
        private string GenRestoreText() { if (currentSave == null) return ""; return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { machine = machineId, save = currentSave }))); }
        private void DoExport()
        {
            if (currentSave == null) { MessageBox.Show("⚠️ ยังไม่ได้เข้าสู่ระบบ", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            // Generate 4-char local restore code (same as HTML)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Random rnd = new Random();
            char[] code = new char[4];
            for (int i = 0; i < 4; i++) code[i] = chars[rnd.Next(chars.Length)];
            string restoreCode = new string(code);
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EmojiTD");
                Directory.CreateDirectory(appData);
                string restoreFile = Path.Combine(appData, "td_restore_4.json");
                Dictionary<string, object> db;
                if (File.Exists(restoreFile))
                {
                    string existing = File.ReadAllText(restoreFile);
                    db = JsonSerializer.Deserialize<Dictionary<string, object>>(existing) ?? new Dictionary<string, object>();
                }
                else db = new Dictionary<string, object>();
                db[restoreCode] = new { email = currentSave.Username, data = currentSave, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                File.WriteAllText(restoreFile, JsonSerializer.Serialize(db));
                Clipboard.SetText(restoreCode);
                MessageBox.Show($"📋 รหัสกู้คืนของคุณ: {restoreCode}\n\n✅ คัดลอกไปยังคลิปบอร์ดแล้ว!\n\nไปที่เครื่องอื่น → เปิดเกม → วางรหัส 4 ตัวในช่องกู้คืน → กดนำเข้า\n\n⚠️ รหัสมีอายุ 7 วัน", "📤 ส่งออก", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"❌ เกิดข้อผิดพลาด: {ex.Message}", "", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void DoImport(string enc)
        {
            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(enc.Trim()));
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (data == null || !data.ContainsKey("save")) { MessageBox.Show("❌ ข้อมูลไม่ถูกต้อง", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                var save = JsonSerializer.Deserialize<SaveData>(data["save"].GetRawText());
                if (save == null) { MessageBox.Show("❌ ข้อมูลเสียหาย", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                currentSave = save; saves[machineId] = save; SaveSaves(); LoadSaveData(); ShowMenu();
                MessageBox.Show($"✅ กู้คืน! ยินดีต้อนรับ {save.Username}", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { MessageBox.Show("❌ ข้อมูลกู้คืนไม่ถูกต้อง", "", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Restore from local 4-char code file (td_restore_4.json)
        private void DoRestoreLocal(string code)
        {
            code = code.Trim().ToUpper();
            if (code.Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z2-9]+$"))
            { MessageBox.Show("❌ รหัสกู้คืนต้องเป็น 4 ตัวอักษร (A-Z, 2-9)", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EmojiTD");
                string restoreFile = Path.Combine(appData, "td_restore_4.json");
                if (!File.Exists(restoreFile)) { MessageBox.Show("❌ ไม่พบรหัสกู้คืนนี้", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                string json = File.ReadAllText(restoreFile);
                var db = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (db == null || !db.ContainsKey(code)) { MessageBox.Show("❌ รหัสกู้คืนไม่ถูกต้องหรือหมดอายุแล้ว", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                var data = JsonSerializer.Deserialize<SaveData>(db[code].GetProperty("data").GetRawText());
                if (data != null)
                {
                    currentSave = data; saves[machineId] = data; SaveSaves(); LoadSaveData(); ShowMenu();
                    db.Remove(code); File.WriteAllText(restoreFile, JsonSerializer.Serialize(db));
                    MessageBox.Show($"✅ กู้คืนสำเร็จ! ยินดีต้อนรับ {data.Username}", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { MessageBox.Show("❌ ผิดพลาดในการกู้คืน", "", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ================================================================
        // GAME ACTIONS
        // ================================================================
        private void SummonCharacter()
        {
            if (tokens < 50) { MessageBox.Show("ต้องการ 50 โทเค่น!", "ไม่พอ", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            tokens -= 50; lblTokens.Text = $"👍 {tokens} โทเค่น";
            var pool = new List<Character> {
                new Character { Emoji = "🐼", Name = "แพนด้าอ้วน", Cost = 220, Dmg = 40, Range = 110, Speed = 1.2, Desc = "โจมตีหนักหน่วง" },
                new Character { Emoji = "🦉", Name = "นกฮูกตาโต", Cost = 200, Dmg = 25, Range = 160, Speed = 0.8, Desc = "โจมตีรวดเร็ว" },
                new Character { Emoji = "🐯", Name = "เสือโคร่ง", Cost = 400, Dmg = 80, Range = 130, Speed = 1.4, Desc = "พลังโจมตีรุนแรง" },
                new Character { Emoji = "🦄", Name = "ยูนิคอร์น", Cost = 450, Dmg = 60, Range = 180, Speed = 0.9, Desc = "โจมตีเวทมนตร์" }
            };
            var s = pool[new Random().Next(pool.Count)]; CHARACTERS.Add(s); RenderCharGrid(); SaveCurrentData();
            MessageBox.Show($"🎉 ได้รับ {s.Emoji} {s.Name}!", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RedeemCodeAction(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            if (REDEEM_CODES.TryGetValue(code.Trim(), out var r))
            { gold += r.Gold; tokens += r.Token; lives += r.Lives; maxLives = Math.Max(maxLives, lives); lblTokens.Text = $"👍 {tokens} โทเค่น"; MessageBox.Show(r.Msg, "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information); SaveCurrentData(); }
            else MessageBox.Show("❌ รหัสไม่ถูกต้อง", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void StartGame()
        {
            if (selectedChars.Count == 0) { MessageBox.Show("เลือกตัวละครอย่างน้อย 1 ตัว!", "คำแนะนำ", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            pnlMenu.Visible = false; pnlGame.Visible = true;
            lives = selectedMode == "easy" ? 25 : (selectedMode == "hard" ? 15 : 20); maxLives = lives;
            gold = selectedMode == "easy" ? 400 : (selectedMode == "hard" ? 250 : 300);
            kills = 0; wave = 1; isPlaying = true; isGameOver = false; isSpawningWave = false;
            placedTowers.Clear(); enemies.Clear(); projectiles.Clear(); purchasedItems.Clear();
            damageMultiplier = 1.0; speedMultiplier = 1.0; RenderShopItems(); UpdateGameUI(); RenderSelectedCharsInGame(); gameLoopTimer.Start();
        }

        private void BackToMenu() { gameLoopTimer.Stop(); isPlaying = false; pnlGame.Visible = false; pnlMenu.Visible = true; lblTokens.Text = $"👍 {tokens} โทเค่น"; RenderCharGrid(); SaveCurrentData(); }
        private void UpdateGameUI() { lblLives.Text = $"❤️ {lives}"; lblGold.Text = $"💰 {gold}"; lblKills.Text = $"💀 {kills}"; lblWave.Text = $"🌊 {wave}"; }
        private void ToggleShop() { pnlShop.Visible = !pnlShop.Visible; }
        private void OpenSettings() { pnlSettings.Visible = !pnlSettings.Visible; }

        private void BuyShopItem(ShopItem item)
        {
            if (gold < item.Price) { lblWaveStatus.Text = $"❌ ต้องการ {item.Price} ทอง"; return; }
            gold -= item.Price; purchasedItems.Add(item.Id); item.Effect(); UpdateGameUI(); RenderShopItems();
            lblWaveStatus.Text = $"✅ ซื้อ {item.Name} สำเร็จ!";
        }

        private void StartNextWave()
        {
            if (isSpawningWave) return;
            if (enemies.Exists(en => en.Alive)) { lblWaveStatus.Text = "⚠️ ยังมีศัตรู!"; return; }
            isSpawningWave = true; waveSpawnCount = 0; waveSpawnMax = 5 + wave * 3; waveSpawnCooldown = 0;
            lblWaveStatus.Text = $"⚔️ คลื่น {wave}!";
        }

        private void GameLoopTimer_Tick(object sender, EventArgs e)
        {
            if (!isPlaying || isGameOver) return;
            for (int step = 0; step < gameSpeed; step++) UpdateGameLogic();
            pbMap.Invalidate();
        }

        private void UpdateGameLogic()
        {
            if (isSpawningWave)
            {
                waveSpawnCooldown--;
                if (waveSpawnCooldown <= 0 && waveSpawnCount < waveSpawnMax)
                { SpawnEnemy(); waveSpawnCount++; waveSpawnCooldown = 25; }
                else if (waveSpawnCount >= waveSpawnMax && !enemies.Exists(en => en.Alive))
                { isSpawningWave = false; gold += 100 + wave * 10; wave++; UpdateGameUI(); lblWaveStatus.Text = $"✅ คลื่น {wave - 1} ผ่าน!"; if (autoWave) StartNextWave(); }
            }

            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;
                if (enemy.SlowTimer > 0) enemy.SlowTimer--;
                double spd = enemy.SlowTimer > 0 ? enemy.Speed * 0.5 : enemy.Speed;
                if (enemy.PathIndex < mapData.Path.Count - 1)
                {
                    var t = mapData.Path[enemy.PathIndex + 1];
                    double dx = t.X - enemy.X, dy = t.Y - enemy.Y, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < spd) { enemy.X = t.X; enemy.Y = t.Y; enemy.PathIndex++; }
                    else { enemy.X += (dx / d) * spd; enemy.Y += (dy / d) * spd; }
                }
                else
                {
                    enemy.Alive = false; lives -= enemy.IsBoss ? 5 : 1; UpdateGameUI();
                    if (lives <= 0) { TriggerGameOver(false); return; }
                }
            }

            foreach (var tower in placedTowers)
            {
                var ch = CHARACTERS[tower.CharIndex];
                if (tower.Cooldown > 0) { tower.Cooldown -= 0.03; continue; }
                Enemy target = null; double best = -1;
                foreach (var enemy in enemies)
                {
                    if (!enemy.Alive) continue;
                    double dx = enemy.X - (tower.TileX * 40 + 20), dy = enemy.Y - (tower.TileY * 40 + 20);
                    if (Math.Sqrt(dx * dx + dy * dy) <= ch.Range)
                    {
                        double p = enemy.PathIndex * 1000 + (enemy.X + enemy.Y);
                        if (p > best) { best = p; target = enemy; }
                    }
                }
                if (target != null)
                {
                    projectiles.Add(new Projectile { X = tower.TileX * 40 + 20, Y = tower.TileY * 40 + 20, Target = target, Emoji = ch.Emoji });
                    int dmg = ch.Dmg + (tower.Level - 1) * (ch.Dmg / 2); dmg = (int)(dmg * damageMultiplier);
                    target.Hp -= dmg;
                    if (ch.Emoji == "🐻") target.SlowTimer = 60;
                    if (target.Hp <= 0 && target.Alive) { target.Alive = false; gold += target.Reward; kills++; UpdateGameUI(); }
                    tower.Cooldown = ch.Speed * speedMultiplier;
                }
            }

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];
                if (!p.Active || !p.Target.Alive) { projectiles.RemoveAt(i); continue; }
                double dx = p.Target.X - p.X, dy = p.Target.Y - p.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < 12) projectiles.RemoveAt(i);
                else { p.X += (dx / Math.Sqrt(dx * dx + dy * dy)) * 12; p.Y += (dy / Math.Sqrt(dx * dx + dy * dy)) * 12; }
            }
        }

        private void SpawnEnemy()
        {
            Random rand = new Random();
            int maxIdx = Math.Min(wave / 2, ENEMY_TYPES.Count - 1);
            var type = ENEMY_TYPES[rand.Next(0, maxIdx + 1)];
            bool boss = wave % 5 == 0 && waveSpawnCount == 0;
            int hp = boss ? type.Hp * 5 : type.Hp;
            double spd = boss ? type.Speed * 0.8 : type.Speed;
            if (selectedMode == "easy") hp = (int)(hp * 0.8);
            else if (selectedMode == "hard") hp = (int)(hp * 1.3);
            enemies.Add(new Enemy { Type = type, Hp = hp, MaxHp = hp, Speed = spd, Reward = type.Reward * (boss ? 5 : 1), Alive = true, PathIndex = 0, X = mapData.Start.X, Y = mapData.Start.Y, Emoji = boss ? "👹" : type.Emoji, SlowTimer = 0, IsBoss = boss });
        }

        private void TriggerGameOver(bool won)
        {
            isGameOver = true; gameLoopTimer.Stop();
            pnlGameOver.Controls.Clear();
            pnlGameOver.Visible = true;

            AddLabel(pnlGameOver, won ? "🎉 ชนะแล้ว! 🎉" : "💀 แพ้แล้ว!",
                new Font("Segoe UI", 30, FontStyle.Bold),
                won ? GREEN : PINK, 460, 60, 10, 30, ContentAlignment.MiddleCenter);
            int reward = won ? (wave * 10) : (wave * 3);
            tokens += reward; SaveCurrentData();

            AddLabel(pnlGameOver, $"📊 สถิติ\n\n🏆 คลื่น: {wave}\n💀 ฆ่า: {kills}",
                new Font("Segoe UI", 13, FontStyle.Bold),
                TEXT_MAIN, 460, 100, 10, 110, ContentAlignment.MiddleCenter);
            if (won) AddLabel(pnlGameOver, $"👍 ได้รับ {reward} โทเค่น",
                new Font("Segoe UI", 11, FontStyle.Bold),
                ORANGE, 460, 25, 10, 210, ContentAlignment.MiddleCenter);

            var restart = CreateGradientButton("🔄 เล่นอีกครั้ง", new Font("Segoe UI", 14, FontStyle.Bold),
                200, 48, 140, 255, GREEN, GREEN_LIGHT, Color.Black);
            restart.Click += (s, e) => { pnlGameOver.Visible = false; BackToMenu(); };
            pnlGameOver.Controls.Add(restart);
        }

        // ================================================================
        // MAP PAINT
        // ================================================================
        private void PbMap_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            // Grid tiles
            using (var gp = new Pen(Color.FromArgb(20, 255, 255, 255), 1))
                for (int x = 0; x < 1000; x += 40)
                    for (int y = 0; y < 500; y += 40)
                        g.DrawRectangle(gp, x, y, 40, 40);

            // Path rendering
            if (mapData.Path.Count > 1)
            {
                using (var glow = new Pen(Color.FromArgb(50, PURPLE), 32) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                using (var outer = new Pen(Color.FromArgb(100, 180, 50, 255), 20) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                using (var inner = new Pen(Color.FromArgb(200, 240, 147, 251), 5) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    for (int i = 0; i < mapData.Path.Count - 1; i++)
                    {
                        g.DrawLine(glow, mapData.Path[i].X, mapData.Path[i].Y, mapData.Path[i + 1].X, mapData.Path[i + 1].Y);
                        g.DrawLine(outer, mapData.Path[i].X, mapData.Path[i].Y, mapData.Path[i + 1].X, mapData.Path[i + 1].Y);
                        g.DrawLine(inner, mapData.Path[i].X, mapData.Path[i].Y, mapData.Path[i + 1].X, mapData.Path[i + 1].Y);
                    }
                }
            }

            // Towers
            foreach (var tower in placedTowers)
            {
                var ch = CHARACTERS[tower.CharIndex];
                int px = tower.TileX * 40, py = tower.TileY * 40;
                if (selectedTowerForUpgrade == tower)
                {
                    DrawGlow(g, px + 20, py + 20, ch.Range, GREEN, 30);
                    using (var p = new Pen(Color.FromArgb(80, GREEN), 2))
                        g.DrawEllipse(p, px + 20 - ch.Range, py + 20 - ch.Range, ch.Range * 2, ch.Range * 2);
                }
                DrawGlow(g, px + 20, py + 20, 22, CYAN, 35);
                using (var b = new SolidBrush(Color.FromArgb(50, 35, 55)))
                    g.FillEllipse(b, px + 2, py + 2, 36, 36);
                using (var p = new Pen(CYAN, 2))
                    g.DrawEllipse(p, px + 2, py + 2, 36, 36);
                using (Font f = new Font("Segoe UI Emoji", 20))
                    g.DrawString(ch.Emoji, f, Brushes.White, px + 2, py);
                if (tower.Level > 1)
                {
                    using (var b = new SolidBrush(ORANGE))
                        g.FillEllipse(b, px + 24, py, 16, 16);
                    using (Font f = new Font("Segoe UI", 8, FontStyle.Bold))
                        g.DrawString(tower.Level.ToString(), f, Brushes.Black, px + 27, py + 1);
                }
            }

            // Enemies
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;
                int sz = enemy.IsBoss ? 38 : 26;
                if (enemy.IsBoss) DrawGlow(g, (int)enemy.X, (int)enemy.Y, 28, PINK, 55);
                using (Font f = new Font("Segoe UI Emoji", enemy.IsBoss ? 26 : 18))
                    g.DrawString(enemy.Emoji, f, Brushes.White, (int)enemy.X - sz / 2, (int)enemy.Y - sz / 2);
                int bw = enemy.IsBoss ? 44 : 28;
                using (var b = new SolidBrush(Color.FromArgb(60, 20, 20)))
                    g.FillRectangle(b, (int)enemy.X - bw / 2, (int)enemy.Y - sz / 2 - 10, bw, 5);
                using (var b = new SolidBrush(GREEN))
                    g.FillRectangle(b, (int)enemy.X - bw / 2, (int)enemy.Y - sz / 2 - 10,
                        (int)(bw * ((double)enemy.Hp / enemy.MaxHp)), 5);
                if (enemy.SlowTimer > 0)
                {
                    using (Font f = new Font("Segoe UI", 6))
                        g.DrawString("❄️", f, Brushes.LightBlue, (int)enemy.X - 4, (int)enemy.Y - sz / 2 - 16);
                }
            }

            // Projectiles
            foreach (var p in projectiles)
                if (p.Active)
                    using (Font f = new Font("Segoe UI Emoji", 10))
                        g.DrawString("💥", f, Brushes.White, (float)p.X - 8, (float)p.Y - 8);

            // Placing preview
            if (activePlacingCharIndex != -1)
            {
                Point mp = pbMap.PointToClient(Cursor.Position);
                int tx = mp.X / 40, ty = mp.Y / 40;
                if (tx >= 0 && tx < 25 && ty >= 0 && ty < 12)
                {
                    var ch = CHARACTERS[activePlacingCharIndex];
                    DrawGlow(g, tx * 40 + 20, ty * 40 + 20, ch.Range, PINK, 35);
                    using (var p = new Pen(Color.FromArgb(100, PINK), 2))
                        g.DrawEllipse(p, tx * 40 + 20 - ch.Range, ty * 40 + 20 - ch.Range, ch.Range * 2, ch.Range * 2);
                    using (Font f = new Font("Segoe UI Emoji", 18))
                        g.DrawString(ch.Emoji, f, Brushes.Gray, tx * 40 + 2, ty * 40 + 2);
                }
            }
        }

        private void PbMap_MouseClick(object sender, MouseEventArgs e)
        {
            int tx = e.X / 40, ty = e.Y / 40;
            if (activePlacingCharIndex != -1)
            {
                var ch = CHARACTERS[activePlacingCharIndex];
                if (placedTowers.Exists(t => t.TileX == tx && t.TileY == ty))
                { lblWaveStatus.Text = "⚠️ มีป้อมแล้ว!"; return; }
                if (IsTileOnPath(tx, ty))
                { lblWaveStatus.Text = "⚠️ วางบนเส้นทาง!"; return; }
                gold -= ch.Cost;
                placedTowers.Add(new PlacedTower { CharIndex = activePlacingCharIndex, TileX = tx, TileY = ty, Cooldown = 0, Level = 1 });
                activePlacingCharIndex = -1; UpdateGameUI(); RenderSelectedCharsInGame();
                lblWaveStatus.Text = $"✅ วาง {ch.Name}!";
                return;
            }
            var tw = placedTowers.Find(t => t.TileX == tx && t.TileY == ty);
            if (tw != null)
            {
                selectedTowerForUpgrade = tw;
                var ch = CHARACTERS[tw.CharIndex];
                int cost = tw.Level * 40;
                if (MessageBox.Show($"🏰 {ch.Name} (Lv.{tw.Level})\n⚔️ {ch.Dmg + (tw.Level - 1) * (ch.Dmg / 2)}\nอัปเกรด Lv.{tw.Level + 1}? (💰{cost})", "อัปเกรด", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (gold < cost) MessageBox.Show("ทองไม่พอ!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else { gold -= cost; tw.Level++; UpdateGameUI(); MessageBox.Show($"⚡ Lv.{tw.Level}!", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                }
                selectedTowerForUpgrade = null;
            }
        }

        private bool IsTileOnPath(int tx, int ty)
        {
            int cx = tx * 40 + 20, cy = ty * 40 + 20;
            for (int i = 0; i < mapData.Path.Count - 1; i++)
            {
                var p1 = mapData.Path[i]; var p2 = mapData.Path[i + 1];
                double dx = p2.X - p1.X, dy = p2.Y - p1.Y;
                if (dx == 0 && dy == 0)
                { dx = cx - p1.X; dy = cy - p1.Y; if (Math.Sqrt(dx * dx + dy * dy) < 25) return true; }
                else
                {
                    double t = ((cx - p1.X) * dx + (cy - p1.Y) * dy) / (dx * dx + dy * dy);
                    if (t < 0) { dx = cx - p1.X; dy = cy - p1.Y; }
                    else if (t > 1) { dx = cx - p2.X; dy = cy - p2.Y; }
                    else { dx = cx - (p1.X + t * dx); dy = cy - (p1.Y + t * dy); }
                    if (Math.Sqrt(dx * dx + dy * dy) < 25) return true;
                }
            }
            return false;
        }

        // ================================================================
        // RESTORE HTTP
        // ================================================================
        private void DoFetchFromServer(string shortCode)
        {
            string code = shortCode.Trim().ToUpper();
            try
            {
                if (code.Length != 6) { MessageBox.Show("❌ รหัสต้องเป็น 6 ตัว", "", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var http = HttpWebRequest.CreateHttp($"{RESTORE_SERVER}/load");
                http.Method = "POST"; http.ContentType = "application/json";
                byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { code }));
                http.ContentLength = body.Length;
                using (var s = http.GetRequestStream()) s.Write(body, 0, body.Length);
                using (var resp = http.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (result != null && result.ContainsKey("success") && result["success"].GetBoolean())
                    {
                        var data = JsonSerializer.Deserialize<SaveData>(result["data"].GetRawText());
                        if (data != null)
                        {
                            currentSave = data; saves[machineId] = data; SaveSaves(); LoadSaveData(); ShowMenu();
                            MessageBox.Show($"✅ กู้คืน! ยินดีต้อนรับ {data.Username}", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else MessageBox.Show($"❌ {result.GetValueOrDefault("error", new JsonElement()).GetString() ?? "ผิดพลาด"}", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (WebException) { TryLocalRestore(code); }
            catch (Exception ex) { MessageBox.Show($"❌ ไม่เชื่อมต่อ server: {ex.Message}\nรัน: node server.js", "", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void TryLocalRestore(string code)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EmojiTD");
                string restoreFile = Path.Combine(appData, "td_restore_6.json");
                if (!File.Exists(restoreFile)) { MessageBox.Show("❌ ไม่พบรหัส\n💡 รัน: node server.js", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                string json = File.ReadAllText(restoreFile);
                var db = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (db == null || !db.ContainsKey(code)) { MessageBox.Show("❌ รหัสไม่ถูกต้องหรือหมดอายุ", "", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                var data = JsonSerializer.Deserialize<SaveData>(db[code].GetProperty("data").GetRawText());
                if (data != null)
                {
                    currentSave = data; saves[machineId] = data; SaveSaves(); LoadSaveData(); ShowMenu();
                    db.Remove(code); File.WriteAllText(restoreFile, JsonSerializer.Serialize(db));
                    MessageBox.Show($"✅ กู้คืน! ยินดีต้อนรับ {data.Username}", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { MessageBox.Show("❌ ผิดพลาด", "", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }
}