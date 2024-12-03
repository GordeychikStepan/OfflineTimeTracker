using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Windows.Input;
using OfflineTimeTracker.Service;
using MaterialDesignThemes.Wpf;

namespace OfflineTimeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime startTime;
        private bool isTracking = false;
        private List<TaskEntry> taskEntries;
        private string dataFile = "tasks.json";

        private DispatcherTimer dispatcherTimer;
        private TimeSpan elapsedTime;

        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            LoadTasks();
            InitializeTimer();

            // Устанавливаем сегодняшнюю дату в календаре
            DateCalendar.SelectedDate = DateTime.Now.Date;

            // Инициализация команд
            EditTaskCommand = new RelayCommand(EditTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask);
        }

        private void EditTask(object parameter)
        {
            if (parameter is TaskEntry task)
            {
                // Проверяем корректность параметра
                if (task == null)
                {
                    MessageBox.Show("Задача не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Открываем диалоговое окно для редактирования задачи
                string newDescription = Microsoft.VisualBasic.Interaction.InputBox(
                    "Введите новое название задачи:",
                    "Редактирование задачи",
                    task.Description);

                if (!string.IsNullOrWhiteSpace(newDescription))
                {
                    task.Description = newDescription;
                    SaveTasks(); // Сохраняем изменения
                    UpdateTaskListView();
                }
            }
            else
            {
                MessageBox.Show("Неверный тип данных!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void DeleteTask(object parameter)
        {
            if (parameter is TaskEntry task)
            {
                if (task == null)
                {
                    MessageBox.Show("Задача не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show($"Вы уверены, что хотите удалить задачу \"{task.Description}\"?",
                    "Удаление задачи", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    taskEntries.Remove(task);
                    SaveTasks(); // Сохраняем изменения
                    UpdateTaskListView();
                }
            }
            else
            {
                MessageBox.Show("Неверный тип данных!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InitializeTimer()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000); // Уменьшаем интервал для более точного обновления
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            elapsedTime = TimeSpan.Zero;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            elapsedTime = now - startTime;
            TimerTextBlock.Text = elapsedTime.ToString(@"hh\:mm\:ss");
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskDescription.Text))
            {
                MessageBox.Show("Пожалуйста, введите описание задачи перед началом.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!isTracking)
            {
                startTime = DateTime.Now;
                isTracking = true;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                TaskDescription.IsEnabled = false;

                elapsedTime = TimeSpan.Zero;
                TimerTextBlock.Text = "00:00:00";
                dispatcherTimer.Start();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isTracking)
            {
                var endTime = DateTime.Now;
                isTracking = false;

                dispatcherTimer.Stop();

                var taskEntry = new TaskEntry
                {
                    Description = TaskDescription.Text,
                    StartTime = startTime,
                    EndTime = endTime,
                    Duration = endTime - startTime
                };

                taskEntries.Add(taskEntry);
                SaveTasks();

                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                TaskDescription.Text = string.Empty;
                TaskDescription.IsEnabled = true;

                TimerTextBlock.Text = "00:00:00";

                UpdateTaskListView();
            }
        }

        private void SaveTasks()
        {
            var json = JsonConvert.SerializeObject(taskEntries, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(dataFile, json);
        }

        private void LoadTasks()
        {
            if (File.Exists(dataFile))
            {
                var json = File.ReadAllText(dataFile);
                taskEntries = JsonConvert.DeserializeObject<List<TaskEntry>>(json);
            }
            else
            {
                taskEntries = new List<TaskEntry>();
            }

            UpdateTaskListView();
        }

        private void UpdateTaskListView()
        {
            if (DateCalendar.SelectedDate == null)
                return;

            DateTime selectedDate = DateCalendar.SelectedDate.Value.Date;

            // Фильтруем задачи по выбранной дате и сортируем по времени начала в порядке убывания
            var tasksForSelectedDate = taskEntries
                .Where(task => task.StartTime.Date == selectedDate)
                .OrderByDescending(task => task.StartTime)
                .ToList();

            // Обновляем заголовок списка задач
            if (selectedDate == DateTime.Now.Date)
            {
                TaskListHeader.Text = "Задачи на сегодня:";
            }
            else
            {
                TaskListHeader.Text = $"Задачи на {selectedDate:dd MMMM}:";
            }

            TaskListView.ItemsSource = null;
            TaskListView.ItemsSource = tasksForSelectedDate;
        }

        private void DateCalendar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTaskListView();
        }

        private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowReportDialogAsync();
        }


        private async Task ShowReportDialogAsync()
        {
            var dialog = new ReportDialog();
            var result = await DialogHost.Show(dialog, "RootDialog");

            if (result is null)
            {
                // Пользователь отменил
                return;
            }

            var selectedDate = (DateTime)result.GetType().GetProperty("SelectedDate").GetValue(result);
            var periodType = (string)result.GetType().GetProperty("PeriodType").GetValue(result);

            GeneratePDFReport(selectedDate, periodType);
        }


        private void GeneratePDFReport(DateTime selectedDate, string periodType)
        {
            // Определяем период
            DateTime startDate = selectedDate.Date;
            DateTime endDate = selectedDate.Date;

            switch (periodType)
            {
                case "День":
                    endDate = startDate;
                    break;
                case "Неделя":
                    int diff = (7 + (startDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = startDate.AddDays(-1 * diff);
                    endDate = startDate.AddDays(6);
                    break;
                case "Месяц":
                    startDate = new DateTime(startDate.Year, startDate.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
            }

            // Фильтруем задачи за выбранный период
            var tasksInPeriod = taskEntries
                .Where(task => task.StartTime.Date >= startDate && task.StartTime.Date <= endDate)
                .OrderBy(task => task.StartTime)
                .ToList();

            if (tasksInPeriod.Count == 0)
            {
                MessageBox.Show("Нет задач за выбранный период.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Группируем задачи по описанию и подсчитываем общее время
            var groupedTasks = tasksInPeriod
                .GroupBy(task => task.Description)
                .Select(group => new
                {
                    Description = group.Key,
                    TotalDuration = group.Aggregate(TimeSpan.Zero, (sum, task) => sum.Add(task.Duration))
                })
                .OrderByDescending(group => group.TotalDuration)
                .ToList();

            // Создаем PDF документ
            PdfDocument document = new PdfDocument();
            document.Info.Title = $"Отчет за период {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 12, XFontStyleEx.Regular);

            double yPoint = 40;

            // Заголовок
            gfx.DrawString($"Отчет за период {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}", new XFont("Verdana", 14, XFontStyleEx.Bold), XBrushes.Black,
                new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
            yPoint += 40;

            // Таблица задач с границами
            gfx.DrawString("Список задач:", font, XBrushes.Black, new XRect(40, yPoint, page.Width, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            // Размеры столбцов
            double col1Width = 300; // Ширина столбца "Описание задачи"
            double col2Width = 200; // Ширина столбца "Дата и время"

            // Заголовок таблицы
            gfx.DrawRectangle(XPens.Black, 40, yPoint, col1Width, 20); // Граница для столбца "Описание задачи"
            gfx.DrawRectangle(XPens.Black, 40 + col1Width, yPoint, col2Width, 20); // Граница для столбца "Дата и время"
            gfx.DrawString("Задача", font, XBrushes.Black, new XRect(45, yPoint, col1Width, 20), XStringFormats.TopLeft);
            gfx.DrawString("Дата и время", font, XBrushes.Black, new XRect(45 + col1Width, yPoint, col2Width, 20), XStringFormats.TopLeft);
            yPoint += 20;

            // Данные таблицы задач
            foreach (var task in tasksInPeriod)
            {
                gfx.DrawRectangle(XPens.Black, 40, yPoint, col1Width, 20); // Граница для "Описание задачи"
                gfx.DrawRectangle(XPens.Black, 40 + col1Width, yPoint, col2Width, 20); // Граница для "Дата и время"

                gfx.DrawString(task.Description, font, XBrushes.Black, new XRect(45, yPoint, col1Width - 5, 20), XStringFormats.TopLeft);
                string dateTimeInfo = $"{task.StartTime:dd.MM.yyyy} {task.StartTime:HH:mm} - {task.EndTime:HH:mm}";
                gfx.DrawString(dateTimeInfo, font, XBrushes.Black, new XRect(45 + col1Width, yPoint, col2Width - 5, 20), XStringFormats.TopLeft);

                yPoint += 20;

                if (yPoint > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Итоговое время
            yPoint += 20;
            gfx.DrawString("Итого:", font, XBrushes.Black, new XRect(40, yPoint, page.Width, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            foreach (var group in groupedTasks)
            {
                gfx.DrawString($"{group.Description} - {group.TotalDuration.Hours} ч {group.TotalDuration.Minutes} мин", font, XBrushes.Black,
                    new XRect(40, yPoint, page.Width, page.Height), XStringFormats.TopLeft);
                yPoint += 20;

                if (yPoint > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Сохранение файла
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF Files|*.pdf";
            saveFileDialog.FileName = $"Отчет_{startDate:ddMMyyyy}_{endDate:ddMMyyyy}.pdf";

            if (saveFileDialog.ShowDialog() == true)
            {
                document.Save(saveFileDialog.FileName);
                MessageBox.Show("Отчет успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}