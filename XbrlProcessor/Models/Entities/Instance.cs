using System.Collections.ObjectModel;

namespace Models.XBRL
{
    /// <summary>
    /// Модель записи инстанса (файла отчета)
    /// </summary>
    /// <remarks>Таблица ReportInstances</remarks>

    public class Instance
    {

        /// <summary>Коллекция контекстов (context)</summary>
        /// <see>xbrli:context</see>
        public virtual Contexts Contexts { get; set; } = new Contexts();

        /// <summary>Коллекция единиц измерения (unit)</summary>
        /// <see>xbrli:unit</see>
        public virtual Units Units { get; set; } = new Units();

        /// <summary>Коллекция фактов (fact)</summary>
        public virtual Facts Facts { get; set; } = new Facts();

    }

}
