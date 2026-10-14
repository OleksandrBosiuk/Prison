using Prison.Core;
using Prison.WPFApp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Prison.WpfApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public Repository<Rookie> RookieRepo { get; } = new();
        public Repository<Philosopher> PhilosopherRepo { get; } = new();
        public Repository<Artist> ArtistRepo { get; } = new();

        public ObservableCollection<string> AvailableTypes { get; } = new()
        {
            "Rookie",
            "Philosopher",
            "Artist"
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

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand NormalActionCommand { get; }
        public ICommand RestCommand { get; }
        public ICommand WorkCommand { get; }
        public ICommand StudyCommand { get; }
        public ICommand CrazyActionCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByEnergyCommand { get; }

        public MainViewModel()
        {
            BindRepoEvents(RookieRepo);
            BindRepoEvents(PhilosopherRepo);
            BindRepoEvents(ArtistRepo);

            AddCommand = new RelayCommand(_ => AddPrisoner(), _ => !string.IsNullOrWhiteSpace(NewInmateName));
            RemoveCommand = new RelayCommand(_ => RemovePrisoner(), _ => SelectedPrisoner != null);

            NormalActionCommand = new RelayCommand(_ => PerformNormal(), _ => SelectedPrisoner != null);
            RestCommand = new RelayCommand(_ => PerformRest(), _ => SelectedPrisoner != null);
            WorkCommand = new RelayCommand(_ => PerformWork(), _ => SelectedPrisoner is IWork);
            StudyCommand = new RelayCommand(_ => PerformStudy(), _ => SelectedPrisoner is IStudy);
            CrazyActionCommand = new RelayCommand(_ => PerformCrazy(), _ => SelectedPrisoner != null);

            SortByNameCommand = new RelayCommand(_ => SortList(byEnergy: false));
            SortByEnergyCommand = new RelayCommand(_ => SortList(byEnergy: true));

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
            return RookieRepo.GetAll()
                .Concat<Prisoner>(PhilosopherRepo.GetAll())
                .Concat<Prisoner>(ArtistRepo.GetAll());
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
                default:
                    var r = new Rookie(NewInmateName);
                    r.ActionLogged += msg => AddLog(msg);
                    RookieRepo.Add(r);
                    break;
            }

            NewInmateName = "";
            ApplyLinq();
        }

        private void RemovePrisoner()
        {
            if (SelectedPrisoner == null) return;

            if (SelectedPrisoner is Rookie r) RookieRepo.Remove(r);
            else if (SelectedPrisoner is Philosopher ph) PhilosopherRepo.Remove(ph);
            else if (SelectedPrisoner is Artist ar) ArtistRepo.Remove(ar);

            SelectedPrisoner = null;
            ApplyLinq();
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}