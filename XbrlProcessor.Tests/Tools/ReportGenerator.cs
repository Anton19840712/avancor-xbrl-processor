using System.Xml.Linq;

namespace XbrlProcessor.Tests.Tools;

public class ReportGenerator
{
    private static readonly XNamespace Xbrli = "http://www.xbrl.org/2003/instance";
    private static readonly XNamespace Xbrldi = "http://xbrl.org/2006/xbrldi";
    private static readonly XNamespace Link = "http://www.xbrl.org/2003/linkbase";
    private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";
    private static readonly XNamespace DimInt = "http://www.cbr.ru/xbrl/udr/dim/dim-int";
    private static readonly XNamespace MemInt = "http://www.cbr.ru/xbrl/udr/dom/mem-int";
    private static readonly XNamespace PurcbDic = "http://www.cbr.ru/xbrl/nso/purcb/dic/purcb-dic";
    private static readonly XNamespace Iso4217 = "http://www.xbrl.org/2003/iso4217";
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private static readonly string[] Entities =
    [
        "1111111111111", "2222222222222", "3333333333333", "4444444444444",
        "5555555555555", "6666666666666", "7777777777777", "8888888888888",
        "9999999999999", "1234567890123"
    ];

    private static readonly string[] Currencies = ["RUB", "USD", "EUR"];

    private static readonly string[] FactNames =
    [
        "Kod_Okato3", "FIOEIO", "Dolzgnostlizapodpotchetnost", "FIOEIOkontr",
        "INN_Prof_uch", "OGRN_Prof_uch", "Poln_Naim_Prof_uch", "SokrNaim_Prof_uch",
        "AdresPocht_Prof_uch", "Nomer_scheta_Rek_kred_org_i_scheta",
        "Naim_Rek_kred_org_i_scheta", "INN_TIN_reg_nomer_Rek_kred_org_i_scheta",
        "DSKO_BB", "DSKO_ObDt", "DSKO_ObKt", "CZB_Sobst_kol",
        "Naim_emitenta_F_I_O_vekseledatelya_Rekv_em", "INN_Rekv_em",
        "KPP_emitenta_Rekv_em", "OGRN_emitenta_Rekv_em",
        "GRN_vypuska_czennyx_bumag_Rekv_Emis_CZen_Bumag",
        "ISIN_Rekv_Emis_CZen_Bumag", "Nominalnaya_stoimost_Rekv_Emis_CZen_Bumag",
        "Naim_org_Sved_ob_org_ved_uchet_prav_czb",
        "INN_ili_TIN_Sved_ob_org_ved_uchet_prav_czb",
        "KPP_Sved_ob_org_ved_uchet_prav_czb", "OGRN_Sved_ob_org_ved_uchet_prav_czb"
    ];

    private static readonly string[] NumericFactNames =
    [
        "DSKO_BB", "DSKO_ObDt", "DSKO_ObKt", "CZB_Sobst_kol",
        "Nominalnaya_stoimost_Rekv_Emis_CZen_Bumag"
    ];

    private static readonly string[] EnumeratorFactNames =
    [
        "Vid_scheta_v_kreditnoj_organizaczii_Enumerator",
        "OKSM_Kred_Org_Enumerator",
        "Valyuta_Rekv_kreditn_org_i_schetaEnumerator",
        "Vozm_isp_den_sred_naxod_na_schete_v_sobst_interesaxEnumerator",
        "TipCZenBum_VidFinInstrEnumerator",
        "Kod_strany_registraczii_Rekvizity_emitentaEnumerator",
        "ValyutaEnumerator",
        "Priznak_org_ved_uchet_prav_org_na_czennye_bumagiEnumerator"
    ];

    private static readonly string[] EnumeratorValues =
    [
        "mem-int:Schet_doveritelnogo_upravleniyaMember",
        "mem-int:Strana_643RusRossiyaMember",
        "mem-int:Valyuta_643RubRossijskijRublMember",
        "mem-int:NetMember",
        "mem-int:SHS1_akcziiKOrezidentov_obyknovennyeMember",
        "mem-int:R_registratorMember",
        "mem-int:Inoe_Rek_kred_org_i_schMember",
        "mem-int:NaSchetDUMember",
        "mem-int:ItogoNPMember"
    ];

    private static readonly string[] DateFactNames =
    [
        "Data_otkrytiya_scheta_Rek_kred_org_i_scheta"
    ];

    private static readonly string[] TypedDimensions =
    [
        "dim-int:Rek_kred_org_i_schetaTaxis",
        "dim-int:ID_sobstv_CZBTaxis",
        "dim-int:IDEmitentaTaxis",
        "dim-int:ID_strokiTaxis"
    ];

