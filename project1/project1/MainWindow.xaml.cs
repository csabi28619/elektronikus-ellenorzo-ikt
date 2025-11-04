using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json;
using System.Text;
using Microsoft.Win32;

namespace project1
{
    public partial class MainWindow : Window
    {
        private List<Tanulo> tanulok = new List<Tanulo>();
        private List<Tantargy> tantargyak = new List<Tantargy>();
        private List<TanuloTargyKapcsolat> kapcsolatok = new List<TanuloTargyKapcsolat>();
        private List<Jegy> jegyek = new List<Jegy>();

        private const string TanulokFile = "tanulok.json";
        private const string TargyakFile = "tantargyak.json";
        private const string KapcsolatokFile = "kapcsolatok.json";
        private const string JegyekFile = "jegyek.json";

        public MainWindow()
        {
            InitializeComponent();
            chkKollegista.Checked += (s, e) => cmbKollegium.IsEnabled = true;
            chkKollegista.Unchecked += (s, e) => cmbKollegium.IsEnabled = false;
            BetoltAdatok();
        }

        private void BetoltAdatok()
        {
            BetoltTanulok();
            BetoltTargyak();
            BetoltKapcsolatok();
            BetoltJegyek();
            FrissitTanuloCombo();
        }

        #region Tanulók kezelése

