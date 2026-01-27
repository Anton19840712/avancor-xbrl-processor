using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands
{
    /// <summary>
    /// Команда для объединения двух XBRL отчетов
    /// </summary>
    public class MergeReportsCommand : IXbrlCommand
    {
        #region Fields

        private readonly Instance _instance1;
        private readonly Instance _instance2;
        private readonly XbrlMerger _merger;
        private readonly string _mergedPath;
        private readonly string _templatePath;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор команды объединения отчетов
        /// </summary>
        /// <param name="instance1">Первый отчет</param>
        /// <param name="instance2">Второй отчет</param>
        /// <param name="merger">Сервис объединения XBRL</param>
        /// <param name="mergedPath">Путь для сохранения объединенного отчета</param>
        /// <param name="templatePath">Путь к файлу шаблона</param>
        public MergeReportsCommand(Instance instance1, Instance instance2, XbrlMerger merger,
            string mergedPath, string templatePath)
        {
            _instance1 = instance1;
            _instance2 = instance2;
            _merger = merger;
            _mergedPath = mergedPath;
            _templatePath = templatePath;
        }

        #endregion

        #region IXbrlCommand Implementation

        public void Execute()
        {
            Console.WriteLine("\n\n=== Задание 2: Объединение отчетов ===\n");

            var mergedInstance = _merger.MergeInstances(_instance1, _instance2);
            Console.WriteLine($"Объединенный отчет: {mergedInstance.Contexts.Count} контекстов, {mergedInstance.Units.Count} единиц, {mergedInstance.Facts.Count} фактов");

            _merger.SaveToXbrl(mergedInstance, _mergedPath, _templatePath);
            Console.WriteLine($"Объединенный отчет сохранен: {_mergedPath}");
        }

        public string GetName() => "MergeReports";

        public string GetDescription() => "Объединение двух XBRL отчетов с удалением дубликатов";

        #endregion
    }
}
