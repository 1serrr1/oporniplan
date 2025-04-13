using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для Ygl.xaml
    /// </summary>
    public partial class Ygl : Page
    {
        public Ygl()
        {
            InitializeComponent();
        }
        private void OnBuildPlanClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var supply = ParseInput(SupplyTextBox.Text);
                var demand = ParseInput(DemandTextBox.Text);
                var cost = ParseCostMatrix(CostTextBox.Text);

                int totalSupply = supply.Sum();
                int totalDemand = demand.Sum();

                if (totalSupply != totalDemand)
                {
                    if (totalSupply > totalDemand)
                    {
                        Array.Resize(ref demand, demand.Length + 1);
                        demand[demand.Length - 1] = totalSupply - totalDemand;

                        cost = ResizeCostMatrix(cost, cost.GetLength(0), cost.GetLength(1) + 1);
                    }
                    else
                    {
                        Array.Resize(ref supply, supply.Length + 1);
                        supply[supply.Length - 1] = totalDemand - totalSupply;

                        cost = ResizeCostMatrix(cost, cost.GetLength(0) + 1, cost.GetLength(1));
                    }
                }

                if (supply.Length != cost.GetLength(0) || demand.Length != cost.GetLength(1))
                {
                    MessageBox.Show("Размерность предложений и потребностей не соответствует размерности стоимости.");
                    return;
                }

                var result = GetNorthWestCornerPlan(supply, demand, cost);
                var totalCost = CalculateTotalCost(result, cost);

                // Обновляем TextBlock для отображения общей стоимости
                CostTextBlock.Text = $"Общая стоимость F = {totalCost}";

                var resultList = new List<ResultRow>();
                for (int i = 0; i < result.Count; i++)
                {
                    var row = new ResultRow
                    {
                        Quantities = string.Join(", ", result[i])
                    };
                    resultList.Add(row);
                }

                ResultDataGrid.ItemsSource = resultList;

                MessageBox.Show("Чтобы увидеть ответ, перейдите во вкладку Результаты", "Информация",
                       MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private int[] ParseInput(string input)
        {
            return input.Split(',').Select(int.Parse).ToArray();
        }

        private int[,] ParseCostMatrix(string input)
        {
            var rows = input.Split(';');
            var matrix = new int[rows.Length, rows[0].Split(',').Length];

            for (int i = 0; i < rows.Length; i++)
            {
                var cols = rows[i].Split(',');
                for (int j = 0; j < cols.Length; j++)
                {
                    matrix[i, j] = int.Parse(cols[j]);
                }
            }

            return matrix;
        }

        // Метод для изменения размера матрицы стоимости (добавление фиктивных строк или столбцов)
        private int[,] ResizeCostMatrix(int[,] cost, int newRowCount, int newColCount)
        {
            int[,] resizedMatrix = new int[newRowCount, newColCount];

            for (int i = 0; i < cost.GetLength(0); i++)
            {
                for (int j = 0; j < cost.GetLength(1); j++)
                {
                    resizedMatrix[i, j] = cost[i, j];
                }
            }

            for (int i = 0; i < resizedMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < resizedMatrix.GetLength(1); j++)
                {
                    if (i >= cost.GetLength(0) || j >= cost.GetLength(1))
                    {
                        resizedMatrix[i, j] = 0;
                    }
                }
            }

            return resizedMatrix;
        }

        // Алгоритм метода северо-западного угла
        private List<int[]> GetNorthWestCornerPlan(int[] supply, int[] demand, int[,] cost)
        {
            int m = supply.Length;
            int n = demand.Length;
            var plan = new List<int[]>(m);

            for (int i = 0; i < m; i++)
            {
                var row = new int[n];
                plan.Add(row);
            }

            int iSupply = 0, jDemand = 0;

            while (iSupply < m && jDemand < n)
            {
                int x = Math.Min(supply[iSupply], demand[jDemand]);
                plan[iSupply][jDemand] = x;

                supply[iSupply] -= x;
                demand[jDemand] -= x;

                if (supply[iSupply] == 0) iSupply++;
                if (demand[jDemand] == 0) jDemand++;
            }

            return plan;
        }

        // Метод для подсчета общей стоимости по опорному плану
        private int CalculateTotalCost(List<int[]> plan, int[,] cost)
        {
            int totalCost = 0;

            for (int i = 0; i < plan.Count; i++)
            {
                for (int j = 0; j < plan[i].Length; j++)
                {
                    totalCost += plan[i][j] * cost[i, j];
                }
            }

            return totalCost;
        }

        public class ResultRow
        {
            public string Quantities { get; set; }
        }
        //Очистка полей
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите очистить все поля?",
                               "Подтверждение очистки",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SupplyTextBox.Text = "";
                DemandTextBox.Text = "";
                CostTextBox.Text = "";

                ResultDataGrid.ItemsSource = null;
                CostTextBlock.Text = "";

                SupplyTextBox.Focus();
            }
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалоговое окно для выбора типа экспорта
                var exportDialog = new Window
                {
                    Title = "Экспорт результатов",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                var txtButton = new Button
                {
                    Content = "Экспорт в текстовый файл (TXT)",
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(5)
                };
                txtButton.Click += (s, args) =>
                {
                    ExportToTxt();
                    exportDialog.Close();
                };

                var excelButton = new Button
                {
                    Content = "Экспорт в Excel (XLSX)",
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(5)
                };
                excelButton.Click += (s, args) =>
                {
                    ExportToExcel();
                    exportDialog.Close();
                };

                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Выберите формат экспорта:",
                    Margin = new Thickness(0, 10, 0, 0)
                });
                stackPanel.Children.Add(txtButton);
                stackPanel.Children.Add(excelButton);

                exportDialog.Content = stackPanel;
                exportDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToTxt()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
                DefaultExt = "txt",
                FileName = "Результаты_СЗУ_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    // Записываем общую стоимость
                    writer.WriteLine(CostTextBlock.Text);
                    writer.WriteLine();

                    // Записываем заголовки столбцов (если есть)
                    if (ResultDataGrid.Columns.Count > 0)
                    {
                        var headers = ResultDataGrid.Columns
                            .Select(c => c.Header.ToString())
                            .ToArray();
                        writer.WriteLine(string.Join("\t", headers));
                    }

                    // Записываем данные
                    foreach (var item in ResultDataGrid.Items)
                    {
                        if (item is ResultRow row)
                        {
                            writer.WriteLine(row.Quantities);
                        }
                    }
                }

                MessageBox.Show("Данные успешно экспортированы в текстовый файл!", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportToExcel()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName = "Результаты_СЗУ_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook = excelApp.Workbooks.Add();
                Excel.Worksheet worksheet = workbook.ActiveSheet;

                try
                {
                    // Записываем общую стоимость
                    worksheet.Cells[1, 1] = CostTextBlock.Text;

                    // Записываем заголовки (если есть)
                    if (ResultDataGrid.Columns.Count > 0)
                    {
                        for (int i = 0; i < ResultDataGrid.Columns.Count; i++)
                        {
                            worksheet.Cells[3, i + 1] = ResultDataGrid.Columns[i].Header.ToString();
                        }
                    }

                    // Записываем данные
                    int rowIndex = 4;
                    foreach (var item in ResultDataGrid.Items)
                    {
                        if (item is ResultRow row)
                        {
                            var values = row.Quantities.Split(',');
                            for (int i = 0; i < values.Length; i++)
                            {
                                worksheet.Cells[rowIndex, i + 1] = values[i].Trim();
                            }
                            rowIndex++;
                        }
                    }

                    // Автоподбор ширины столбцов
                    worksheet.Columns.AutoFit();

                    // Сохраняем файл
                    workbook.SaveAs(saveFileDialog.FileName);
                    workbook.Close();
                    excelApp.Quit();

                    MessageBox.Show("Данные успешно экспортированы в Excel файл!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    // Освобождаем ресурсы
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                }
            }
        }
    }
}