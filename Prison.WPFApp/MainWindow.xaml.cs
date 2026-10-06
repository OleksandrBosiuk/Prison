using Prison.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Prison.WpfApp
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Prisoner> _prisoners;

        public MainWindow()
        {
            InitializeComponent();
            _prisoners = new ObservableCollection<Prisoner>();
            PrisonersList.ItemsSource = _prisoners;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text;
            string type = ((ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString();

            try
            {
                Prisoner newPrisoner = null;

                if (type == "Rookie")
                    newPrisoner = new Rookie(name);
                else if (type == "Philosopher")
                    newPrisoner = new Philosopher(name);
                else if (type == "Artist")
                    newPrisoner = new Artist(name);

                if (newPrisoner != null)
                {
                    _prisoners.Add(newPrisoner);
                    Log(string.Format(Prison.Core.Resources.LogArrival, name));
                    NameTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Prison.Core.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                _prisoners.Remove(p);
                Log(string.Format(Prison.Core.Resources.LogRemoved, p.Name));
            }
        }

        private void PrisonersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PrisonersList.SelectedItem is Prisoner p)
                {
                    SelectedInfoText.Text = $"Selected: {p.Name} ({p.GetType().Name})";
                    EnergyBar.DataContext = p;
                }
                else
                {
                    SelectedInfoText.Text = "Select a prisoner...";
                    EnergyBar.DataContext = null;
                    EnergyBar.Value = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}\n\nLocation: {ex.StackTrace}",
                                "Selection Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void NormalAction_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                Log(p.NormalAction());
            }
        }

        private void CrazyAction_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                Log(p.CrazyAction());
            }
        }

        private void WorkAction_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                if (p is IWork worker)
                {
                    Log(worker.Work());
                }
                else
                {
                    Log(string.Format(Prison.Core.Resources.LogNoWork, p.Name));
                }
            }
        }

        private void StudyAction_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                if (p is IStudy student)
                {
                    Log(student.Study());
                }
                else
                {
                    Log(string.Format(Prison.Core.Resources.LogNoStudy, p.Name));
                }
            }
        }

        private void Log(string message)
        {
            LogList.Items.Add(message);
            LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        }
        private void RestAction_Click(object sender, RoutedEventArgs e)
        {
            if (PrisonersList.SelectedItem is Prisoner p)
            {
                Log(p.Rest());
            }
        }
    }
}
