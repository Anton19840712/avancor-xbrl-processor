# XBRL Processor

Консольное приложение для обработки XBRL отчётов.

## Использование

```bash
cd XbrlProcessor
dotnet run
```

Приложение выполняет 4 задачи:
1. Поиск дубликатов контекстов в report1.xbrl
2. Объединение report1.xbrl и report2.xbrl → создаёт `merged.xbrl`
3. Сравнение report1.xbrl и report2.xbrl
4. Выполнение XPath запросов

Входные файлы: `Reports/report1.xbrl`, `Reports/report2.xbrl`
Результат: `merged.xbrl` (объединённый отчёт)

### Примеры вывода

**Task 1: Поиск дубликатов контекстов**
```
Report1:
  Найдены дубликаты: A1, A4
  Найдены дубликаты: A41, A42

Report2:
  Найдены дубликаты: A1, A4
  Найдены дубликаты: A41, A42
  Найдены дубликаты: A58, A60
```

**Task 2: Объединение отчётов**
```
Было: report1 (60 контекстов) + report2 (61 контекст)
Объединённый отчёт: 58 контекстов, 2 единиц, 353 фактов
(удалены дубликаты контекстов)
```

**Task 3: Сравнение отчётов**
```
Отсутствующие факты (в report1, но нет в report2): 1
  - V8_1C_f314purcb-dic_CZB_Uchtenn_BB (context: A54)

Новые факты (нет в report1, но есть в report2): 1
  - V8_1C_f286purcb-dic_ISIN_Rekv_Emis_CZen_Bumag (context: A50)

Изменённые факты: 2
  - V8_1C_f9purcb-dic_AdresPocht_Prof_uch (context: A0)
    Report1: 119000, г. Москва, ул. Иваново, д.1
    Report2: 129000, г. Москва, ул. Иваново, д.1

  - V8_1C_f183purcb-dic_SummaInvestPortf (context: A29)
    Report1: 565827102366.75
    Report2: 565827102366.55
```

**Task 4: XPath запросы**
```
1. Контексты с периодом instant = "2019-04-30"
   Найдено: 38 контекстов (A0, A1, A4, ..., A39)

2. Контексты со сценарием dimension="dim-int:ID_sobstv_CZBTaxis"
   Найдено: 20 контекстов

3. Контексты без сценария
   Найдено: 1 контекст (A0)
```

## Диаграммы

Открыть на [app.diagrams.net](https://app.diagrams.net)

- `1-processing-flow.drawio` - последовательность выполнения задач
- `2-parser-sequence.drawio` - работа парсера
- `3-command-pattern.drawio` - паттерн команд

## Технологии

**.NET 9 / C# 12**

- **Records** (C# 9) - Scenario, Unit, FactDifference, ComparisonResult
- **Primary Constructors** (C# 12) - XbrlParser, XbrlMerger, XbrlAnalyzer, Commands
- **File-scoped Namespaces** (C# 10) - все файлы
- **Collection Expressions** (C# 12) - `[..]` в XbrlAnalyzer
- **Required Properties** (C# 11) - поля Id в моделях
- **Init-only Properties** (C# 9) - `{ get; init; }` в records

**Паттерны**
- Builder Pattern - InstanceBuilder
- Command Pattern - IXbrlCommand, CommandInvoker

**Тесты**
- xUnit 2.9.2
- FluentAssertions 8.8.0
