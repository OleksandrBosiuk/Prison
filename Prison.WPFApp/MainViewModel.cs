using Microsoft.Win32;
using Prison.Core;
using Prison.WPFApp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Prison.WpfApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IWorldStorage _defaultDiStorage;
        private readonly JsonWorldStorage _jsonStorage;
        private readonly XmlWorldStorage _xmlStorage;
        private readonly SqlWorldStorage _sqlStorage;

        public Repository<Rookie> RookieRepo { get; } = new();
        public Repository<Philosopher> PhilosopherRepo { get; } = new();
        public Repository<Artist> ArtistRepo { get; } = new();
        public Repository<Lawyer> LawyerRepo { get; } = new();
        public Repository<Athlete> AthleteRepo { get; } = new();
        public Repository<Mechanic> MechanicRepo { get; } = new();

        public ObservableCollection<string> AvailableTypes { get; } = new()
        {
            "Rookie", "Philosopher", "Artist", "Lawyer", "Athlete", "Mechanic"
        };

        private string _selectedType = "Rookie";
        public string SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); }
        }

        private string _newInmateName = "";
        public string NewInmateName
        {
            get => _newInmateName;
            set { _newInmateName = value; OnPropertyChanged(); }
        }

        private Prisoner? _selectedPrisoner;
        public Prisoner? SelectedPrisoner
        {
            get => _selectedPrisoner;
            set
            {
                _selectedPrisoner = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPrisonerText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedPrisonerText => SelectedPrisoner != null
            ? $"{SelectedPrisoner.Name} ({SelectedPrisoner.TypeName}) [ID: {SelectedPrisoner.Id.ToString()[..8]}]"
            : "Select a prisoner...";

        private string _searchFilter = "";
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                OnPropertyChanged();
                ApplyLinq();
            }
        }

        private string _groupSummaryText = "Summary: No inmates registered.";
        public string GroupSummaryText
        {
            get => _groupSummaryText;
            set { _groupSummaryText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Prisoner> DisplayedInmates { get; } = new();
        public ObservableCollection<string> EventLogs { get; } = new();

        private CancellationTokenSource? _cts;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _workload = 15_000_000;
        public int Workload
        {
            get => _workload;
            set { _workload = value; OnPropertyChanged(); }
        }

        private bool _simulateErrorOnSecond;
        public bool SimulateErrorOnSecond
        {
            get => _simulateErrorOnSecond;
            set { _simulateErrorOnSecond = value; OnPropertyChanged(); }
        }

        private string _totalExecutionTimeText = "Total Time: -";
        public string TotalExecutionTimeText
        {
            get => _totalExecutionTimeText;
            set { _totalExecutionTimeText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TaskReportItem> SimulationReports { get; } = new();

        public string ActiveDiStorageText => $"DI Active: {_defaultDiStorage.StorageName}";

        private string _storageStatusText = "Storage ready.";
        public string StorageStatusText
        {
            get => _storageStatusText;
            set { _storageStatusText = value; OnPropertyChanged(); }
        }
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand NormalActionCommand { get; }
        public ICommand RestCommand { get; }
        public ICommand WorkCommand { get; }
        public ICommand StudyCommand { get; }
        public ICommand CrazyActionCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByEnergyCommand { get; }

        public ICommand StartSequentialCommand { get; }
        public ICommand StartParallelCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand SaveJsonCommand { get; }
        public ICommand LoadJsonCommand { get; }
        public ICommand SaveXmlCommand { get; }
        public ICommand LoadXmlCommand { get; }
        public ICommand SaveDbCommand { get; }
        public ICommand LoadDbCommand { get; }

        public MainViewModel(
            IWorldStorage defaultDiStorage,
            JsonWorldStorage jsonStorage,
            XmlWorldStorage xmlStorage,
            SqlWorldStorage sqlStorage)
        {
            _defaultDiStorage = defaultDiStorage;
            _jsonStorage = jsonStorage;
            _xmlStorage = xmlStorage;
            _sqlStorage = sqlStorage;

            BindRepoEvents(RookieRepo);
            BindRepoEvents(PhilosopherRepo);
            BindRepoEvents(ArtistRepo);
            BindRepoEvents(LawyerRepo);
            BindRepoEvents(AthleteRepo);
            BindRepoEvents(MechanicRepo);

            AddCommand = new RelayCommand(_ => AddPrisoner(), _ => !string.IsNullOrWhiteSpace(NewInmateName));
            RemoveCommand = new RelayCommand(_ => RemovePrisoner(), _ => SelectedPrisoner != null);
            NormalActionCommand = new RelayCommand(_ => PerformNormal(), _ => SelectedPrisoner != null);
            RestCommand = new RelayCommand(_ => PerformRest(), _ => SelectedPrisoner != null);
            WorkCommand = new RelayCommand(_ => PerformWork(), _ => SelectedPrisoner is IWork);
            StudyCommand = new RelayCommand(_ => PerformStudy(), _ => SelectedPrisoner is IStudy);
            CrazyActionCommand = new RelayCommand(_ => PerformCrazy(), _ => SelectedPrisoner != null);
            SortByNameCommand = new RelayCommand(_ => SortList(byEnergy: false));
            SortByEnergyCommand = new RelayCommand(_ => SortList(byEnergy: true));

            StartSequentialCommand = new RelayCommand(async _ => await RunSequentialAsync(), _ => !IsBusy && DisplayedInmates.Count >= 3);
            StartParallelCommand = new RelayCommand(async _ => await RunParallelAsync(), _ => !IsBusy && DisplayedInmates.Count >= 3);
            CancelCommand = new RelayCommand(_ => CancelCalculation(), _ => IsBusy);

            SaveJsonCommand = new RelayCommand(async _ => await SaveToFileAsync(_jsonStorage, "JSON files (*.json)|*.json", ".json"), _ => !IsBusy);
            LoadJsonCommand = new RelayCommand(async _ => await LoadFromFileAsync(_jsonStorage, "JSON files (*.json)|*.json"), _ => !IsBusy);

            SaveXmlCommand = new RelayCommand(async _ => await SaveToFileAsync(_xmlStorage, "XML files (*.xml)|*.xml", ".xml"), _ => !IsBusy);
            LoadXmlCommand = new RelayCommand(async _ => await LoadFromFileAsync(_xmlStorage, "XML files (*.xml)|*.xml"), _ => !IsBusy);

            SaveDbCommand = new RelayCommand(async _ => await SaveToDbAsync(), _ => !IsBusy);
            LoadDbCommand = new RelayCommand(async _ => await LoadFromDbAsync(), _ => !IsBusy);

            ApplyLinq();
        }

        private void BindRepoEvents<T>(Repository<T> repo) where T : Prisoner
        {
            repo.ItemAdded += (s, e) => AddLog($"[Added] {e.Message}");
            repo.ItemRemoved += (s, e) => AddLog($"[Removed] {e.Message}");
            repo.ItemWentCrazy += (s, e) => AddLog($"[Crazy] {e.Actor.Name}: {e.Message}");
        }

        private IEnumerable<Prisoner> GetAllInmates()
        {
            var list = new List<Prisoner>();
            list.AddRange(RookieRepo.GetAll());
            list.AddRange(PhilosopherRepo.GetAll());
            list.AddRange(ArtistRepo.GetAll());
            list.AddRange(LawyerRepo.GetAll());
            list.AddRange(AthleteRepo.GetAll());
            list.AddRange(MechanicRepo.GetAll());
            return list;
        }

        private void AddPrisoner()
        {
            AddInmateInstance(SelectedType, NewInmateName, Guid.NewGuid(), 100);
            NewInmateName = "";
            ApplyLinq();
            CommandManager.InvalidateRequerySuggested();
        }

        private void AddInmateInstance(string typeName, string name, Guid id, int energy)
        {
            switch (typeName)
            {
                case "Philosopher":
                    var p = new Philosopher(name);
                    p.RestoreState(id, energy);
                    p.ActionLogged += msg => AddLog(msg);
                    PhilosopherRepo.Add(p);
                    break;
                case "Artist":
                    var a = new Artist(name);
                    a.RestoreState(id, energy);
                    a.ActionLogged += msg => AddLog(msg);
                    ArtistRepo.Add(a);
                    break;
                case "Lawyer":
                    var l = new Lawyer(name);
                    l.RestoreState(id, energy);
                    l.ActionLogged += msg => AddLog(msg);
                    LawyerRepo.Add(l);
                    break;
                case "Athlete":
                    var ath = new Athlete(name);
                    ath.RestoreState(id, energy);
                    ath.ActionLogged += msg => AddLog(msg);
                    AthleteRepo.Add(ath);
                    break;
                case "Mechanic":
                    var m = new Mechanic(name);
                    m.RestoreState(id, energy);
                    m.ActionLogged += msg => AddLog(msg);
                    MechanicRepo.Add(m);
                    break;
                default:
                    var r = new Rookie(name);
                    r.RestoreState(id, energy);
                    r.ActionLogged += msg => AddLog(msg);
                    RookieRepo.Add(r);
                    break;
            }
        }

        private WorldDto CreateWorldSnapshot()
        {
            var world = new WorldDto { SavedAt = DateTime.UtcNow };

            void AddBlock<T>(string blockName, Repository<T> repo) where T : Prisoner
            {
                var inmates = repo.GetAll().Select(p => new PrisonerDto
                {
                    Id = p.Id,
                    TypeName = p.TypeName,
                    Name = p.Name,
                    Energy = p.Energy
                }).ToList();

                world.Blocks.Add(new PrisonBlockDto
                {
                    BlockName = blockName,
                    Inmates = inmates
                });
            }

            AddBlock("RookieBlock", RookieRepo);
            AddBlock("PhilosopherBlock", PhilosopherRepo);
            AddBlock("ArtistBlock", ArtistRepo);
            AddBlock("LawyerBlock", LawyerRepo);
            AddBlock("AthleteBlock", AthleteRepo);
            AddBlock("MechanicBlock", MechanicRepo);

            return world;
        }

        private void ClearAllRepositories()
        {
            foreach (var item in RookieRepo.GetAll().ToList()) RookieRepo.Remove(item);
            foreach (var item in PhilosopherRepo.GetAll().ToList()) PhilosopherRepo.Remove(item);
            foreach (var item in ArtistRepo.GetAll().ToList()) ArtistRepo.Remove(item);
            foreach (var item in LawyerRepo.GetAll().ToList()) LawyerRepo.Remove(item);
            foreach (var item in AthleteRepo.GetAll().ToList()) AthleteRepo.Remove(item);
            foreach (var item in MechanicRepo.GetAll().ToList()) MechanicRepo.Remove(item);
            SelectedPrisoner = null;
        }

        private void RestoreWorldFromDto(WorldDto dto)
        {
            ClearAllRepositories();
            EventLogs.Clear();

            foreach (var block in dto.Blocks)
            {
                foreach (var p in block.Inmates)
                {
                    AddInmateInstance(p.TypeName, p.Name, p.Id, p.Energy);
                }
            }

            ApplyLinq();
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task SaveToFileAsync(IWorldStorage storage, string filter, string defaultExt)
        {
            var dlg = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "prison_world"
            };

            if (dlg.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                var snapshot = CreateWorldSnapshot();
                await storage.SaveAsync(snapshot, dlg.FileName);
                StorageStatusText = $"Saved to {System.IO.Path.GetFileName(dlg.FileName)} ({storage.StorageName})";
                AddLog($"[Storage] {StorageStatusText}");
            }
            catch (Exception ex)
            {
                StorageStatusText = $"Save error: {ex.Message}";
                AddLog($"[Storage Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFromFileAsync(IWorldStorage storage, string filter)
        {
            var dlg = new OpenFileDialog
            {
                Filter = filter,
                CheckFileExists = true
            };

            if (dlg.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                var loadedWorld = await storage.LoadAsync(dlg.FileName);
                RestoreWorldFromDto(loadedWorld);
                StorageStatusText = $"Loaded from {System.IO.Path.GetFileName(dlg.FileName)}";
                AddLog($"[Storage] World restored from {System.IO.Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex)
            {
                StorageStatusText = $"Load failed (world kept): {ex.Message}";
                AddLog($"[Load Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveToDbAsync()
        {
            IsBusy = true;
            try
            {
                var snapshot = CreateWorldSnapshot();
                IWorldStorage target = _defaultDiStorage is SqlWorldStorage ? _defaultDiStorage : _sqlStorage;
                await target.SaveAsync(snapshot, App.DefaultConnectionString);
                StorageStatusText = "Saved to LocalDB (PrisonBlock13Db)!";
                AddLog($"[LocalDB] {StorageStatusText}");
            }
            catch (Exception ex)
            {
                StorageStatusText = $"LocalDB save error: {ex.Message}";
                AddLog($"[LocalDB Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFromDbAsync()
        {
            IsBusy = true;
            try
            {
                IWorldStorage target = _defaultDiStorage is SqlWorldStorage ? _defaultDiStorage : _sqlStorage;
                var loadedWorld = await target.LoadAsync(App.DefaultConnectionString);
                RestoreWorldFromDto(loadedWorld);
                StorageStatusText = "Loaded from LocalDB (PrisonBlock13Db)!";
                AddLog($"[LocalDB] World restored from LocalDB.");
            }
            catch (Exception ex)
            {
                StorageStatusText = $"LocalDB load error: {ex.Message}";
                AddLog($"[LocalDB Error] {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RemovePrisoner()
        {
            if (SelectedPrisoner == null) return;

            if (SelectedPrisoner is Rookie r) RookieRepo.Remove(r);
            else if (SelectedPrisoner is Philosopher ph) PhilosopherRepo.Remove(ph);
            else if (SelectedPrisoner is Artist ar) ArtistRepo.Remove(ar);
            else if (SelectedPrisoner is Lawyer lw) LawyerRepo.Remove(lw);
            else if (SelectedPrisoner is Athlete ath) AthleteRepo.Remove(ath);
            else if (SelectedPrisoner is Mechanic mc) MechanicRepo.Remove(mc);

            SelectedPrisoner = null;
            ApplyLinq();
            CommandManager.InvalidateRequerySuggested();
        }

        private void PerformNormal()
        {
            if (SelectedPrisoner == null) return;
            AddLog($"[Action] {SelectedPrisoner.NormalAction()}");
            ApplyLinq();
        }

        private void PerformRest()
        {
            if (SelectedPrisoner == null) return;
            AddLog($"[Rest] {SelectedPrisoner.Rest()}");
            ApplyLinq();
        }

        private void PerformWork()
        {
            if (SelectedPrisoner is IWork worker)
            {
                AddLog($"[Work] {worker.Work()}");
                ApplyLinq();
            }
        }

        private void PerformStudy()
        {
            if (SelectedPrisoner is IStudy student)
            {
                AddLog($"[Study] {student.Study()}");
                ApplyLinq();
            }
        }

        private void PerformCrazy()
        {
            if (SelectedPrisoner == null) return;

            if (SelectedPrisoner is Rookie r) RookieRepo.TriggerCrazyAction(r);
            else if (SelectedPrisoner is Philosopher ph) PhilosopherRepo.TriggerCrazyAction(ph);
            else if (SelectedPrisoner is Artist ar) ArtistRepo.TriggerCrazyAction(ar);
            else if (SelectedPrisoner is Lawyer lw) LawyerRepo.TriggerCrazyAction(lw);
            else if (SelectedPrisoner is Athlete ath) AthleteRepo.TriggerCrazyAction(ath);
            else if (SelectedPrisoner is Mechanic mc) MechanicRepo.TriggerCrazyAction(mc);

            ApplyLinq();
        }

        private void ApplyLinq()
        {
            var currentSelected = SelectedPrisoner;
            var all = GetAllInmates().ToList();

            var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                ? all
                : all.Where(x => x.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                              || x.TypeName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            DisplayedInmates.Clear();
            foreach (var item in filtered) DisplayedInmates.Add(item);

            if (currentSelected != null && DisplayedInmates.Contains(currentSelected))
                SelectedPrisoner = currentSelected;

            var groups = all.GroupBy(x => x.TypeName)
                            .Select(g => $"{g.Key}: {g.Count()} (Avg: {(g.Any() ? (int)g.Average(i => i.Energy) : 0)}%)");

            GroupSummaryText = all.Any()
                ? "Summary: " + string.Join(" | ", groups)
                : "Summary: No inmates registered.";
        }

        private void SortList(bool byEnergy)
        {
            var list = byEnergy
                ? DisplayedInmates.OrderByDescending(x => x.Energy).ToList()
                : DisplayedInmates.OrderBy(x => x.Name).ToList();

            DisplayedInmates.Clear();
            foreach (var item in list) DisplayedInmates.Add(item);
        }

        private void AddLog(string log) => EventLogs.Insert(0, log);

        private void CancelCalculation() => _cts?.Cancel();

        private List<PrisonerSnapshot> CreateSnapshots()
        {
            return DisplayedInmates
                .Take(Math.Max(3, DisplayedInmates.Count))
                .Select(p => new PrisonerSnapshot(p.Name, p.TypeName, p.Energy))
                .ToList();
        }

        private async Task RunSequentialAsync()
        {
            var snapshots = CreateSnapshots();
            if (snapshots.Count < 3) return;

            IsBusy = true;
            SimulationReports.Clear();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var swTotal = Stopwatch.StartNew();
            TotalExecutionTimeText = "Running sequentially...";

            try
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    var snap = snapshots[i];

                    if (token.IsCancellationRequested)
                    {
                        SimulationReports.Add(new TaskReportItem
                        {
                            PrisonerName = snap.Name,
                            Status = "Canceled",
                            DurationMs = 0,
                            ResultOrError = "Calculation was canceled before start."
                        });
                        continue;
                    }

                    bool failThis = SimulateErrorOnSecond && i == 1;

                    try
                    {
                        var report = await Task.Run(() => SimulationService.RunHeavySimulation(snap, Workload, failThis, token));
                        SimulationReports.Add(report);
                    }
                    catch (OperationCanceledException)
                    {
                        SimulationReports.Add(new TaskReportItem
                        {
                            PrisonerName = snap.Name,
                            Status = "Canceled",
                            DurationMs = 0,
                            ResultOrError = "Calculation was canceled by user."
                        });
                    }
                }
            }
            finally
            {
                swTotal.Stop();
                TotalExecutionTimeText = $"Sequential finished in: {swTotal.ElapsedMilliseconds} ms";
                IsBusy = false;
            }
        }

        private async Task RunParallelAsync()
        {
            var snapshots = CreateSnapshots();
            if (snapshots.Count < 3) return;

            IsBusy = true;
            SimulationReports.Clear();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var swTotal = Stopwatch.StartNew();
            TotalExecutionTimeText = "Running in parallel...";

            try
            {
                var tasks = snapshots.Select((snap, idx) =>
                {
                    bool failThis = SimulateErrorOnSecond && idx == 1;
                    return Task.Run(() => SimulationService.RunHeavySimulation(snap, Workload, failThis, token));
                }).ToList();

                var results = await Task.WhenAll(tasks);
                foreach (var report in results) SimulationReports.Add(report);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                swTotal.Stop();
                TotalExecutionTimeText = $"Parallel finished in: {swTotal.ElapsedMilliseconds} ms";
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}