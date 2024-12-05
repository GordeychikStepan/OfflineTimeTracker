using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OfflineTimeTracker.Service;
using MessageBox = System.Windows.MessageBox;

namespace OfflineTimeTracker.Controls
{
    /// <summary>
    /// Логика взаимодействия для EditTaskDialog.xaml
    /// </summary>
    public partial class EditTaskDialog : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public TaskEntry TaskEntry { get; set; }
        public List<string> RecentProjects { get; set; }

        // Свойства для валидации
        public bool CanSave => EndDateTime >= StartDateTime;
        public bool IsEndDateInvalid => EndDateTime < StartDateTime;

        public EditTaskDialog(TaskEntry taskEntry, List<TaskEntry> allTasks)
        {
            InitializeComponent();

            TaskEntry = taskEntry;

            // Получаем последние проекты за 7 дней
            DateTime now = DateTime.Now.Date;
            DateTime sevenDaysAgo = now.AddDays(-7);

            RecentProjects = allTasks
                .Where(task => task.StartTime.Date >= sevenDaysAgo && task.StartTime.Date <= now)
                .Where(task => !string.IsNullOrEmpty(task.ProjectDescription))
                .GroupBy(task => task.ProjectDescription)
                .Select(g => g.Key)
                .ToList();

            DataContext = this;

            // Инициализируем свойства
            TaskDescription = TaskEntry.Description;
            ProjectDescription = TaskEntry.ProjectDescription;
            StartDateTime = TaskEntry.StartTime;
            EndDateTime = TaskEntry.EndTime;
        }

        // Свойства для привязки
        private string _taskDescription;
        public string TaskDescription
        {
            get => _taskDescription;
            set
            {
                if (_taskDescription != value)
                {
                    _taskDescription = value;
                    OnPropertyChanged(nameof(TaskDescription));
                }
            }
        }

        private string _projectDescription;
        public string ProjectDescription
        {
            get => _projectDescription;
            set
            {
                if (_projectDescription != value)
                {
                    _projectDescription = value;
                    OnPropertyChanged(nameof(ProjectDescription));
                }
            }
        }

        private DateTime _startDateTime;
        public DateTime StartDateTime
        {
            get => _startDateTime;
            set
            {
                if (_startDateTime != value)
                {
                    _startDateTime = value;
                    OnPropertyChanged(nameof(StartDateTime));
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(IsEndDateInvalid));
                }
            }
        }

        private DateTime _endDateTime;
        public DateTime EndDateTime
        {
            get => _endDateTime;
            set
            {
                if (_endDateTime != value)
                {
                    _endDateTime = value;
                    OnPropertyChanged(nameof(EndDateTime));
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(IsEndDateInvalid));
                }
            }
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Команды
        public RelayCommand SaveCommand => new RelayCommand(Save);
        public RelayCommand CancelCommand => new RelayCommand(Cancel);

        private void Save(object parameter)
        {
            if (!CanSave)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Обновляем значения в TaskEntry
            TaskEntry.Description = TaskDescription;
            TaskEntry.ProjectDescription = ProjectDescription;
            TaskEntry.StartTime = StartDateTime;
            TaskEntry.EndTime = EndDateTime;
            TaskEntry.Duration = EndDateTime - StartDateTime;

            // Закрываем диалог и возвращаем обновленную TaskEntry
            DialogHost.CloseDialogCommand.Execute(TaskEntry, null);
        }

        private void Cancel(object parameter)
        {
            // Закрываем диалог без сохранения
            DialogHost.CloseDialogCommand.Execute(null, null);
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem is string selectedProject)
            {
                ProjectDescription = selectedProject;
                ProjectDescriptionTextBox.Text = selectedProject;
            }
        }


    }


}
