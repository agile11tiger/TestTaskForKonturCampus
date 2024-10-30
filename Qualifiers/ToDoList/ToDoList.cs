using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        public ToDoList()
        {
            commandComparer = new CommandComparer<Command<UserCell>>();
            commandValidator = new CommandValidator();
            sections = new Dictionary<int, ToDoListSection>();
            users = new Dictionary<int, User>();
            users.Add(int.MinValue, new User(int.MinValue));
        }

        private readonly ICommandValidator commandValidator;
        private readonly IComparer<Command<UserCell>> commandComparer;
        private readonly IDictionary<int, ToDoListSection> sections;
        private readonly IDictionary<int, User> users;
        public int Count { get => sections.Values.Where(i => !i.IsRemoving).Count(); }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            Command<UserCell> CreateCommand(UserCell c) => new AddCommand<UserCell>(c);
            ExecuteCommand(entryId, name, EntryState.Undone, userId, timestamp, CreateCommand);
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            Command<UserCell> CreateCommand(UserCell c) => new RemoveCommand<UserCell>(c);
            ExecuteCommand(entryId, "", EntryState.Undone, userId, timestamp, CreateCommand);
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            Command<UserCell> CreateCommand(UserCell c) => new MarkDoneCommand<UserCell>(c);
            ExecuteCommand(entryId, "", EntryState.Done, userId, timestamp, CreateCommand);
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            Command<UserCell> CreateCommand(UserCell c) => new MarkUndoneCommand<UserCell>(c);
            ExecuteCommand(entryId, "", EntryState.Undone, userId, timestamp, CreateCommand);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            foreach (var section in sections)
            {
                if (!section.Value.IsRemoving)
                    yield return section.Value.UserEntryCell.Entry;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void DismissUser(int userId)
        {
            CreateUserIfNotExist(userId);
            users[userId].IsAllowance = false;
            Refresh();
        }

        public void AllowUser(int userId)
        {
            CreateUserIfNotExist(userId);
            users[userId].IsAllowance = true;
            Refresh();
        }

        private void CreateUserIfNotExist(int userId)
        {
            if (!users.ContainsKey(userId))
                users[userId] = new User(userId);
        }

        private void CreateSectionIfNotExist(int entryId)
        {
            if (!sections.ContainsKey(entryId))
            {
                var commandsHistory = new SortedSet<Command<UserCell>>(commandComparer);
                var userEntryCell = new UserCell(entryId, "", EntryState.Undone, int.MinValue, long.MinValue);
                var userMarkCell = new UserCell(entryId, "", EntryState.Undone, int.MinValue, long.MinValue);
                var section = new ToDoListSection(userEntryCell, userMarkCell, commandsHistory);
                var addCommand = new AddCommand<UserCell>(userEntryCell);
                var markUndoneCommand = new MarkUndoneCommand<UserCell>(userMarkCell);
                section.CommandsHistory.Add(addCommand);
                section.CommandsHistory.Add(markUndoneCommand);
                sections[entryId] = section;
            }
        }

        private void Refresh()
        {
            foreach (var section in sections.Values)
                section.Refresh(sections, users);
        }

        private void ExecuteCommand(
            int entryId,
            string name,
            EntryState state,
            int userId,
            long timestamp,
            Func<UserCell, Command<UserCell>> CreateCommand)
        {
            CreateUserIfNotExist(userId);
            CreateSectionIfNotExist(entryId);

            var userCell = new UserCell(entryId, name, state, userId, timestamp);
            var command = CreateCommand(userCell);

            if (users[userId].IsAllowance
             && commandValidator.IsValid(sections[entryId], command.Name, userId, timestamp))
                command.Execute(sections);

            sections[entryId].CommandsHistory.Replace(command);
        }
    }

    public class ToDoListSection
    {
        public ToDoListSection(UserCell userEntryCell, UserCell userMarkCell, SortedSet<Command<UserCell>> commandsHistory)
        {
            UserEntryCell = userEntryCell;
            UserMarkCell = userMarkCell;
            CommandsHistory = commandsHistory;
        }

        public SortedSet<Command<UserCell>> CommandsHistory { get; }
        public UserCell UserEntryCell { get; set; }
        public UserCell UserMarkCell { get; set; }
        public bool IsRemoving { get => string.IsNullOrEmpty(UserEntryCell?.Entry.Name) ? true : false; }

        public void Refresh(IDictionary<int, ToDoListSection> sections, IDictionary<int, User> users)
        {
            var isExecutedAddingOrRemoving = false;
            var isExecutedMarking = false;

            foreach (var command in CommandsHistory.Reverse())
            {
                if (!users[command.UserCell.UserId].IsAllowance)
                    continue;

                if (isExecutedMarking && isExecutedAddingOrRemoving)
                    break;

                if (!isExecutedAddingOrRemoving
                && (command.Name == CommandNames.Add || command.Name == CommandNames.Remove))
                {
                    command.Execute(sections);
                    isExecutedAddingOrRemoving = true;
                }

                if (!isExecutedMarking
                && (command.Name == CommandNames.MarkDone || command.Name == CommandNames.MarkUndone))
                {
                    command.Execute(sections);
                    isExecutedMarking = true;
                }
            }
        }
    }

    public class User
    {
        public User(int userId)
        {
            UserId = userId;
            IsAllowance = true;
        }

        public int UserId { get; }
        public bool IsAllowance { get; set; }
    }

    public class UserCell
    {
        public UserCell(int entryId, string name, EntryState state, int userId, long timestamp)
        {
            Entry = new Entry(entryId, name, state);
            UserId = userId;
            TimeStamp = timestamp;
        }

        public Entry Entry { get; set; }
        public int UserId { get; set; }
        public long TimeStamp { get; set; }

        public UserCell Clone()
        {
            var clone = (UserCell)MemberwiseClone();
            clone.Entry = new Entry(Entry.Id, Entry.Name, Entry.State);
            return clone;
        }
    }

    public abstract class Command<T> : IEquatable<Command<T>> where T : UserCell
    {
        protected Command(CommandNames name, T userCell)
        {
            Name = name;
            UserCell = userCell;
        }

        public CommandNames Name { get; }
        public T UserCell { get; }
        public int EntryId { get => UserCell.Entry.Id; }
        public abstract void Execute(IDictionary<int, ToDoListSection> sections);

        public bool Equals(Command<T> other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Name == other.Name
                && UserCell.UserId == other.UserCell.UserId
                && UserCell.TimeStamp == other.UserCell.TimeStamp
                && UserCell.Entry.Equals(other.UserCell.Entry);
        }

        public override bool Equals(object obj)
        {
            return obj is Command<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = UserCell.UserId;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ UserCell.TimeStamp.GetHashCode();
                hashCode = (hashCode * 397) ^ UserCell.Entry.GetHashCode();
                return hashCode;
            }
        }
    }

    public class AddCommand<T> : Command<T> where T : UserCell
    {
        public AddCommand(T userCell) : base(CommandNames.Add, userCell)
        {
        }

        public override void Execute(IDictionary<int, ToDoListSection> sections)
        {
            sections[EntryId].UserEntryCell = UserCell.Clone();
            var state = sections[EntryId].UserMarkCell.Entry.State;
            var cell = sections[EntryId].UserEntryCell;
            cell.Entry = state == EntryState.Undone
                ? cell.Entry.MarkUndone()
                : cell.Entry.MarkDone();
        }
    }

    public class RemoveCommand<T> : Command<T> where T : UserCell
    {
        public RemoveCommand(T userCell) : base(CommandNames.Remove, userCell)
        {
        }

        public override void Execute(IDictionary<int, ToDoListSection> sections)
        {
            sections[EntryId].UserEntryCell = UserCell.Clone();
        }
    }

    public class MarkDoneCommand<T> : Command<T> where T : UserCell
    {
        public MarkDoneCommand(T userCell) : base(CommandNames.MarkDone, userCell)
        {
        }

        public override void Execute(IDictionary<int, ToDoListSection> sections)
        {
            sections[EntryId].UserMarkCell = UserCell.Clone();
            sections[EntryId].UserEntryCell.Entry = sections[EntryId].UserEntryCell.Entry.MarkDone();
        }
    }

    public class MarkUndoneCommand<T> : Command<T> where T : UserCell
    {
        public MarkUndoneCommand(T userCell) : base(CommandNames.MarkUndone, userCell)
        {
        }

        public override void Execute(IDictionary<int, ToDoListSection> sections)
        {
            sections[EntryId].UserMarkCell = UserCell.Clone();
            sections[EntryId].UserEntryCell.Entry = sections[EntryId].UserEntryCell.Entry.MarkUndone();
        }
    }

    public class CommandValidator : ICommandValidator
    {
        public bool IsValid(ToDoListSection section, CommandNames commandName, int userId, long timestamp)
        {
            switch (commandName)
            {
                case CommandNames.Add:
                    return ValidateAddCommand(section, userId, timestamp);
                case CommandNames.MarkDone:
                case CommandNames.MarkUndone:
                    return ValidateMarkCommand(section, timestamp);
                default:
                    return ValidateCommonCommand(section, timestamp);
            }
        }

        private bool ValidateAddCommand(ToDoListSection section, int userId, long timestamp)
        {
            if (section.IsRemoving == false
             && section.UserEntryCell.UserId < userId
             && section.UserEntryCell.TimeStamp == timestamp)
                return false;

            if (section.IsRemoving == true
             && section.UserEntryCell.TimeStamp == timestamp)
                return false;

            return ValidateCommonCommand(section, timestamp);
        }

        private bool ValidateMarkCommand(ToDoListSection section, long timestamp)
        {
            if (section.UserMarkCell.TimeStamp == timestamp
             && section.UserMarkCell.Entry.State == EntryState.Undone)
                return false;

            if (section.UserMarkCell.TimeStamp > timestamp)
                return false;

            return true;
        }

        private bool ValidateCommonCommand(ToDoListSection section, long timestamp)
        {
            if (section.UserEntryCell.TimeStamp > timestamp)
                return false;

            return true;
        }
    }

    public class CommandComparer<T> : IComparer<T> where T : Command<UserCell>
    {
        public int Compare(T x, T y)
        {
            if (x.Equals(y))
                return 0;

            if (x.UserCell.TimeStamp > y.UserCell.TimeStamp)
                return 1;

            if (x.UserCell.TimeStamp == y.UserCell.TimeStamp)
            {
                if (x.UserCell.UserId > y.UserCell.UserId)
                    return 1;
                else
                    return -1;
            }

            return -1;
        }
    }

    public interface ICommandValidator
    {
        bool IsValid(ToDoListSection section, CommandNames commandName, int userId, long timestamp);
    }

    public enum CommandNames
    {
        Add,
        Remove,
        MarkDone,
        MarkUndone
    }

    public static class SortedSetExtensions
    {
        /// <summary>
        /// Добавляет или заменяет указанный элемент из набора <see cref="SortedSet{T}"/>
        /// </summary>
        public static void Replace<T>(this SortedSet<T> sortedSet, T item)
        {
            sortedSet.Remove(item);
            sortedSet.Add(item);
        }
    }
}