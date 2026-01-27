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
        /// Имя файла первого отчета
        /// </summary>
        public string Report1FileName { get; set; } = "report1.xbrl";

        /// <summary>
        /// Имя файла второго отчета
        /// </summary>
        public string Report2FileName { get; set; } = "report2.xbrl";

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
        /// Настройки XML пространств имен
        /// </summary>
        public XmlNamespacesSettings XmlNamespaces { get; set; } = new();

        /// <summary>
        /// Настройки XPath запросов
        /// </summary>
        public XPathQueriesSettings XPathQueries { get; set; } = new();

        /// <summary>
        /// Получить полный путь к первому отчету
        /// </summary>
        /// <returns>Полный путь к файлу report1.xbrl</returns>
        public string GetReport1Path() => Path.Combine(ReportsPath, Report1FileName);

        /// <summary>
        /// Получить полный путь ко второму отчету
        /// </summary>
        /// <returns>Полный путь к файлу report2.xbrl</returns>
        public string GetReport2Path() => Path.Combine(ReportsPath, Report2FileName);

        /// <summary>
        /// Получить полный путь к объединенному отчету
        /// </summary>
        /// <returns>Полный путь к файлу merged_report.xbrl</returns>
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
    /// Настройки XPath запросов для поиска контекстов
    /// </summary>
    public record XPathQueriesSettings
    {
        /// <summary>
        /// XPath запрос для поиска контекстов с периодом instant = "2019-04-30"
        /// </summary>
        public string ContextsWithInstant { get; init; } = "//xbrli:context[xbrli:period/xbrli:instant='2019-04-30']/@id";

        /// <summary>
        /// XPath запрос для поиска контекстов со сценарием dimension="dim-int:ID_sobstv_CZBTaxis"
        /// </summary>
        public string ContextsWithDimension { get; init; } = "//xbrli:context[xbrli:scenario/xbrldi:typedMember[@dimension='dim-int:ID_sobstv_CZBTaxis']]/@id";

        /// <summary>
        /// XPath запрос для поиска контекстов без сценария
        /// </summary>
        public string ContextsWithoutScenario { get; init; } = "//xbrli:context[not(xbrli:scenario)]/@id";
    }
