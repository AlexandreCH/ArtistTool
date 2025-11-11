using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArtistTool.Domain
{
    public abstract class BaseObservable : INotifyPropertyChanged, IDisposable
    {
        private readonly string Name;
        
        public BaseObservable() => Name = GetType().Name;

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<string, object> _propertyValues = [];
        private bool disposedValue;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void OnObservablePropertyChanged<T>(
            [CallerMemberName]string propertyName = "", T? value = default) where
            T : class, INotifyPropertyChanged
        {
            if (_propertyValues.TryGetValue(propertyName, out object? value1) && value1 is T oldValue)
            {
                oldValue.PropertyChanged -= ChildPropertyChanged;
            }
            
            if (value is not null)
            {
                value.PropertyChanged += ChildPropertyChanged;
                _propertyValues[propertyName] = value;
            }

            OnPropertyChanged(propertyName);
        }

        private void ChildPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
            OnPropertyChanged($"{Name}.{e.PropertyName}");

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _propertyValues.Values.OfType<INotifyPropertyChanged>())
                    {
                        kvp.PropertyChanged -= ChildPropertyChanged;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
