using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using OfflineTimeTracker.Service;
using MaterialDesignThemes.Wpf;
using MessageBox = System.Windows.MessageBox;

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

        private NotifyIcon notifyIcon;


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

            // Инициализация NotifyIcon
            InitializeNotifyIcon();

            // Подписываемся на событие изменения состояния окна
            this.StateChanged += MainWindow_StateChanged;

            // Обновляем ComboBox
            UpdateTaskComboBox();
            UpdateProjectComboBox();
        }

        private void UpdateTaskComboBox()
        {
            DateTime now = DateTime.Now.Date;
            DateTime sevenDaysAgo = now.AddDays(-7);

            var tasksLastSevenDays = taskEntries
                .Where(task => task.StartTime.Date >= sevenDaysAgo && task.StartTime.Date <= now)
                .GroupBy(task => task.Description)
                .Select(g => g.OrderByDescending(task => task.StartTime).First())
                .OrderByDescending(task => task.StartTime)
                .ToList();

            TaskComboBox.ItemsSource = tasksLastSevenDays;
            TaskComboBox.DisplayMemberPath = "Description";
        }

        private void UpdateProjectComboBox()
        {
            DateTime now = DateTime.Now.Date;
            DateTime sevenDaysAgo = now.AddDays(-7);

            var projectsLastSevenDays = taskEntries
                .Where(task => task.StartTime.Date >= sevenDaysAgo && task.StartTime.Date <= now)
                .Where(task => !string.IsNullOrEmpty(task.ProjectDescription))
                .GroupBy(task => task.ProjectDescription)
                .Select(g => g.OrderByDescending(task => task.StartTime).First())
                .OrderByDescending(task => task.StartTime)
                .ToList();

            ProjectComboBox.ItemsSource = projectsLastSevenDays;
            ProjectComboBox.DisplayMemberPath = "ProjectDescription";
        }


        private void TaskComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskComboBox.SelectedItem is TaskEntry selectedTask)
            {
                TaskDescription.Text = selectedTask.Description;
                // Также устанавливаем ProjectDescription, если оно есть
                if (!string.IsNullOrEmpty(selectedTask.ProjectDescription))
                {
                    ProjectDescription.Text = selectedTask.ProjectDescription;
                }
            }
        }


        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem is TaskEntry selectedProject)
            {
                ProjectDescription.Text = selectedProject.ProjectDescription;
            }
        }

        private void UpdateTrayIconTooltip()
        {
            if (isTracking)
            {
                // Получаем название текущей задачи
                string currentTask = TaskDescription.Text;
                // Форматируем время
                string formattedTime = elapsedTime.ToString(@"hh\:mm\:ss");
                // Устанавливаем Tooltip с использованием перевода строки
                notifyIcon.Text = $"OfflineTimeTracker\n\n{currentTask} - {formattedTime}";
            }
            else
            {
                // Устанавливаем Tooltip без запущенной задачи
                notifyIcon.Text = "OfflineTimeTracker\n\nЗадача не запущена";
            }
        }


        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/time50.ico");

            if (File.Exists(iconPath))
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // Если иконка не найдена, используем стандартную
                notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                MessageBox.Show("Иконка не найдена в папке приложения. Будет использована стандартная иконка.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            notifyIcon.Visible = false;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Создание контекстного меню
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // Создание пунктов меню
            ToolStripMenuItem showItem = new ToolStripMenuItem("Показать");
            showItem.Click += ShowItem_Click;

            ToolStripMenuItem stopTaskItem = new ToolStripMenuItem("Остановить задачу");
            stopTaskItem.Click += StopTaskItem_Click;

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += ExitItem_Click;

            // Добавление пунктов в контекстное меню
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(stopTaskItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            // Привязка контекстного меню к NotifyIcon
            notifyIcon.ContextMenuStrip = contextMenu;

            // Устанавливаем начальный Tooltip
            UpdateTrayIconTooltip();
        }


        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                notifyIcon.Visible = false;
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowItem_Click(sender, e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnClosing(e);
        }

        private void ShowItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            notifyIcon.Visible = false;
        }

        private void StopTaskItem_Click(object sender, EventArgs e)
        {
            // Проверяем, идет ли сейчас отслеживание задачи
            if (isTracking)
            {
                StopButton_Click(null, null);
                MessageBox.Show("Задача остановлена.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нет активной задачи для остановки.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            // Закрываем приложение
            System.Windows.Application.Current.Shutdown();
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
                    UpdateTaskComboBox();
                    UpdateProjectComboBox();
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
                    UpdateTaskComboBox();
                    UpdateProjectComboBox();
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

            // Обновляем Tooltip в трее
            UpdateTrayIconTooltip();
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
                TaskComboBox.IsEnabled = false;
                ProjectDescription.IsEnabled = false;
                ProjectComboBox.IsEnabled = false;

                elapsedTime = TimeSpan.Zero;
                TimerTextBlock.Text = "00:00:00";
                dispatcherTimer.Start();

                // Обновляем Tooltip
                UpdateTrayIconTooltip();
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
                    ProjectDescription = string.IsNullOrWhiteSpace(ProjectDescription.Text) ? null : ProjectDescription.Text,
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
                TaskComboBox.SelectedIndex = -1;
                TaskComboBox.IsEnabled = true;
                ProjectDescription.Text = string.Empty;
                ProjectDescription.IsEnabled = true;
                ProjectComboBox.SelectedIndex = -1;
                ProjectComboBox.IsEnabled = true;

                TimerTextBlock.Text = "00:00:00";

                UpdateTaskListView();

                // Обновляем Tooltip
                UpdateTrayIconTooltip();
            }
        }


        private void SaveTasks()
        {
            var json = JsonConvert.SerializeObject(taskEntries, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(dataFile, json);

            UpdateTaskComboBox();
            UpdateProjectComboBox();
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
            UpdateTaskComboBox();
            UpdateProjectComboBox();
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

            var startDate = (DateTime)result.GetType().GetProperty("StartDate").GetValue(result);
            var endDate = (DateTime)result.GetType().GetProperty("EndDate").GetValue(result);

            GeneratePDFReport(startDate, endDate);
        }


        private void GeneratePDFReport(DateTime startDate, DateTime endDate)
        {
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
            XFont headerFont = new XFont("Verdana", 12, XFontStyleEx.Bold);

            double yPoint = 40;
            double leftMargin = 40;
            double rightMargin = 40;
            double tableWidth = page.Width - leftMargin - rightMargin;

            // Заголовок
            gfx.DrawString($"Отчет за период {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}", new XFont("Verdana", 14, XFontStyleEx.Bold), XBrushes.Black,
                new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
            yPoint += 40;

            // Заголовок таблицы задач
            gfx.DrawString("Список задач:", font, XBrushes.Black, new XRect(leftMargin, yPoint, tableWidth, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            // Размеры столбцов
            double col1Width = 300; // Ширина столбца "Описание задачи"
            double col2Width = 200; // Ширина столбца "Дата и время"

            // Внутренние отступы внутри ячеек
            double cellPadding = 5;

            // Заголовок таблицы
            // Рисуем границы заголовка
            gfx.DrawRectangle(XPens.Black, leftMargin, yPoint, col1Width, 20);
            gfx.DrawRectangle(XPens.Black, leftMargin + col1Width, yPoint, col2Width, 20);

            // Отображаем заголовки столбцов с отступами
            gfx.DrawString("Задача", headerFont, XBrushes.Black, new XRect(leftMargin + cellPadding, yPoint + cellPadding, col1Width - 2 * cellPadding, 20 - 2 * cellPadding), XStringFormats.TopLeft);
            gfx.DrawString("Дата и время", headerFont, XBrushes.Black, new XRect(leftMargin + col1Width + cellPadding, yPoint + cellPadding, col2Width - 2 * cellPadding, 20 - 2 * cellPadding), XStringFormats.TopLeft);
            yPoint += 20;

            // Данные таблицы задач
            foreach (var task in tasksInPeriod)
            {
                string taskDescription = task.Description;
                if (!string.IsNullOrWhiteSpace(task.ProjectDescription))
                {
                    taskDescription += $" ({task.ProjectDescription})";
                }

                string dateTimeInfo = $"{task.StartTime:dd.MM.yyyy} {task.StartTime:HH:mm} - {task.EndTime:HH:mm}";

                // Разбиваем текст, если он длиннее 55 символов
                string[] lines = SplitText(taskDescription, 55);
                double lineHeight = font.GetHeight();
                double cellHeight = lines.Length * lineHeight + 2 * cellPadding;

                // Рисуем границы ячеек с учетом высоты
                gfx.DrawRectangle(XPens.Black, leftMargin, yPoint, col1Width, cellHeight);
                gfx.DrawRectangle(XPens.Black, leftMargin + col1Width, yPoint, col2Width, cellHeight);

                // Отображаем текст с переносом и отступами
                for (int i = 0; i < lines.Length; i++)
                {
                    gfx.DrawString(lines[i], font, XBrushes.Black, new XRect(leftMargin + cellPadding, yPoint + cellPadding + i * lineHeight, col1Width - 2 * cellPadding, lineHeight), XStringFormats.TopLeft);
                }

                // Отображаем дату и время с отступами
                gfx.DrawString(dateTimeInfo, font, XBrushes.Black, new XRect(leftMargin + col1Width + cellPadding, yPoint + cellPadding, col2Width - 2 * cellPadding, cellHeight - 2 * cellPadding), XStringFormats.TopLeft);

                yPoint += cellHeight;

                // Проверка на переход на новую страницу
                if (yPoint > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Итоговое время по задачам
            yPoint += 20;
            gfx.DrawString("Итого по задачам:", headerFont, XBrushes.Black, new XRect(leftMargin, yPoint, tableWidth, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            foreach (var group in groupedTasks)
            {
                gfx.DrawString($"{group.Description} - {group.TotalDuration.Hours} ч {group.TotalDuration.Minutes} мин", font, XBrushes.Black,
                    new XRect(leftMargin + cellPadding, yPoint + cellPadding, tableWidth - 2 * cellPadding, font.GetHeight()), XStringFormats.TopLeft);
                yPoint += font.GetHeight() + cellPadding;

                if (yPoint > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Время по проектам
            yPoint += 20;
            gfx.DrawString("Время по проектам:", headerFont, XBrushes.Black, new XRect(leftMargin, yPoint, tableWidth, page.Height), XStringFormats.TopLeft);
            yPoint += 20;

            var groupedProjects = tasksInPeriod
                .Where(task => !string.IsNullOrWhiteSpace(task.ProjectDescription))
                .GroupBy(task => task.ProjectDescription)
                .Select(group => new
                {
                    ProjectDescription = group.Key,
                    TotalDuration = group.Aggregate(TimeSpan.Zero, (sum, task) => sum.Add(task.Duration))
                })
                .OrderByDescending(group => group.TotalDuration)
                .ToList();

            foreach (var group in groupedProjects)
            {
                gfx.DrawString($"{group.ProjectDescription} - {group.TotalDuration.Hours} ч {group.TotalDuration.Minutes} мин", font, XBrushes.Black,
                    new XRect(leftMargin + cellPadding, yPoint + cellPadding, tableWidth - 2 * cellPadding, font.GetHeight()), XStringFormats.TopLeft);
                yPoint += font.GetHeight() + cellPadding;

                if (yPoint > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // Задачи без проекта
            var tasksWithoutProject = tasksInPeriod
                .Where(task => string.IsNullOrWhiteSpace(task.ProjectDescription))
                .ToList();

            if (tasksWithoutProject.Any())
            {
                TimeSpan totalDuration = tasksWithoutProject.Aggregate(TimeSpan.Zero, (sum, task) => sum.Add(task.Duration));
                gfx.DrawString($"Без проекта - {totalDuration.Hours} ч {totalDuration.Minutes} мин", font, XBrushes.Black,
                    new XRect(leftMargin + cellPadding, yPoint + cellPadding, tableWidth - 2 * cellPadding, font.GetHeight()), XStringFormats.TopLeft);
                yPoint += font.GetHeight() + cellPadding;
            }

            // Сохранение файла
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "PDF Files|*.pdf";
            saveFileDialog.FileName = $"Отчет_{startDate:ddMMyyyy}_{endDate:ddMMyyyy}.pdf";

            if (saveFileDialog.ShowDialog() == true)
            {
                document.Save(saveFileDialog.FileName);
                MessageBox.Show("Отчет успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



        private string[] SplitText(string text, int maxLineLength)
        {
            List<string> lines = new List<string>();

            while (text.Length > 0)
            {
                int lineLength = text.Length > maxLineLength ? maxLineLength : text.Length;
                string line = text.Substring(0, lineLength);

                // Ищем последнее пробел для переноса
                int lastSpace = line.LastIndexOf(' ');
                if (lastSpace > 0 && lineLength == maxLineLength)
                {
                    line = line.Substring(0, lastSpace);
                    lineLength = lastSpace + 1;
                }

                lines.Add(line);
                text = text.Substring(lineLength);
            }

            return lines.ToArray();
        }


    }
}