        private void BtnMentTanulo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNev.Text) || cmbSzak.SelectedItem == null ||
                    cmbOsztaly.SelectedItem == null || !dpBeiratkozas.SelectedDate.HasValue)
                {
                    MessageBox.Show("Kérlek töltsd ki az összes kötelező mezőt!", "Hiányos adatok",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var tanulo = new Tanulo
                {
                    Id = Guid.NewGuid().ToString(),
                    Nev = txtNev.Text,
                    SzuletesiHely = txtSzulHely.Text,
                    SzuletesiIdo = dpSzulIdo.SelectedDate ?? DateTime.Now,
                    AnyjaNeve = txtAnyja.Text,
                    Lakcim = txtLakcim.Text,
                    BeiratkozasIdeje = dpBeiratkozas.SelectedDate.Value,
                    Szak = (cmbSzak.SelectedItem as ComboBoxItem).Content.ToString(),
                    Osztaly = (cmbOsztaly.SelectedItem as ComboBoxItem).Content.ToString(),
                    Kollegista = chkKollegista.IsChecked ?? false,
                    Kollegium = chkKollegista.IsChecked == true && cmbKollegium.SelectedItem != null
                        ? (cmbKollegium.SelectedItem as ComboBoxItem).Content.ToString()
                        : ""
                };

                GeneralNaploSzamokat(tanulo);
                tanulok.Add(tanulo);
                MentTanulok();
                dgTanulok.ItemsSource = null;
                dgTanulok.ItemsSource = tanulok;
                FrissitTanuloCombo();
                UritTanuloForm();

                MessageBox.Show("Tanuló sikeresen mentve!", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneralNaploSzamokat(Tanulo tanulo)
        {
            var osztalyTanulok = tanulok.Where(t => t.Osztaly == tanulo.Osztaly).ToList();

            var szept1 = new DateTime(tanulo.BeiratkozasIdeje.Year, 9, 1);
            var koraiBeiratkozottak = osztalyTanulok
                .Where(t => t.BeiratkozasIdeje < szept1)
                .OrderBy(t => t.Nev)
                .ToList();

            var kesoBeiratkozottak = osztalyTanulok
                .Where(t => t.BeiratkozasIdeje >= szept1)
                .OrderBy(t => t.BeiratkozasIdeje)
                .ToList();

            int sorszam;
            if (tanulo.BeiratkozasIdeje < szept1)
            {
                sorszam = koraiBeiratkozottak.Count + 1;
            }
            else
            {
                sorszam = koraiBeiratkozottak.Count + kesoBeiratkozottak.Count + 1;
            }

            tanulo.NaploSzam = sorszam;
            tanulo.TorzslapSzam = $"{sorszam}/{tanulo.BeiratkozasIdeje.Year}";
        }

        private void BtnTorolTanulo_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tanulo = button.DataContext as Tanulo;

            var result = MessageBox.Show($"Biztosan törölni szeretnéd {tanulo.Nev} adatait?",
                "Törlés megerősítése", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                tanulok.Remove(tanulo);
                MentTanulok();
                dgTanulok.ItemsSource = null;
                dgTanulok.ItemsSource = tanulok;
                FrissitTanuloCombo();
                MessageBox.Show("Tanuló törölve!", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnBetoltTanulok_Click(object sender, RoutedEventArgs e)
        {
            BetoltTanulok();
            MessageBox.Show($"{tanulok.Count} tanuló betöltve!", "Betöltés",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnTanuloStatisztika_Click(object sender, RoutedEventArgs e)
        {
            var statWin = new Window
            {
                Title = "Tanuló Statisztika",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(20) };

            var kollegistak = tanulok.Count(t => t.Kollegista);
            var debreceniek = tanulok.Count(t => t.Lakcim.Contains("Debrecen"));
            var bejarok = tanulok.Count - kollegistak - debreceniek;

            panel.Children.Add(new TextBlock
            {
                Text = "Tanulói statisztika",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            panel.Children.Add(new TextBlock { Text = $"Kollégisták száma: {kollegistak}", FontSize = 14, Margin = new Thickness(0, 5, 0, 5) });
            panel.Children.Add(new TextBlock { Text = $"Debreceni lakhelyűek: {debreceniek}", FontSize = 14, Margin = new Thickness(0, 5, 0, 5) });
            panel.Children.Add(new TextBlock { Text = $"Bejárók száma: {bejarok}", FontSize = 14, Margin = new Thickness(0, 5, 0, 15) });

            var evenkent = tanulok.GroupBy(t => t.BeiratkozasIdeje.Year)
                .OrderBy(g => g.Key);

            panel.Children.Add(new TextBlock
            {
                Text = "Évenkénti felvételi statisztika szakonként:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 10)
            });

            foreach (var ev in evenkent)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{ev.Key} év:",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5)
                });

                var szakonkent = ev.GroupBy(t => t.Szak);
                foreach (var szak in szakonkent)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"  {szak.Key}: {szak.Count()} fő",
                        FontSize = 13,
                        Margin = new Thickness(20, 2, 0, 2)
                    });
                }
                panel.Children.Add(new TextBlock
                {
                    Text = $"  Összesen: {ev.Count()} fő",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(20, 5, 0, 2)
                });
            }

            scroll.Content = panel;
            statWin.Content = scroll;
            statWin.ShowDialog();
        }

        private void UritTanuloForm()
        {
            txtNev.Clear();
            txtSzulHely.Clear();
            dpSzulIdo.SelectedDate = null;
            txtAnyja.Clear();
            txtLakcim.Clear();
            dpBeiratkozas.SelectedDate = null;
            cmbSzak.SelectedIndex = -1;
            cmbOsztaly.SelectedIndex = -1;
            chkKollegista.IsChecked = false;
            cmbKollegium.SelectedIndex = -1;
        }

        private void BtnExportTanuloCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV fájl (*.csv)|*.csv",
                    FileName = $"tanulok_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Napló szám;Törzslap szám;Név;Születési hely;Születési idő;Anyja neve;Lakcím;Beiratkozás ideje;Szak;Osztály;Kollégista;Kollégium");

                    foreach (var t in tanulok)
                    {
                        csv.AppendLine($"{t.NaploSzam};{t.TorzslapSzam};{t.Nev};{t.SzuletesiHely};{t.SzuletesiIdo:yyyy.MM.dd};{t.AnyjaNeve};{t.Lakcim};{t.BeiratkozasIdeje:yyyy.MM.dd};{t.Szak};{t.Osztaly};{t.KollegistaStr};{t.Kollegium}");
                    }

                    File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Tanulók sikeresen exportálva CSV formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportTanuloJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON fájl (*.json)|*.json",
                    FileName = $"tanulok_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(tanulok, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                    MessageBox.Show($"Tanulók sikeresen exportálva JSON formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportTanuloCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "CSV fájl (*.csv)|*.csv",
                    Title = "Tanulók importálása CSV-ből"
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                    if (lines.Length < 2)
                    {
                        MessageBox.Show("A CSV fájl üres vagy nem tartalmaz adatokat!", "Hiba",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int importalt = 0;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(';');
                        if (parts.Length >= 12)
                        {
                            var tanulo = new Tanulo
                            {
                                Id = Guid.NewGuid().ToString(),
                                NaploSzam = int.TryParse(parts[0], out int ns) ? ns : 0,
                                TorzslapSzam = parts[1],
                                Nev = parts[2],
                                SzuletesiHely = parts[3],
                                SzuletesiIdo = DateTime.TryParse(parts[4], out DateTime si) ? si : DateTime.Now,
                                AnyjaNeve = parts[5],
                                Lakcim = parts[6],
                                BeiratkozasIdeje = DateTime.TryParse(parts[7], out DateTime bi) ? bi : DateTime.Now,
                                Szak = parts[8],
                                Osztaly = parts[9],
                                Kollegista = parts[10] == "Igen",
                                Kollegium = parts[11]
                            };
                            tanulok.Add(tanulo);
                            importalt++;
                        }
                    }

                    MentTanulok();
                    dgTanulok.ItemsSource = null;
                    dgTanulok.ItemsSource = tanulok;
                    FrissitTanuloCombo();
                    MessageBox.Show($"{importalt} tanuló sikeresen importálva!", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az importálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportTanuloJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON fájl (*.json)|*.json",
                    Title = "Tanulók importálása JSON-ből"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    var importaltTanulok = JsonSerializer.Deserialize<List<Tanulo>>(json);

                    if (importaltTanulok != null && importaltTanulok.Count > 0)
                    {
                        foreach (var tanulo in importaltTanulok)
                        {
                            if (string.IsNullOrEmpty(tanulo.Id))
                                tanulo.Id = Guid.NewGuid().ToString();
                            tanulok.Add(tanulo);
                        }

                        MentTanulok();
                        dgTanulok.ItemsSource = null;
                        dgTanulok.ItemsSource = tanulok;
                        FrissitTanuloCombo();
                        MessageBox.Show($"{importaltTanulok.Count} tanuló sikeresen importálva!", "Siker",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("A JSON fájl nem tartalmaz érvényes adatokat!", "Hiba",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az importálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Tantárgyak kezelése

        private void BtnMentTargy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTargyNev.Text) ||
                    cmbEvfolyam.SelectedItem == null ||
                    cmbTargyTipus.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtHetiOra.Text))
                {
                    MessageBox.Show("Kérlek töltsd ki az összes mezőt!", "Hiányos adatok",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtHetiOra.Text, out int hetiOra) || hetiOra <= 0)
                {
                    MessageBox.Show("A heti óraszám csak pozitív szám lehet!", "Hibás adat",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int evfolyam = int.Parse((cmbEvfolyam.SelectedItem as ComboBoxItem).Content.ToString());
                string tipus = (cmbTargyTipus.SelectedItem as ComboBoxItem).Content.ToString();

                int evesOra = SzamolEvesOra(evfolyam, tipus, hetiOra);

                var targy = new Tantargy
                {
                    Id = Guid.NewGuid().ToString(),
                    Nev = txtTargyNev.Text,
                    Evfolyam = evfolyam,
                    Tipus = tipus,
                    HetiOra = hetiOra,
                    EvesOra = evesOra
                };

                tantargyak.Add(targy);
                MentTargyak();
                dgTargyak.ItemsSource = null;
                dgTargyak.ItemsSource = tantargyak;
                UritTargyForm();

                MessageBox.Show("Tantárgy sikeresen mentve!", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int SzamolEvesOra(int evfolyam, string tipus, int hetiOra)
        {
            int hetek;
            if (evfolyam >= 9 && evfolyam <= 11)
            {
                hetek = 36;
            }
            else if (evfolyam == 12)
            {
                hetek = tipus == "Közismereti" ? 31 : 36;
            }
            else // 13. évfolyam
            {
                hetek = 31;
            }
            return hetiOra * hetek;
        }

        private void BtnTorolTargy_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var targy = button.DataContext as Tantargy;

            var result = MessageBox.Show($"Biztosan törölni szeretnéd a(z) {targy.Nev} tantárgyat?",
                "Törlés megerősítése", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                tantargyak.Remove(targy);
                MentTargyak();
                dgTargyak.ItemsSource = null;
                dgTargyak.ItemsSource = tantargyak;
                MessageBox.Show("Tantárgy törölve!", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnBetoltTargyak_Click(object sender, RoutedEventArgs e)
        {
            BetoltTargyak();
            MessageBox.Show($"{tantargyak.Count} tantárgy betöltve!", "Betöltés",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnTargyStatisztika_Click(object sender, RoutedEventArgs e)
        {
            var statWin = new Window
            {
                Title = "Tantárgy Statisztika",
                Width = 700,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Tantárgyak statisztikája évfolyamonként",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            var evfolyamok = tantargyak.GroupBy(t => t.Evfolyam).OrderBy(g => g.Key);

            foreach (var evf in evfolyamok)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{evf.Key}. évfolyam",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 15, 0, 10),
                    Foreground = new SolidColorBrush(Colors.DarkBlue)
                });

                var kozismereti = evf.Where(t => t.Tipus == "Közismereti").ToList();
                var szakmai = evf.Where(t => t.Tipus == "Szakmai").ToList();

                panel.Children.Add(new TextBlock
                {
                    Text = $"Közismereti tárgyak száma: {kozismereti.Count}",
                    FontSize = 13,
                    Margin = new Thickness(20, 2, 0, 2)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = $"Szakmai tárgyak száma: {szakmai.Count}",
                    FontSize = 13,
                    Margin = new Thickness(20, 2, 0, 2)
                });

                int kozOsszOra = kozismereti.Sum(t => t.EvesOra);
                int szakOsszOra = szakmai.Sum(t => t.EvesOra);
                int osszes = kozOsszOra + szakOsszOra;

                panel.Children.Add(new TextBlock
                {
                    Text = $"Közismereti éves óraszám: {kozOsszOra}",
                    FontSize = 13,
                    Margin = new Thickness(20, 5, 0, 2)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = $"Szakmai éves óraszám: {szakOsszOra}",
                    FontSize = 13,
                    Margin = new Thickness(20, 2, 0, 2)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = $"Összesített éves óraszám: {osszes}",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(20, 2, 0, 2)
                });
            }

            scroll.Content = panel;
            statWin.Content = scroll;
            statWin.ShowDialog();
        }

        private void BtnTargyHozzarendeles_Click(object sender, RoutedEventArgs e)
        {
            var hozzaWin = new Window
            {
                Title = "Tantárgyak hozzárendelése",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var cmbTanulo = new ComboBox { Margin = new Thickness(0, 5, 0, 15) };
            cmbTanulo.ItemsSource = tanulok;
            cmbTanulo.DisplayMemberPath = "Nev";

            var cmbTargy = new ComboBox { Margin = new Thickness(0, 5, 0, 15) };
            cmbTargy.ItemsSource = tantargyak;
            cmbTargy.DisplayMemberPath = "Nev";

            var lstHozzarendelt = new ListBox { Margin = new Thickness(0, 5, 0, 15) };

            var btnHozzaad = new Button
            {
                Content = "Hozzáad",
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50")),
                Foreground = new SolidColorBrush(Colors.White)
            };

            var panel1 = new StackPanel();
            panel1.Children.Add(new TextBlock { Text = "Válassz tanulót:" });
            panel1.Children.Add(cmbTanulo);
            Grid.SetRow(panel1, 0);

            var panel2 = new StackPanel();
            panel2.Children.Add(new TextBlock { Text = "Válassz tantárgyat:" });
            panel2.Children.Add(cmbTargy);
            Grid.SetRow(panel2, 1);

            var panel3 = new StackPanel();
            panel3.Children.Add(new TextBlock { Text = "Hozzárendelt tantárgyak:" });
            panel3.Children.Add(lstHozzarendelt);
            Grid.SetRow(panel3, 2);

            Grid.SetRow(btnHozzaad, 3);

            cmbTanulo.SelectionChanged += (s, ev) =>
            {
                if (cmbTanulo.SelectedItem is Tanulo t)
                {
                    var targyak = kapcsolatok
                        .Where(k => k.TanuloId == t.Id)
                        .Select(k => tantargyak.FirstOrDefault(ta => ta.Id == k.TargyId)?.Nev)
                        .Where(n => n != null)
                        .ToList();
                    lstHozzarendelt.ItemsSource = targyak;
                }
            };

            btnHozzaad.Click += (s, ev) =>
            {
                if (cmbTanulo.SelectedItem is Tanulo t && cmbTargy.SelectedItem is Tantargy ta)
                {
                    if (!kapcsolatok.Any(k => k.TanuloId == t.Id && k.TargyId == ta.Id))
                    {
                        kapcsolatok.Add(new TanuloTargyKapcsolat
                        {
                            Id = Guid.NewGuid().ToString(),
                            TanuloId = t.Id,
                            TargyId = ta.Id
                        });
                        MentKapcsolatok();

                        var targyak = kapcsolatok
                            .Where(k => k.TanuloId == t.Id)
                            .Select(k => tantargyak.FirstOrDefault(tar => tar.Id == k.TargyId)?.Nev)
                            .Where(n => n != null)
                            .ToList();
                        lstHozzarendelt.ItemsSource = null;
                        lstHozzarendelt.ItemsSource = targyak;

                        MessageBox.Show("Tantárgy hozzárendelve!", "Siker",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ez a tantárgy már hozzá van rendelve!", "Figyelmeztetés",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            };

            grid.Children.Add(panel1);
            grid.Children.Add(panel2);
            grid.Children.Add(panel3);
            grid.Children.Add(btnHozzaad);

            hozzaWin.Content = grid;
            hozzaWin.ShowDialog();
        }

        private void UritTargyForm()
        {
            txtTargyNev.Clear();
            cmbEvfolyam.SelectedIndex = -1;
            cmbTargyTipus.SelectedIndex = -1;
            txtHetiOra.Clear();
        }

        private void BtnExportTargyCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV fájl (*.csv)|*.csv",
                    FileName = $"tantargyak_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Tantárgy neve;Évfolyam;Típus;Heti óraszám;Éves óraszám");

                    foreach (var t in tantargyak)
                    {
                        csv.AppendLine($"{t.Nev};{t.Evfolyam};{t.Tipus};{t.HetiOra};{t.EvesOra}");
                    }

                    File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Tantárgyak sikeresen exportálva CSV formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportTargyJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON fájl (*.json)|*.json",
                    FileName = $"tantargyak_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = JsonSerializer.Serialize(tantargyak, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                    MessageBox.Show($"Tantárgyak sikeresen exportálva JSON formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Jegyek kezelése

        private void FrissitTanuloCombo()
        {
            cmbTanulokJegy.ItemsSource = null;
            cmbTanulokJegy.ItemsSource = tanulok;
            cmbTanulokJegy.DisplayMemberPath = "Nev";
        }

        private void CmbTanulokJegy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTanulokJegy.SelectedItem is Tanulo tanulo)
            {
                var tanuloTargyak = kapcsolatok
                    .Where(k => k.TanuloId == tanulo.Id)
                    .Select(k => tantargyak.FirstOrDefault(t => t.Id == k.TargyId))
                    .Where(t => t != null)
                    .ToList();

                cmbTargyakJegy.ItemsSource = tanuloTargyak;
                cmbTargyakJegy.DisplayMemberPath = "Nev";
                pnlJegyBevitel.Visibility = Visibility.Visible;

                MegjelenitetTanuloDashboard(tanulo);
            }
        }

        private void BtnMentJegy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbTanulokJegy.SelectedItem == null || cmbTargyakJegy.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtJegy.Text))
                {
                    MessageBox.Show("Kérlek válassz tanulót, tantárgyat és add meg a jegyet!",
                        "Hiányos adatok", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtJegy.Text, out int jegyErtek) || jegyErtek < 1 || jegyErtek > 5)
                {
                    MessageBox.Show("A jegy 1 és 5 közötti szám kell legyen!", "Hibás adat",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var tanulo = cmbTanulokJegy.SelectedItem as Tanulo;
                var targy = cmbTargyakJegy.SelectedItem as Tantargy;

                var jegy = new Jegy
                {
                    Id = Guid.NewGuid().ToString(),
                    TanuloId = tanulo.Id,
                    TargyId = targy.Id,
                    JegyErtek = jegyErtek,
                    Tema = txtTema.Text,
                    SzamonkeresTipus = cmbSzamonkeres.SelectedItem != null
                        ? (cmbSzamonkeres.SelectedItem as ComboBoxItem).Content.ToString()
                        : "",
                    Datum = DateTime.Now
                };

                jegyek.Add(jegy);
                MentJegyek();
                MegjelenitetTanuloDashboard(tanulo);
                UritJegyForm();

                MessageBox.Show("Jegy sikeresen mentve!", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MegjelenitetTanuloDashboard(Tanulo tanulo)
        {
            pnlTanuloDashboard.Children.Clear();

            var header = new TextBlock
            {
                Text = $"{tanulo.Nev} jegyei",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            pnlTanuloDashboard.Children.Add(header);

            var tanuloTargyak = kapcsolatok
                .Where(k => k.TanuloId == tanulo.Id)
                .Select(k => tantargyak.FirstOrDefault(t => t.Id == k.TargyId))
                .Where(t => t != null)
                .ToList();

            double osszesitettAtlag = 0;
            int targySzam = 0;
            int bukasraSzam = 0;

            foreach (var targy in tanuloTargyak)
            {
                var targyJegyek = jegyek.Where(j => j.TanuloId == tanulo.Id && j.TargyId == targy.Id).ToList();

                var groupBox = new GroupBox
                {
                    Header = targy.Nev,
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(10)
                };

                var targyPanel = new StackPanel();

                var jegyPanel = new WrapPanel { Margin = new Thickness(0, 5, 0, 10) };
                foreach (var j in targyJegyek)
                {
                    var jegyBtn = new Button
                    {
                        Content = j.JegyErtek.ToString(),
                        Margin = new Thickness(5),
                        Padding = new Thickness(10, 5, 10, 5),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        ToolTip = $"Téma: {j.Tema}\nTípus: {j.SzamonkeresTipus}\nDátum: {j.Datum:yyyy.MM.dd}"
                    };
                    jegyPanel.Children.Add(jegyBtn);
                }
                targyPanel.Children.Add(jegyPanel);

                if (targyJegyek.Count > 0)
                {
                    double atlag = targyJegyek.Average(j => j.JegyErtek);
                    var atlagTxt = new TextBlock
                    {
                        Text = $"Tantárgy átlag: {atlag:F2}",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = atlag < 2.0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black)
                    };
                    targyPanel.Children.Add(atlagTxt);

                    osszesitettAtlag += atlag;
                    targySzam++;

                    if (atlag < 1.75)
                    {
                        bukasraSzam++;
                    }
                }
                else
                {
                    targyPanel.Children.Add(new TextBlock { Text = "Még nincs jegy", FontStyle = FontStyles.Italic });
                }

                groupBox.Content = targyPanel;
                pnlTanuloDashboard.Children.Add(groupBox);
            }

            if (targySzam > 0)
            {
                double vegsoAtlag = osszesitettAtlag / targySzam;
                var osszAtlagTxt = new TextBlock
                {
                    Text = $"Összesített átlag: {vegsoAtlag:F2}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 15, 0, 10)
                };
                pnlTanuloDashboard.Children.Add(osszAtlagTxt);
            }

            var chkVeszelyeztetett = new CheckBox
            {
                Content = "Lemorzsolódással veszélyeztetett",
                IsChecked = bukasraSzam >= 3,
                IsEnabled = false,
                FontWeight = FontWeights.Bold,
                Foreground = bukasraSzam >= 3 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black),
                Margin = new Thickness(0, 5, 0, 0)
            };
            pnlTanuloDashboard.Children.Add(chkVeszelyeztetett);
        }

        private void BtnJegyStatisztika_Click(object sender, RoutedEventArgs e)
        {
            var statWin = new Window
            {
                Title = "Jegy Statisztika - Admin nézet",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var tab = new TabControl();

            // Tantárgyak átlagai tab
            var targTab = new TabItem { Header = "Tantárgyak átlagai" };
            var targScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var targPanel = new StackPanel { Margin = new Thickness(20) };

            targPanel.Children.Add(new TextBlock
            {
                Text = "Tantárgyak átlagai",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            foreach (var targy in tantargyak.OrderBy(t => t.Nev))
            {
                var targyJegyek = jegyek.Where(j => j.TargyId == targy.Id).ToList();
                if (targyJegyek.Count > 0)
                {
                    double atlag = targyJegyek.Average(j => j.JegyErtek);
                    targPanel.Children.Add(new TextBlock
                    {
                        Text = $"{targy.Nev}: {atlag:F2} ({targyJegyek.Count} jegy)",
                        FontSize = 14,
                        Margin = new Thickness(0, 5, 0, 5)
                    });
                }
            }

            targScroll.Content = targPanel;
            targTab.Content = targScroll;

            // Tanulók átlagai tab
            var tanuloTab = new TabItem { Header = "Tanulók átlagai" };
            var tanuloScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var tanuloPanel = new StackPanel { Margin = new Thickness(20) };

            tanuloPanel.Children.Add(new TextBlock
            {
                Text = "Tanulók összesített átlagai",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            foreach (var tanulo in tanulok.OrderBy(t => t.Nev))
            {
                var tanuloJegyek = jegyek.Where(j => j.TanuloId == tanulo.Id).ToList();
                if (tanuloJegyek.Count > 0)
                {
                    var tanuloTargyak = kapcsolatok
                        .Where(k => k.TanuloId == tanulo.Id)
                        .Select(k => k.TargyId)
                        .Distinct()
                        .ToList();

                    double osszAtlag = 0;
                    int targySzam = 0;

                    foreach (var targyId in tanuloTargyak)
                    {
                        var targyJegyek = tanuloJegyek.Where(j => j.TargyId == targyId).ToList();
                        if (targyJegyek.Count > 0)
                        {
                            osszAtlag += targyJegyek.Average(j => j.JegyErtek);
                            targySzam++;
                        }
                    }

                    if (targySzam > 0)
                    {
                        double vegsoAtlag = osszAtlag / targySzam;
                        tanuloPanel.Children.Add(new TextBlock
                        {
                            Text = $"{tanulo.Nev} ({tanulo.Osztaly}): {vegsoAtlag:F2}",
                            FontSize = 14,
                            Margin = new Thickness(0, 5, 0, 5),
                            Foreground = vegsoAtlag < 2.0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black)
                        });
                    }
                }
            }

            tanuloScroll.Content = tanuloPanel;
            tanuloTab.Content = tanuloScroll;

            tab.Items.Add(targTab);
            tab.Items.Add(tanuloTab);
            statWin.Content = tab;
            statWin.ShowDialog();
        }

        private void UritJegyForm()
        {
            txtJegy.Clear();
            txtTema.Clear();
            cmbSzamonkeres.SelectedIndex = -1;
            cmbTargyakJegy.SelectedIndex = -1;
        }

        private void BtnExportJegyCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV fájl (*.csv)|*.csv",
                    FileName = $"jegyek_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Tanuló neve;Tantárgy neve;Jegy;Téma;Számonkérés típusa;Dátum");

                    foreach (var j in jegyek)
                    {
                        var tanulo = tanulok.FirstOrDefault(t => t.Id == j.TanuloId);
                        var targy = tantargyak.FirstOrDefault(t => t.Id == j.TargyId);

                        if (tanulo != null && targy != null)
                        {
                            csv.AppendLine($"{tanulo.Nev};{targy.Nev};{j.JegyErtek};{j.Tema};{j.SzamonkeresTipus};{j.Datum:yyyy.MM.dd HH:mm}");
                        }
                    }

                    File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Jegyek sikeresen exportálva CSV formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportJegyJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON fájl (*.json)|*.json",
                    FileName = $"jegyek_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var exportData = jegyek.Select(j => new
                    {
                        TanuloNeve = tanulok.FirstOrDefault(t => t.Id == j.TanuloId)?.Nev,
                        TargyNeve = tantargyak.FirstOrDefault(t => t.Id == j.TargyId)?.Nev,
                        j.JegyErtek,
                        j.Tema,
                        j.SzamonkeresTipus,
                        j.Datum
                    }).ToList();

                    var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                    MessageBox.Show($"Jegyek sikeresen exportálva JSON formátumban!\n{dialog.FileName}", "Siker",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az exportálás során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Adatkezelés

        private void MentTanulok()
        {
            try
            {
                var json = JsonSerializer.Serialize(tanulok, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TanulokFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BetoltTanulok()
        {
            try
            {
                if (File.Exists(TanulokFile))
                {
                    var json = File.ReadAllText(TanulokFile);
                    tanulok = JsonSerializer.Deserialize<List<Tanulo>>(json) ?? new List<Tanulo>();
                    dgTanulok.ItemsSource = tanulok;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a betöltés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MentTargyak()
        {
            try
            {
                var json = JsonSerializer.Serialize(tantargyak, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TargyakFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BetoltTargyak()
        {
            try
            {
                if (File.Exists(TargyakFile))
                {
                    var json = File.ReadAllText(TargyakFile);
                    tantargyak = JsonSerializer.Deserialize<List<Tantargy>>(json) ?? new List<Tantargy>();
                    dgTargyak.ItemsSource = tantargyak;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a betöltés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MentKapcsolatok()
        {
            try
            {
                var json = JsonSerializer.Serialize(kapcsolatok, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(KapcsolatokFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BetoltKapcsolatok()
        {
            try
            {
                if (File.Exists(KapcsolatokFile))
                {
                    var json = File.ReadAllText(KapcsolatokFile);
                    kapcsolatok = JsonSerializer.Deserialize<List<TanuloTargyKapcsolat>>(json) ?? new List<TanuloTargyKapcsolat>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a betöltés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MentJegyek()
        {
            try
            {
                var json = JsonSerializer.Serialize(jegyek, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JegyekFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a mentés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BetoltJegyek()
        {
            try
            {
                if (File.Exists(JegyekFile))
                {
                    var json = File.ReadAllText(JegyekFile);
                    jegyek = JsonSerializer.Deserialize<List<Jegy>>(json) ?? new List<Jegy>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a betöltés során: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region Adatmodellek

    public class Tanulo
    {
        public string Id { get; set; }
        public string Nev { get; set; }
        public string SzuletesiHely { get; set; }
        public DateTime SzuletesiIdo { get; set; }
        public string AnyjaNeve { get; set; }
        public string Lakcim { get; set; }
        public DateTime BeiratkozasIdeje { get; set; }
        public string Szak { get; set; }
        public string Osztaly { get; set; }
        public bool Kollegista { get; set; }
        public string Kollegium { get; set; }
        public int NaploSzam { get; set; }
        public string TorzslapSzam { get; set; }

        public string KollegistaStr => Kollegista ? "Igen" : "Nem";
    }

    public class Tantargy
    {
        public string Id { get; set; }
        public string Nev { get; set; }
        public int Evfolyam { get; set; }
        public string Tipus { get; set; }
        public int HetiOra { get; set; }
        public int EvesOra { get; set; }
    }

    public class TanuloTargyKapcsolat
    {
        public string Id { get; set; }
        public string TanuloId { get; set; }
        public string TargyId { get; set; }
    }

    public class Jegy
    {
        public string Id { get; set; }
        public string TanuloId { get; set; }
        public string TargyId { get; set; }
        public int JegyErtek { get; set; }
        public string Tema { get; set; }
        public string SzamonkeresTipus { get; set; }
        public DateTime Datum { get; set; }
    }

    #endregion
}