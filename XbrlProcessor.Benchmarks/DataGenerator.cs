using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Benchmarks;

/// <summary>
/// Генератор тестовых данных в памяти для бенчмарков.
/// Создаёт Instance объекты без XML файлов.
/// </summary>
public static class DataGenerator
{
    private static readonly string[] DimensionNames =
    [
        "dim-int:Rek_kred_org_i_schetaTaxis",
        "dim-int:ID_sobstv_CZBTaxis",
        "dim-int:IDEmitentaTaxis",
        "dim-int:ID_strokiTaxis"
    ];

    private static readonly string[] ConceptNames =
    [
        "purcb-dic:Kod_Okato3", "purcb-dic:FIOEIO", "purcb-dic:DSKO_BB",
        "purcb-dic:DSKO_ObDt", "purcb-dic:DSKO_ObKt", "purcb-dic:CZB_Sobst_kol",
        "purcb-dic:INN_Prof_uch", "purcb-dic:OGRN_Prof_uch",
        "purcb-dic:Poln_Naim_Prof_uch", "purcb-dic:SokrNaim_Prof_uch"
    ];

    /// <summary>
    /// Генерирует один Instance с заданным числом контекстов и фактов.
    /// duplicatePercent — процент дублирующихся контекстов (0-100).
    /// </summary>
    public static Instance GenerateInstance(int contextCount, int factCount, int scenariosPerContext = 2, int duplicatePercent = 10)
    {
        var instance = new Instance();
        var rng = new Random(42);

        var uniqueContexts = (int)(contextCount * (1 - duplicatePercent / 100.0));
        if (uniqueContexts < 1) uniqueContexts = 1;

        for (var i = 0; i < contextCount; i++)
        {
            var sourceIndex = i < uniqueContexts ? i : rng.Next(uniqueContexts);
            instance.Contexts.Add(CreateContext($"C{i}", sourceIndex, scenariosPerContext));
        }

        instance.Units.Add(new Unit { Id = "RUB", Measure = "iso4217:RUB" });
        instance.Units.Add(new Unit { Id = "USD", Measure = "iso4217:USD" });
        instance.Units.Add(new Unit { Id = "pure", Measure = "xbrli-pure:pure" });

        for (var i = 0; i < factCount; i++)
        {
            var contextRef = $"C{i % contextCount}";
            var concept = ConceptNames[i % ConceptNames.Length];
            var isNumeric = concept.Contains("DSKO") || concept.Contains("CZB");

            instance.Facts.Add(new Fact
            {
                Id = $"F{i}",
                ConceptName = concept,
                ContextRef = contextRef,
                UnitRef = isNumeric ? "RUB" : null,
                Decimals = isNumeric ? 2 : null,
                Value = isNumeric
                    ? XbrlValue.Parse((rng.Next(1000, 9999999) + rng.Next(0, 100) / 100m).ToString("F2"))
                    : XbrlValue.Parse($"value_{i}")
            });
        }

        return instance;
    }

    /// <summary>
    /// Генерирует N инстансов с частичным пересечением данных (для GlobalCompare).
    /// </summary>
    public static List<(string Name, Instance Instance)> GenerateReports(int fileCount, int contextsPerFile, int factsPerFile, int overlapPercent = 30)
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var f = 0; f < fileCount; f++)
        {
            var instance = GenerateInstance(contextsPerFile, factsPerFile, duplicatePercent: 10);
            reports.Add(($"report{f + 1}.xbrl", instance));
        }
        return reports;
    }

    private static Context CreateContext(string id, int sourceIndex, int scenariosPerContext)
    {
        var context = new Context
        {
            Id = id,
            EntityValue = "1111111111111",
            EntityScheme = "http://www.cbr.ru",
            PeriodInstant = new DateTime(2019, 4, 30).AddDays(sourceIndex % 30),
        };

        for (var s = 0; s < scenariosPerContext; s++)
        {
            context.Scenarios.Add(new Scenario
            {
                DimensionType = s % 2 == 0 ? DimensionType.TypedMember : DimensionType.ExplicitMember,
                DimensionName = DimensionNames[(sourceIndex + s) % DimensionNames.Length],
                DimensionCode = s % 2 == 0 ? "dim-int:ID_YULTypedName" : null,
                DimensionValue = $"value_{sourceIndex}_{s}"
            });
        }

        return context;
    }
}
