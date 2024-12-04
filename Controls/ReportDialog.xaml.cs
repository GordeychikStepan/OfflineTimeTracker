using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using OfflineTimeTracker.Service;

namespace OfflineTimeTracker
{
    /// <summary>
    /// Логика взаимодействия для ReportDialog.xaml
    /// </summary>
    public partial class ReportDialog : System.Windows.Controls.UserControl
    {
        public ICommand CancelCommand { get; }
        public ICommand GenerateCommand { get; }

        public DateTime SelectedDate { get; private set; }
        public string PeriodType { get; private set; }

        public ReportDialog()
        {
            InitializeComponent();

            DataContext = this;

            CancelCommand = new RelayCommand(_ => CloseDialog(null));
            GenerateCommand = new RelayCommand(_ => GenerateReport());
        }

        private void CloseDialog(object result)
        {
            DialogHost.CloseDialogCommand.Execute(result, this);
        }

        private void GenerateReport()
        {
            if (StartDatePicker.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Пожалуйста, выберите дату.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedDate = StartDatePicker.SelectedDate.Value;
            PeriodType = ((ComboBoxItem)PeriodTypeComboBox.SelectedItem).Content.ToString();

            CloseDialog(new { SelectedDate, PeriodType });
        }
    }
}