    private static readonly Dictionary<string, string> TypedDimensionCodes = new()
    {
        ["dim-int:Rek_kred_org_i_schetaTaxis"] = "dim-int:ID_YULTypedName",
        ["dim-int:ID_sobstv_CZBTaxis"] = "dim-int:ID_CZBTypedname",
        ["dim-int:IDEmitentaTaxis"] = "dim-int:ID_YULTypedName",
        ["dim-int:ID_strokiTaxis"] = "dim-int:ID_strokiTypedname"
    };

    private static readonly string[] ExplicitDimensions =
    [
        "dim-int:Detaliz_kolva_czenBum_naPravSobstvAxis",
        "dim-int:CZenBumUchityvNaInyxSchetAxis"
    ];

    private static readonly string[] ExplicitMemberValues =
    [
        "mem-int:ItogoNPMember",
        "mem-int:NaSchetDUMember"
    ];

    private readonly Random _random = new(42); // fixed seed for reproducibility

    [Fact]
    public void GenerateTestReports()
    {
        var reportsDir = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..",
            "XbrlProcessor", "Reports");

        Directory.CreateDirectory(reportsDir);

        for (var i = 1; i <= 50; i++)
        {
            var (contextCount, factCount) = GetSizeParams(i);
            var doc = GenerateReport(i, contextCount, factCount);
            var filePath = Path.Combine(reportsDir, $"report{i + 2:D2}.xbrl");
            doc.Save(filePath);
        }

        // Verify files were created
        var files = Enumerable.Range(3, 50)
            .Select(n => Path.Combine(reportsDir, $"report{n:D2}.xbrl"))
            .Where(File.Exists)
            .ToArray();
        Assert.Equal(50, files.Length);

