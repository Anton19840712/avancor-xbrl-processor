namespace XbrlProcessor.Models.Entities;

/* XBRL definition
 * namespace: http://www.xbrl.org/2003/instance
 * file path: www.xbrl.org\2003\xbrl-instance-2003-12-31.xsd

*/

public class Fact
    {
        /// <summary>Идентификатор фактов (значений отчета)</summary>
        /// <see>xbrli:unit/[@id]</see>
        public required string? Id { get; set; }

        /// <summary>Ссылка на контекст</summary>
        /// <see>xbrli:unit/[@contextRef]</see>
        public virtual string? ContextRef { get; set; }

        /// <summary>Используемые контекст</summary>
        public virtual Context? Context { get; set; }

        /// <summary>Ссылка на единицу измерения</summary>
        /// <see>xbrli:unit/[@unitRef]</see>
        public virtual string? UnitRef { get; set; }

        /// <summary>Используемые юнит</summary>
        public virtual Unit? Unit { get; set; }

        /// <summary>Точность измерения</summary>
        /// <see>xbrli:unit/[@decimals]</see>
        public virtual int? Decimals { get; set; }

        /// <summary>Точность значения</summary>
        /// <see>xbrli:unit/[@precision]</see>
        public virtual int? Precision { get; set; }

        /// <summary>Типизированное значение факта</summary>
        /// <see>xbrli:unit/*</see>
        public virtual XbrlValue Value { get; set; } = XbrlValue.Parse(null);

    }
