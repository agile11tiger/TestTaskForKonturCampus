using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<int, EntryInfo> _items = new Dictionary<int, EntryInfo>();
        private readonly Dictionary<int, User> _users = new Dictionary<int, User>();

        public int Count => this.Count();

        private class User
        {
            public bool isAllow = true;
        }

        private class TimestampedValue<T>
        {
            public readonly long timestamp;
            public readonly T value;

            public TimestampedValue(long timestamp, T value)
            {
                this.timestamp = timestamp;
                this.value = value;
            }
        }

        private class UserChanges<T> : TimestampedValue<T>
        {
            public readonly int userId;

            public UserChanges(int userId, TimestampedValue<T> timestampedValue) : base(timestampedValue.timestamp, timestampedValue.value)
            {
                this.userId = userId;
            }

            public static UserChanges<T> Merge(UserChanges<T> a, UserChanges<T> b, Func<UserChanges<T>, UserChanges<T>, bool> bigger)
            {
                if (a == null)
                {
                    return b;
                }
                if (b == null)
                {
                    return a;
                }
                return bigger(a, b) ? a : b;
            }

            public TimestampedValue<T> ToBase()
            {
                return new TimestampedValue<T>(timestamp, value);
            }
        }

        private class EntryInfo
        {
            private readonly Dictionary<int, UserEntryChanges> _lastChangesPerUser = new Dictionary<int, UserEntryChanges>();

            private class UserEntryChanges
            {
                public TimestampedValue<bool> isExist;
                public TimestampedValue<EntryState> state;
                public TimestampedValue<string> name;
            };

            private static bool ChangesNameBigger(UserChanges<string> x, UserChanges<string> y)
            {
                if (x.timestamp > y.timestamp)
                {
                    return true;
                }
                if (x.timestamp < y.timestamp)
                {
                    return false;
                }
                if (x.userId < y.userId)
                {
                    return true;
                }
                if (x.userId > y.userId)
                {
                    return false;
                }
                if (String.Compare(x.value, y.value, StringComparison.InvariantCulture) < 0)
                {
                    return false;
                }
                if (String.Compare(x.value, y.value, StringComparison.InvariantCulture) > 0)
                {
                    return true;
                }
                return false; // equal changes
            }

            private static bool ChangesStateBigger(UserChanges<EntryState> x, UserChanges<EntryState> y)
            {
                if (x.timestamp > y.timestamp)
                {
                    return true;
                }
                if (x.timestamp < y.timestamp)
                {
                    return false;
                }
                if (x.value == EntryState.Undone && y.value == EntryState.Done)
                {
                    return true;
                }
                if (x.value == EntryState.Done && y.value == EntryState.Undone)
                {
                    return false;
                }
                if (x.userId < y.userId)
                {
                    return true;
                }
                if (x.userId > y.userId)
                {
                    return false;
                }
                return false; // equal changes
            }

            private static bool ChangesIsExistBigger(UserChanges<bool> x, UserChanges<bool> y)
            {
                if (x.timestamp > y.timestamp)
                {
                    return true;
                }
                if (x.timestamp < y.timestamp)
                {
                    return false;
                }
                if (!x.value && y.value)
                {
                    return true;
                }
                if (x.value && !y.value)
                {
                    return false;
                }
                if (x.userId < y.userId)
                {
                    return true;
                }
                if (x.userId > y.userId)
                {
                    return false;
                }
                return false; // equal changes
            }

            private static UserChanges<T> CreateUserChanges<T>(int? userId, TimestampedValue<T> value)
            {
                if (value == null || userId == null)
                    return null;
                return new UserChanges<T>(userId.Value, value);
            }

            public void AddUserChangeName(int userId, TimestampedValue<string> name)
            {
                if (!_lastChangesPerUser.ContainsKey(userId))
                {
                    _lastChangesPerUser[userId] = new UserEntryChanges();
                }

                _lastChangesPerUser[userId].name = UserChanges<string>.Merge(
                    CreateUserChanges(userId, name),
                    CreateUserChanges(userId, _lastChangesPerUser[userId].name),
                    ChangesNameBigger
                    ).ToBase();
            }

            public void AddUserChangeState(int userId, TimestampedValue<EntryState> state)
            {
                if (!_lastChangesPerUser.ContainsKey(userId))
                {
                    _lastChangesPerUser[userId] = new UserEntryChanges();
                }

                _lastChangesPerUser[userId].state = UserChanges<EntryState>.Merge(
                    CreateUserChanges(userId, state),
                    CreateUserChanges(userId, _lastChangesPerUser[userId].state),
                    ChangesStateBigger
                ).ToBase();
            }

            public void AddUserChangeExist(int userId, TimestampedValue<bool> isExist)
            {
                if (!_lastChangesPerUser.ContainsKey(userId))
                {
                    _lastChangesPerUser[userId] = new UserEntryChanges();
                }
                _lastChangesPerUser[userId].isExist = UserChanges<bool>.Merge(
                    CreateUserChanges(userId, isExist),
                    CreateUserChanges(userId, _lastChangesPerUser[userId].isExist),
                    ChangesIsExistBigger
                ).ToBase();
            }

            public bool IsExist(IReadOnlyDictionary<int, User> users)
            {
                UserChanges<bool> isExistCurrent = _lastChangesPerUser
                    .Where(userChanges => users[userChanges.Key].isAllow && userChanges.Value.isExist != null)
                    .Aggregate<KeyValuePair<int, UserEntryChanges>, UserChanges<bool>>(
                        null,
                        (current, userChanges) =>
                            UserChanges<bool>.Merge(
                                current,
                                CreateUserChanges(userChanges.Key, userChanges.Value.isExist),
                                ChangesIsExistBigger
                            )
                    );

                return isExistCurrent?.value ?? false;
            }

            public EntryState IsDone(IReadOnlyDictionary<int, User> users)
            {
                UserChanges<EntryState> isDoneCurrent = _lastChangesPerUser
                    .Where(userChanges => users[userChanges.Key].isAllow && userChanges.Value.state != null)
                    .Aggregate<KeyValuePair<int, UserEntryChanges>, UserChanges<EntryState>>(
                        null,
                        (current, userChanges) =>
                            UserChanges<EntryState>.Merge(
                                current,
                                CreateUserChanges(userChanges.Key, userChanges.Value.state),
                                ChangesStateBigger
                            )
                    );

                return isDoneCurrent?.value ?? EntryState.Undone;
            }

            public string Name(IReadOnlyDictionary<int, User> users)
            {
                UserChanges<string> nameCurrent = _lastChangesPerUser
                    .Where(userChanges => users[userChanges.Key].isAllow && userChanges.Value.name != null)
                    .Aggregate<KeyValuePair<int, UserEntryChanges>, UserChanges<string>>(
                        null,
                        (current, userChanges) =>
                            UserChanges<string>.Merge(
                                current,
                                CreateUserChanges(userChanges.Key, userChanges.Value.name),
                                ChangesNameBigger
                            )
                    );

                if (nameCurrent == null)
                {
                    throw new ArgumentException("Try get name, but entry not exist");
                }
                return nameCurrent.value;
            }
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            CreateIfNotExist(entryId, userId);

            _items[entryId].AddUserChangeExist(userId, new TimestampedValue<bool>(timestamp, true));
            _items[entryId].AddUserChangeName(userId, new TimestampedValue<string>(timestamp, name));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            CreateIfNotExist(entryId, userId);

            _items[entryId].AddUserChangeExist(userId, new TimestampedValue<bool>(timestamp, false));
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            CreateIfNotExist(entryId, userId);
            _items[entryId].AddUserChangeState(userId, new TimestampedValue<EntryState>(timestamp, EntryState.Done));
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            CreateIfNotExist(entryId, userId);
            _items[entryId].AddUserChangeState(userId, new TimestampedValue<EntryState>(timestamp, EntryState.Undone));
        }

        public void DismissUser(int userId)
        {
            CreateIfNotExistUser(userId);

            _users[userId].isAllow = false;
        }

        public void AllowUser(int userId)
        {
            CreateIfNotExistUser(userId);

            _users[userId].isAllow = true;
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return (from pair in _items
                    where pair.Value.IsExist(_users)
                    let entryInfo = pair.Value
                    let entryId = pair.Key
                    select new Entry(entryId, entryInfo.Name(_users), entryInfo.IsDone(_users))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CreateIfNotExist(int entryId, int userId)
        {
            CreateIfNotExistEntry(entryId);
            CreateIfNotExistUser(userId);
        }

        private void CreateIfNotExistUser(int userId)
        {
            if (!_users.ContainsKey(userId))
            {
                _users[userId] = new User();
            }
        }

        private void CreateIfNotExistEntry(int entryId)
        {
            if (!_items.ContainsKey(entryId))
            {
                _items[entryId] = new EntryInfo();
            }
        }
    }
}