        // Verify size distribution
        foreach (var file in files.OrderBy(f => f))
        {
            var size = new FileInfo(file).Length;
            Assert.True(size > 0, $"File {Path.GetFileName(file)} is empty");
        }
    }

    [Fact]
    public void GenerateHeavyReports()
    {
        var reportsDir = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..",
            "XbrlProcessor", "Reports");

        Directory.CreateDirectory(reportsDir);

        // Heavy files: 53-62 (indices 51-60), 100-500 contexts, 1000-5000 facts
        for (var i = 51; i <= 60; i++)
        {
            var (contextCount, factCount) = GetSizeParams(i);
            var doc = GenerateReport(i, contextCount, factCount);
            var filePath = Path.Combine(reportsDir, $"report{i + 2:D2}.xbrl");
            doc.Save(filePath);
        }

        var files = Enumerable.Range(53, 10)
            .Select(n => Path.Combine(reportsDir, $"report{n:D2}.xbrl"))
            .Where(File.Exists)
            .ToArray();
        Assert.Equal(10, files.Length);

        foreach (var file in files.OrderBy(f => f))
        {
            var size = new FileInfo(file).Length;
            Assert.True(size > 100_000, $"File {Path.GetFileName(file)} should be >100KB, got {size / 1024}KB");
        }
    }

    private static (int contexts, int facts) GetSizeParams(int index)
    {
        return index switch
        {
            <= 15 => (3 + (index - 1) % 3, 5 + (index - 1) * 1),        // Small: 3-5 contexts, 5-19 facts
            <= 35 => (8 + (index - 16) % 13, 20 + (index - 16) * 3),     // Medium: 8-20 contexts, 20-77 facts
            <= 50 => (20 + (index - 36) * 2, 80 + (index - 36) * 15),    // Large: 20-48 contexts, 80-290 facts
            _     => (100 + (index - 51) * 45, 1000 + (index - 51) * 450) // Heavy: 100-505 contexts, 1000-5050 facts
        };
    }

    private XDocument GenerateReport(int index, int contextCount, int factCount)
    {
        var entity = Entities[index % Entities.Length];
        var currency = Currencies[index % Currencies.Length];
        var year = 2019 + (index % 6); // 2019-2024

        // For duplicate testing: files 14-15 are near-duplicates of 1-2
        bool isDuplicate = index is 14 or 15;
        int sourceIndex = isDuplicate ? index - 13 : index;

        if (isDuplicate)
        {
            entity = Entities[(sourceIndex) % Entities.Length];
            year = 2019 + (sourceIndex % 6);
        }

        var root = new XElement(Xbrli + "xbrl",
            new XAttribute(XNamespace.Xmlns + "dim-int", DimInt.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "iso4217", Iso4217.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "link", Link.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "mem-int", MemInt.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "purcb-dic", PurcbDic.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xbrldi", Xbrldi.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xbrli", Xbrli.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xlink", Xlink.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xs", Xs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName));

        root.Add(new XElement(Link + "schemaRef",
            new XAttribute(Xlink + "type", "simple"),
            new XAttribute(Xlink + "href",
                "http://www.cbr.ru/xbrl/nso/purcb/rep/2018-03-31/ep/ep_nso_purcb_m_10d_ex_reestr_0420417.xsd")));

        // Generate contexts
        var contextIds = new List<string>();
        for (var c = 0; c < contextCount; c++)
        {
            var contextId = $"A{c}";
            contextIds.Add(contextId);
            root.Add(GenerateContext(contextId, entity, year, c, index));
        }

        // Generate units
        root.Add(GenerateUnit(currency));
        root.Add(GenerateUnit("pure", isPure: true));
        if (index > 20) // Medium/Large files get extra currency units
        {
            foreach (var cur in Currencies.Where(c => c != currency))
                root.Add(GenerateUnit(cur));
        }

        // Generate facts
        var factIndex = 0;
        for (var f = 0; f < factCount; f++)
        {
            factIndex++;
            var contextRef = contextIds[f % contextIds.Count];
            root.Add(GenerateFact(index, factIndex, contextRef, currency, isDuplicate, year));
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XComment("Generated by ReportGenerator"),
            root);

        return doc;
    }

    private XElement GenerateContext(string id, string entity, int year, int contextIndex, int fileIndex)
    {
        var context = new XElement(Xbrli + "context", new XAttribute("id", id));

        context.Add(new XElement(Xbrli + "entity",
            new XElement(Xbrli + "identifier",
                new XAttribute("scheme", "http://www.cbr.ru"),
                entity)));

        var period = new XElement(Xbrli + "period");
        if (contextIndex == 0)
        {
            // First context is always instant (base period)
            period.Add(new XElement(Xbrli + "instant", $"{year}-04-30"));
        }
        else if (contextIndex % 4 == 3)
        {
            // Every 4th context is duration
            var month = 1 + (contextIndex % 12);
            var endMonth = Math.Min(month + 2, 12);
            period.Add(new XElement(Xbrli + "startDate", $"{year}-{month:D2}-01"));
            period.Add(new XElement(Xbrli + "endDate", $"{year}-{endMonth:D2}-28"));
        }
        else
        {
            // Rest are instant with varying dates
            var month = 1 + (contextIndex % 12);
            var day = Math.Min(28, 15 + contextIndex);
            period.Add(new XElement(Xbrli + "instant", $"{year}-{month:D2}-{day:D2}"));
        }
        context.Add(period);

        // Add scenario for contexts after the first
        if (contextIndex > 0)
        {
            var scenario = new XElement(Xbrli + "scenario");
            var dimCount = 1 + (contextIndex % 3); // 1-3 dimensions per scenario

            for (var d = 0; d < dimCount && d < TypedDimensions.Length; d++)
            {
                var dim = TypedDimensions[(contextIndex + d) % TypedDimensions.Length];
                var code = TypedDimensionCodes[dim];
                var value = GenerateTypedValue(dim, fileIndex, contextIndex);

                // Parse prefixed element name for XElement creation
                var codeParts = code.Split(':');
                var codeNs = codeParts[0] == "dim-int" ? DimInt : PurcbDic;
                var codeLocal = codeParts[1];

                var dimParts = dim.Split(':');

                scenario.Add(new XElement(Xbrldi + "typedMember",
                    new XAttribute("dimension", dim),
                    new XElement(codeNs + codeLocal, value)));
            }

            // Some contexts also get explicit members
            if (contextIndex % 3 == 0)
            {
                var explDim = ExplicitDimensions[contextIndex % ExplicitDimensions.Length];
                var explVal = ExplicitMemberValues[contextIndex % ExplicitMemberValues.Length];
                scenario.Add(new XElement(Xbrldi + "explicitMember",
                    new XAttribute("dimension", explDim),
                    explVal));
            }

            context.Add(scenario);
        }

        return context;
    }

    private static string GenerateTypedValue(string dimension, int fileIndex, int contextIndex)
    {
        return dimension switch
        {
            "dim-int:Rek_kred_org_i_schetaTaxis"
                => $"id10277001{fileIndex:D5}_{contextIndex:D20}",
            "dim-int:ID_sobstv_CZBTaxis"
                => $"idRU000A0ZZY{(char)('A' + (fileIndex + contextIndex) % 26)}{contextIndex}",
            "dim-int:IDEmitentaTaxis"
                => $"id10277390{fileIndex:D4}",
            "dim-int:ID_strokiTaxis"
                => contextIndex % 2 == 0 ? "НП" : "ОС",
            _ => $"value_{fileIndex}_{contextIndex}"
        };
    }

    private static XElement GenerateUnit(string currency, bool isPure = false)
    {
        if (isPure)
        {
            return new XElement(Xbrli + "unit",
                new XAttribute("id", "pure"),
                new XElement(Xbrli + "measure", "xbrli-pure:pure"));
        }

        return new XElement(Xbrli + "unit",
            new XAttribute("id", currency),
            new XElement(Xbrli + "measure", $"iso4217:{currency}"));
    }

    private XElement GenerateFact(int fileIndex, int factIndex, string contextRef,
        string currency, bool isDuplicate, int year)
    {
        var factId = $"V{fileIndex}_f{factIndex}";

        // Cycle through different fact types
        var totalFactTypes = FactNames.Length + EnumeratorFactNames.Length + DateFactNames.Length;
        var typeIndex = (factIndex - 1) % totalFactTypes;

        if (typeIndex < FactNames.Length)
        {
            var factName = FactNames[typeIndex];
            var isNumeric = NumericFactNames.Contains(factName);

            var element = new XElement(PurcbDic + factName,
                new XAttribute("id", factId + "purcb-dic_" + factName),
                new XAttribute("contextRef", contextRef));

            if (isNumeric)
            {
                var decimals = factName == "CZB_Sobst_kol" ? 0 : 2;
                var unitRef = factName == "CZB_Sobst_kol" ? "pure" : currency;
                element.Add(new XAttribute("decimals", decimals));
                element.Add(new XAttribute("unitRef", unitRef));

                var baseValue = (decimal)(_random.Next(1000, 9999999)) + _random.Next(0, 100) / 100m;
                // For duplicates, use slightly different values
                if (isDuplicate)
                    baseValue += 0.01m;
                element.Value = baseValue.ToString("F" + decimals);
            }
            else
            {
                element.Value = GenerateStringValue(factName, fileIndex, factIndex);
            }

            return element;
        }

        typeIndex -= FactNames.Length;
        if (typeIndex < EnumeratorFactNames.Length)
        {
            var factName = EnumeratorFactNames[typeIndex];
            var enumValue = EnumeratorValues[(factIndex + fileIndex) % EnumeratorValues.Length];

            return new XElement(PurcbDic + factName,
                new XAttribute("contextRef", contextRef),
                new XAttribute("id", factId + "purcb-dic_" + factName),
                enumValue);
        }

        // Date facts
        {
            var factName = DateFactNames[(typeIndex - EnumeratorFactNames.Length) % DateFactNames.Length];
            var month = 1 + (factIndex % 12);
            var day = 1 + (factIndex % 28);

            return new XElement(PurcbDic + factName,
                new XAttribute("contextRef", contextRef),
                new XAttribute("id", factId + "purcb-dic_" + factName),
                $"{year}-{month:D2}-{day:D2}");
        }
    }

    private static string GenerateStringValue(string factName, int fileIndex, int factIndex)
    {
        return factName switch
        {
            "Kod_Okato3" => $"4528659{fileIndex:D4}",
            "FIOEIO" => $"Иванов Иван {fileIndex}",
            "Dolzgnostlizapodpotchetnost" => "Генеральный директор",
            "FIOEIOkontr" => $"Иванова Ксения {fileIndex}",
            "INN_Prof_uch" => $"77{fileIndex:D8}",
            "OGRN_Prof_uch" => $"{fileIndex:D13}",
            "Poln_Naim_Prof_uch" => $"ООО \"Компания {fileIndex}\"",
            "SokrNaim_Prof_uch" => $"ООО \"К{fileIndex}\"",
            "AdresPocht_Prof_uch" => $"1{fileIndex:D5}, г. Москва, ул. Тестовая, д.{factIndex}",
            "Nomer_scheta_Rek_kred_org_i_scheta" => $"{fileIndex:D20}",
            "Naim_Rek_kred_org_i_scheta" => $"ПАО Банк {fileIndex}",
            "INN_TIN_reg_nomer_Rek_kred_org_i_scheta" => $"04452{fileIndex:D4}",
            "Naim_emitenta_F_I_O_vekseledatelya_Rekv_em" => $"Эмитент {fileIndex}",
            "INN_Rekv_em" => $"77060{fileIndex:D5}",
            "KPP_emitenta_Rekv_em" => $"7705{fileIndex:D5}",
            "OGRN_emitenta_Rekv_em" => $"10277390{fileIndex:D5}",
            "GRN_vypuska_czennyx_bumag_Rekv_Emis_CZen_Bumag" => $"10202{fileIndex:D4}B003D",
            "ISIN_Rekv_Emis_CZen_Bumag" => $"RU000A0ZZY{(char)('A' + fileIndex % 26)}{fileIndex % 10}",
            "Naim_org_Sved_ob_org_ved_uchet_prav_czb" => "НРК-Р.О.С.Т.",
            "INN_ili_TIN_Sved_ob_org_ved_uchet_prav_czb" => $"77260{fileIndex:D5}",
            "KPP_Sved_ob_org_ved_uchet_prav_czb" => $"7718{fileIndex:D5}",
            "OGRN_Sved_ob_org_ved_uchet_prav_czb" => $"10277392{fileIndex:D5}",
            _ => $"value_{fileIndex}_{factIndex}"
        };
    }
}
