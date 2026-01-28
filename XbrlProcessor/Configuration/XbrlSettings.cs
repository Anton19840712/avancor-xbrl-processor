namespace XbrlProcessor.Configuration
{
    public class XbrlSettings
    {
        public string ReportsPath { get; set; } = "Reports";
        public string Report1FileName { get; set; } = "report1.xbrl";
        public string Report2FileName { get; set; } = "report2.xbrl";
        public string MergedReportFileName { get; set; } = "merged_report.xbrl";
        public int MaxDisplayedFacts { get; set; } = 10;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string ContextSignatureSeparator { get; set; } = "||";
        public XmlNamespacesSettings XmlNamespaces { get; set; } = new();
        public XPathQueriesSettings XPathQueries { get; set; } = new();

        public string GetReport1Path() => Path.Combine(ReportsPath, Report1FileName);
        public string GetReport2Path() => Path.Combine(ReportsPath, Report2FileName);
        public string GetMergedReportPath() => Path.Combine(ReportsPath, MergedReportFileName);
    }

    public class XmlNamespacesSettings
    {
        public string Xbrli { get; set; } = "http://www.xbrl.org/2003/instance";
        public string Xbrldi { get; set; } = "http://xbrl.org/2006/xbrldi";
        public string Link { get; set; } = "http://www.xbrl.org/2003/linkbase";
        public string Xlink { get; set; } = "http://www.w3.org/1999/xlink";
        public string DimInt { get; set; } = "http://www.cbr.ru/xbrl/udr/dim/dim-int";
        public string PurcbDic { get; set; } = "http://www.cbr.ru/xbrl/nso/purcb/dic/purcb-dic";
    }

    public class XPathQueriesSettings
    {
        public string ContextsWithInstant { get; set; } = "//xbrli:context[xbrli:period/xbrli:instant='2019-04-30']/@id";
        public string ContextsWithDimension { get; set; } = "//xbrli:context[xbrli:scenario/xbrldi:typedMember[@dimension='dim-int:ID_sobstv_CZBTaxis']]/@id";
        public string ContextsWithoutScenario { get; set; } = "//xbrli:context[not(xbrli:scenario)]/@id";
    }
}
