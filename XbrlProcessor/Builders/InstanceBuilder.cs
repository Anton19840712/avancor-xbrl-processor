using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Builders
{
    /// <summary>
    /// Builder для создания объектов Instance
    /// </summary>
    public class InstanceBuilder
    {
        #region Fields

        private readonly Instance _instance;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор билдера Instance
        /// </summary>
        public InstanceBuilder()
        {
            _instance = new Instance();
        }

        #endregion

        #region Context Methods

        /// <summary>
        /// Добавляет один контекст в Instance
        /// </summary>
        /// <param name="context">Контекст для добавления</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddContext(Context context)
        {
            _instance.Contexts.Add(context);
            return this;
        }

        /// <summary>
        /// Добавляет коллекцию контекстов в Instance
        /// </summary>
        /// <param name="contexts">Коллекция контекстов</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddContexts(IEnumerable<Context> contexts)
        {
            foreach (var context in contexts)
            {
                _instance.Contexts.Add(context);
            }
            return this;
        }

        #endregion

        #region Unit Methods

        /// <summary>
        /// Добавляет одну единицу измерения в Instance
        /// </summary>
        /// <param name="unit">Единица измерения для добавления</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddUnit(Unit unit)
        {
            _instance.Units.Add(unit);
            return this;
        }

        /// <summary>
        /// Добавляет коллекцию единиц измерения в Instance
        /// </summary>
        /// <param name="units">Коллекция единиц измерения</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddUnits(IEnumerable<Unit> units)
        {
            foreach (var unit in units)
            {
                _instance.Units.Add(unit);
            }
            return this;
        }

        #endregion

        #region Fact Methods

        /// <summary>
        /// Добавляет один факт в Instance
        /// </summary>
        /// <param name="fact">Факт для добавления</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddFact(Fact fact)
        {
            _instance.Facts.Add(fact);
            return this;
        }

        /// <summary>
        /// Добавляет коллекцию фактов в Instance
        /// </summary>
        /// <param name="facts">Коллекция фактов</param>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder AddFacts(IEnumerable<Fact> facts)
        {
            foreach (var fact in facts)
            {
                _instance.Facts.Add(fact);
            }
            return this;
        }

        #endregion

        #region Clear Methods

        /// <summary>
        /// Очищает все контексты из Instance
        /// </summary>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder ClearContexts()
        {
            _instance.Contexts.Clear();
            return this;
        }

        /// <summary>
        /// Очищает все единицы измерения из Instance
        /// </summary>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder ClearUnits()
        {
            _instance.Units.Clear();
            return this;
        }

        /// <summary>
        /// Очищает все факты из Instance
        /// </summary>
        /// <returns>Текущий билдер для цепочки вызовов</returns>
        public InstanceBuilder ClearFacts()
        {
            _instance.Facts.Clear();
            return this;
        }

        #endregion

        #region Build Methods

        /// <summary>
        /// Строит и возвращает готовый объект Instance
        /// </summary>
        /// <returns>Построенный объект Instance</returns>
        public Instance Build()
        {
            // Можно добавить валидацию перед возвратом
            ValidateInstance();
            return _instance;
        }

        /// <summary>
        /// Валидирует созданный Instance перед возвратом
        /// </summary>
        private void ValidateInstance()
        {
            // Базовая валидация - можно расширить при необходимости
            if (_instance.Contexts == null)
            {
                throw new InvalidOperationException("Contexts collection cannot be null");
            }
            if (_instance.Units == null)
            {
                throw new InvalidOperationException("Units collection cannot be null");
            }
            if (_instance.Facts == null)
            {
                throw new InvalidOperationException("Facts collection cannot be null");
            }
        }

        #endregion
    }
}
