namespace ArtistTool.Domain.Agents
{
    public class CritiqueResponse : BaseObservable
    {
        private Critique[] _critiques = [];

        public Critique[] Critiques
        {
            get => _critiques;
            set
            {
                if (_critiques != value)
                {
                    _critiques = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
