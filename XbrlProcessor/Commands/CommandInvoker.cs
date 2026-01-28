namespace XbrlProcessor.Commands
{
    /// <summary>
    /// Инвокер для управления и выполнения команд
    /// </summary>
    public class CommandInvoker
    {
        private readonly List<IXbrlCommand> _commands = new();

        /// <summary>
        /// Добавляет команду в очередь выполнения
        /// </summary>
        /// <param name="command">Команда для добавления</param>
        public void AddCommand(IXbrlCommand command)
        {
            _commands.Add(command);
        }

        /// <summary>
        /// Выполняет все добавленные команды в порядке добавления
        /// </summary>
        public void ExecuteAll()
        {
            foreach (var command in _commands)
            {
                command.Execute();
            }
        }

        /// <summary>
        /// Выполняет конкретную команду по индексу
        /// </summary>
        /// <param name="index">Индекс команды</param>
        public void ExecuteCommand(int index)
        {
            if (index >= 0 && index < _commands.Count)
            {
                _commands[index].Execute();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Command index is out of range");
            }
        }

        /// <summary>
        /// Очищает очередь команд
        /// </summary>
        public void ClearCommands()
        {
            _commands.Clear();
        }

        /// <summary>
        /// Возвращает количество команд в очереди
        /// </summary>
        public int GetCommandCount() => _commands.Count;

        /// <summary>
        /// Возвращает список всех команд
        /// </summary>
        public IReadOnlyList<IXbrlCommand> GetCommands() => _commands.AsReadOnly();
    }
}
