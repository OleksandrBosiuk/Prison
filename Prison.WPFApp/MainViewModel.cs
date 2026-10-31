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
        public Repository<Rookie> RookieRepo { get; } = new();
        public Repository<Philosopher> PhilosopherRepo { get; } = new();
        public Repository<Artist> ArtistRepo { get; } = new();
        public Repository<Lawyer> LawyerRepo { get; } = new();
        public Repository<Athlete> AthleteRepo { get; } = new();
        public Repository<Mechanic> MechanicRepo { get; } = new();

        public ObservableCollection<string> AvailableTypes { get; } = new()
        {
            "Rookie",
            "Philosopher",
            "Artist",
            "Lawyer",
            "Athlete",
            "Mechanic"
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
            ? $"{SelectedPrisoner.Name} ({SelectedPrisoner.TypeName})"
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

        // Поля Vol.3
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

        // Команды Vol.2
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand NormalActionCommand { get; }
        public ICommand RestCommand { get; }
        public ICommand WorkCommand { get; }
        public ICommand StudyCommand { get; }
        public ICommand CrazyActionCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByEnergyCommand { get; }

        // Команды Vol.3
        public ICommand StartSequentialCommand { get; }
        public ICommand StartParallelCommand { get; }
        public ICommand CancelCommand { get; }

        public MainViewModel()
        {
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
            switch (SelectedType)
            {
                case "Philosopher":
                    var p = new Philosopher(NewInmateName);
                    p.ActionLogged += msg => AddLog(msg);
                    PhilosopherRepo.Add(p);
                    break;
                case "Artist":
                    var a = new Artist(NewInmateName);
                    a.ActionLogged += msg => AddLog(msg);
                    ArtistRepo.Add(a);
                    break;
                case "Lawyer":
                    var l = new Lawyer(NewInmateName);
                    l.ActionLogged += msg => AddLog(msg);
                    LawyerRepo.Add(l);
                    break;
                case "Athlete":
                    var ath = new Athlete(NewInmateName);
                    ath.ActionLogged += msg => AddLog(msg);
                    AthleteRepo.Add(ath);
                    break;
                case "Mechanic":
                    var m = new Mechanic(NewInmateName);
                    m.ActionLogged += msg => AddLog(msg);
                    MechanicRepo.Add(m);
                    break;
                default:
                    var r = new Rookie(NewInmateName);
                    r.ActionLogged += msg => AddLog(msg);
                    RookieRepo.Add(r);
                    break;
            }

            NewInmateName = "";
            ApplyLinq();
            CommandManager.InvalidateRequerySuggested();
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
            foreach (var item in filtered)
            {
                DisplayedInmates.Add(item);
            }

            if (currentSelected != null && DisplayedInmates.Contains(currentSelected))
            {
                SelectedPrisoner = currentSelected;
            }

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

        private void AddLog(string log)
        {
            EventLogs.Insert(0, log);
        }

        private void CancelCalculation()
        {
            _cts?.Cancel();
        }

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
                    bool failThis = SimulateErrorOnSecond && i == 1;

                    var report = await Task.Run(() => SimulationService.RunHeavySimulation(snap, Workload, failThis, token), token);
                    SimulationReports.Add(report);
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

                foreach (var report in results)
                {
                    SimulationReports.Add(report);
                }
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