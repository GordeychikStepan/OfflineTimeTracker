using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OfflineTimeTracker.Service;
using MessageBox = System.Windows.MessageBox;

namespace OfflineTimeTracker
{
    /// <summary>
    /// Логика взаимодействия для ReportDialog.xaml
    /// </summary>
    public partial class ReportDialog : System.Windows.Controls.UserControl
    {
        public ICommand CancelCommand { get; }
        public ICommand GenerateCommand { get; }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public ReportDialog()
        {
            InitializeComponent();

            DataContext = this;

            CancelCommand = new RelayCommand(_ => CloseDialog(null));
            GenerateCommand = new RelayCommand(_ => GenerateReport());

            // Добавляем обработчик события для StartDatePicker
            StartDatePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;
        }

        private void CloseDialog(object result)
        {
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(result, null);
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate != null)
            {
                // Обновляем DisplayDateStart у EndDatePicker
                EndDatePicker.DisplayDateStart = StartDatePicker.SelectedDate;

                // Если дата окончания раньше даты начала, устанавливаем дату окончания равной дате начала
                if (EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
                {
                    EndDatePicker.SelectedDate = StartDatePicker.SelectedDate;
                }
            }
            else
            {
                // Если дата начала не выбрана, сбрасываем ограничения
                EndDatePicker.DisplayDateStart = null;
            }
        }

        private void GenerateReport()
        {
            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StartDate = StartDatePicker.SelectedDate.Value.Date;
            EndDate = EndDatePicker.SelectedDate.Value.Date;

            if (EndDate < StartDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloseDialog(new { StartDate, EndDate });
        }
    }
}
