using ArtistTool.Domain;
using System.ComponentModel;

namespace ArtistTool.Workflows
{
    public class WorkflowNode<TResult> : BaseObservable
    {
        private bool _started;
        private bool _completed;
        private TResult? result;

        public bool Started
        {
            get => _started;
            set
            {
                if (_started != value)
                {
                    _started = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Completed
        {
            get => _completed;
            set
            {
                if (_completed != value)
                {
                    _completed = value;
                    OnPropertyChanged();
                }
            }
        }

        public TResult? Result
        {
            get => result;
            set
            {
                if (!Equals(result, value))
                {
                    result = value;
                    if (result is INotifyPropertyChanged observable)
                    {
                        OnObservablePropertyChanged(value: observable);
                    }
                    else
                    {
                        OnPropertyChanged();
                    }
                }
            }
        }
    }
}
