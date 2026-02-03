namespace XbrlProcessor.Configuration;

/// <summary>
/// Настройки приложения для обработки XBRL файлов
/// </summary>
public class XbrlSettings
    {
        /// <summary>
        /// Путь к папке с отчетами
        /// </summary>
        public string ReportsPath { get; set; } = "Reports";

        /// <summary>
        /// Паттерн для поиска файлов отчетов (glob)
        /// </summary>
        public string ReportFilePattern { get; set; } = "*.xbrl";

        /// <summary>
        /// Имя файла объединенного отчета
        /// </summary>
        public string MergedReportFileName { get; set; } = "merged_report.xbrl";

        /// <summary>
        /// Максимальное количество фактов для отображения в консоли
        /// </summary>
        public int MaxDisplayedFacts { get; set; } = 10;

        /// <summary>
        /// Формат даты для преобразования в строку
        /// </summary>
        public string DateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// Разделитель для создания сигнатуры контекста
        /// </summary>
        public string ContextSignatureSeparator { get; set; } = "||";

        /// <summary>
        /// Настройки обработки отчетов
        /// </summary>
        public ProcessingSettings Processing { get; set; } = new();

        /// <summary>
        /// Настройки XML пространств имен
        /// </summary>
        public XmlNamespacesSettings XmlNamespaces { get; set; } = new();

        /// <summary>
        /// Получить пути ко всем файлам отчетов (исключая merged)
        /// </summary>
        public string[] GetReportPaths()
        {
            var mergedPath = GetMergedReportPath();
            return Directory.GetFiles(ReportsPath, ReportFilePattern)
                .Where(f => !string.Equals(Path.GetFullPath(f), Path.GetFullPath(mergedPath), StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();
        }

        /// <summary>
        /// Получить полный путь к объединенному отчету
        /// </summary>
        public string GetMergedReportPath() => Path.Combine(ReportsPath, MergedReportFileName);
    }

    /// <summary>
    /// Настройки XML пространств имен для XBRL
    /// </summary>
    public record XmlNamespacesSettings
    {
        /// <summary>
        /// Пространство имен XBRL Instance
        /// </summary>
        public string Xbrli { get; init; } = "http://www.xbrl.org/2003/instance";

        /// <summary>
        /// Пространство имен XBRL Dimensions
        /// </summary>
        public string Xbrldi { get; init; } = "http://xbrl.org/2006/xbrldi";

        /// <summary>
        /// Пространство имен XBRL Linkbase
        /// </summary>
        public string Link { get; init; } = "http://www.xbrl.org/2003/linkbase";

        /// <summary>
        /// Пространство имен XLink
        /// </summary>
        public string Xlink { get; init; } = "http://www.w3.org/1999/xlink";

        /// <summary>
        /// Пространство имен измерений ЦБ РФ
        /// </summary>
        public string DimInt { get; init; } = "http://www.cbr.ru/xbrl/udr/dim/dim-int";

        /// <summary>
        /// Пространство имен словаря показателей ЦБ РФ
        /// </summary>
        public string PurcbDic { get; init; } = "http://www.cbr.ru/xbrl/nso/purcb/dic/purcb-dic";
    }

    /// <summary>
    /// Настройки обработки отчетов (параллелизм)
    /// </summary>
    public record ProcessingSettings
    {
        /// <summary>
        /// Степень параллелизма при обработке файлов.
        /// 1 = последовательно, >1 = параллельно, 0 = Environment.ProcessorCount.
        /// </summary>
        public int MaxDegreeOfParallelism { get; init; } = 1;
    }
