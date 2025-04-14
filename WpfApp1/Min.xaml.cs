using System;
using System.Collections.Generic;
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


namespace WpfApp1
{
    public partial class Min : Page
    {
        public Min()
        {
            InitializeComponent();
        }

        private void OnBuildPlanClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Парсим данные из текстовых полей
                var supply = ParseInput(SupplyTextBox.Text);
                var demand = ParseInput(DemandTextBox.Text);
                var cost = ParseCostMatrix(CostTextBox.Text);

                // Проверка на балансировку задачи
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

                // Получаем план с использованием метода минимальных элементов
                var result = GetMinimumCostMethodPlan(supply, demand, cost);

                // Формируем список для отображения в DataGrid
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

                // Подсчитываем общую стоимость
                int totalCost = CalculateTotalCost(result, cost);

                // Отображаем общую стоимость в TextBlock
                CostTextBlock.Text = $"Общая стоимость: {totalCost}";

                MessageBox.Show("Чтобы увидеть ответ, перейдите во вкладку Результаты", "Информация",
                       MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Метод для парсинга строк ввода в массив
        private int[] ParseInput(string input)
        {
            return input.Split(',').Select(int.Parse).ToArray();
        }

        // Метод для парсинга матрицы стоимости
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

            // Копируем существующие значения в новую матрицу
            for (int i = 0; i < cost.GetLength(0); i++)
            {
                for (int j = 0; j < cost.GetLength(1); j++)
                {
                    resizedMatrix[i, j] = cost[i, j];
                }
            }

            // Заполняем фиктивные элементы в новой матрице
            
            for (int i = 0; i < resizedMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < resizedMatrix.GetLength(1); j++)
                {
                    if (i >= cost.GetLength(0) || j >= cost.GetLength(1))
                    {
                        resizedMatrix[i, j] = 0; // Фиктивная стоимость для фиктивных строк/столбцов
                    }
                }
            }

            return resizedMatrix;
        }

        // Алгоритм метода минимальных элементов
        private List<int[]> GetMinimumCostMethodPlan(int[] supply, int[] demand, int[,] cost)
        {
            int m = supply.Length;
            int n = demand.Length;
            var plan = new List<int[]>(m);

            // Инициализируем пустой план
            for (int i = 0; i < m; i++)
            {
                var row = new int[n];
                plan.Add(row);
            }

            // Создаем копии массивов, чтобы не изменять оригиналы
            int[] remainingSupply = (int[])supply.Clone();
            int[] remainingDemand = (int[])demand.Clone();

            while (remainingSupply.Sum() > 0 && remainingDemand.Sum() > 0)
            {
                // Шаг 1: Находим минимальную стоимость среди всех элементов
                int minCost = int.MaxValue;
                int minRow = -1, minCol = -1;

                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        // Ищем минимальную стоимость среди элементов, которые еще не выбраны
                        if (cost[i, j] < minCost && remainingSupply[i] > 0 && remainingDemand[j] > 0)
                        {
                            minCost = cost[i, j];
                            minRow = i;
                            minCol = j;
                        }
                    }
                }

                // Если не нашли минимальную стоимость (все элементы обработаны)
                if (minRow == -1 || minCol == -1)
                    break;

                // Назначаем товар
                int quantity = Math.Min(remainingSupply[minRow], remainingDemand[minCol]);
                plan[minRow][minCol] = quantity;

                // Обновляем оставшееся предложение и потребности
                remainingSupply[minRow] -= quantity;
                remainingDemand[minCol] -= quantity;
            }

            return plan;
        }

        // Метод для подсчета общей стоимости
        private int CalculateTotalCost(List<int[]> plan, int[,] cost)
        {
            int totalCost = 0;

            // Перебираем все строки и столбцы плана
            for (int i = 0; i < plan.Count; i++)
            {
                for (int j = 0; j < plan[i].Length; j++)
                {
                    if (plan[i][j] > 0) // если количество больше нуля, то учитываем в стоимости
                    {
                        totalCost += plan[i][j] * cost[i, j];  // Умножаем количество на стоимость
                    }
                }
            }

            return totalCost;
        }

        // Класс для отображения результата в DataGrid
        public class ResultRow
        {
            public string Quantities { get; set; }
        }
    }
}
