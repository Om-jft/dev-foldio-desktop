using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSLR_Tool_PC.ViewModels
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        protected IDictionary<TKey, TValue> Dictionary { get; }

        #region Constants (standart constants for collection/dictionary)

        private const string CountString = "Count";
        private const string IndexerName = "Item[]";
        private const string KeysName = "Keys";
        private const string ValuesName = "Values";

        #endregion

        #region .ctor

        public ObservableDictionary()
        {
            Dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public ObservableDictionary(int capacity)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        #endregion

        #region INotifyCollectionChanged and INotifyPropertyChanged

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IDictionary<TKey, TValue> Implementation

        public TValue this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                TValue oldItem;
                InsertObject(key: key, value: value, appendMode: AppendMode.Replace, oldValue: out oldItem);

                if (oldItem != null)
                {
                    OnCollectionChanged(action: NotifyCollectionChangedAction.Replace, newItem: new KeyValuePair<TKey, TValue>(key, value), oldItem: new KeyValuePair<TKey, TValue>(key, oldItem));
                }
                else
                {
                    OnCollectionChanged(action: NotifyCollectionChangedAction.Add, changedItem: new KeyValuePair<TKey, TValue>(key, value));
                }
            }
        }

        public ICollection<TKey> Keys => Dictionary.Keys;

        public ICollection<TValue> Values => Dictionary.Values;

        public int Count => Dictionary.Count;

        public bool IsReadOnly => Dictionary.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            InsertObject(key: key, value: value, appendMode: AppendMode.Add);
            OnCollectionChanged(action: NotifyCollectionChangedAction.Add, changedItem: new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            InsertObject(key: item.Key, value: item.Value, appendMode: AppendMode.Add);
            OnCollectionChanged(action: NotifyCollectionChangedAction.Add, changedItem: new KeyValuePair<TKey, TValue>(item.Key, item.Value));
        }

        public void Clear()
        {
            if (!Dictionary.Any()) { return; }

            var removedItems = new List<KeyValuePair<TKey, TValue>>(Dictionary.ToList());
            Dictionary.Clear();
            OnCollectionChanged(action: NotifyCollectionChangedAction.Reset, newItems: null, oldItems: removedItems);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Dictionary.CopyTo(array: array, arrayIndex: arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            TValue value;
            if (Dictionary.TryGetValue(key, out value))
            {
                Dictionary.Remove(key);
                OnCollectionChanged(action: NotifyCollectionChangedAction.Remove, changedItem: new KeyValuePair<TKey, TValue>(key, value));
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Dictionary.Remove(item))
            {
                OnCollectionChanged(action: NotifyCollectionChangedAction.Remove, changedItem: item);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        #endregion

        #region IReadOnlyDictionary

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Dictionary.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Dictionary.Values;

        #endregion

        #region ObservableDictionary inner methods

        private void InsertObject(TKey key, TValue value, AppendMode appendMode)
        {
            TValue trash;
            InsertObject(key, value, appendMode, out trash);
        }

        private void InsertObject(TKey key, TValue value, AppendMode appendMode, out TValue oldValue)
        {
            oldValue = default(TValue);

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            TValue item;
            if (Dictionary.TryGetValue(key, out item))
            {
                if (appendMode == AppendMode.Add)
                {
                    throw new ArgumentException("Item with the same key has already been added");
                }

                if (Equals(item, value))
                {
                    return;
                }

                Dictionary[key] = value;
                oldValue = item;
            }
            else
            {
                Dictionary[key] = value;
            }
        }

        private void OnPropertyChanged()
        {
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                OnPropertyChanged();
            }

            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(
                this, new NotifyCollectionChangedEventArgs(
                    action: NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(this, new NotifyCollectionChangedEventArgs(action: action, changedItem: changedItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(this, new NotifyCollectionChangedEventArgs(action: action, newItem: newItem, oldItem: oldItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(this, new NotifyCollectionChangedEventArgs(action: action, changedItems: newItems));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
        {
            OnPropertyChanged();
            var handler = CollectionChanged;
            handler?.Invoke(this, new NotifyCollectionChangedEventArgs(action: action, newItems: newItems, oldItems: oldItems));
        }

        #endregion

        internal enum AppendMode
        {
            Add,
            Replace
        }
    }
}
