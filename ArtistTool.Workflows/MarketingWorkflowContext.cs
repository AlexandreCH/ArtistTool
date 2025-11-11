using ArtistTool.Domain;
using ArtistTool.Domain.Agents;
using ArtistTool.Services;

namespace ArtistTool.Workflows
{
    public class MarketingWorkflowContext : BaseObservable
    {
        private string _baseDirectory = string.Empty;

        private bool _completed = false; 

        public string BaseDirectory => _baseDirectory;

        public int FanInNodes { get; set; }

        private WorkflowNode<CritiqueResponse> _critique = new();
        
        private Dictionary<string, WorkflowNode<MediumPreviewResponse>> _mediumPreviews = [];

        private List<WorkflowNode<ResearchResponse>> _researchNodes = [];

        private IImageRegistry imageRegistry = new ImageRegistry();
        
        private Photograph? _photo; 
        public string Id => Photo?.Id ?? string.Empty;
        
        private string workflowDiagram = string.Empty;

        public Photograph? Photo
        {   
            get => _photo;
            set
            {
                if (_photo != value)
                {
                    _photo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Id));

                    if (value is not null)
                    {
                        _baseDirectory = Path.Combine(
                            Path.GetDirectoryName(value.Path)!,
                            value.Id);
                        
                        if (!Directory.Exists(_baseDirectory))
                        {
                            Directory.CreateDirectory(_baseDirectory);
                        }

                
                        OnPropertyChanged(nameof(BaseDirectory));
                    }
                }
            }
        }

        public void RegisterResearch(WorkflowNode<ResearchResponse> response)
        {
            _researchNodes.Add(response);
        }

        public IImageRegistry ImageRegistry => imageRegistry;

        public WorkflowNode<ResearchResponse>[] Research
        {
            get => [.. _researchNodes];
        }

        public WorkflowNode<CritiqueResponse> Critique
        {
            get => _critique;
            set
            {
                if (_critique != value)
                {
                    _critique = value;
                    OnObservablePropertyChanged(value: value);
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

        public string WorkflowDiagram
        {
            get => workflowDiagram;
            set
            {
                if (workflowDiagram != value)
                {
                    workflowDiagram = value;
                    OnPropertyChanged();
                }
            }
        }

        public Dictionary<string, WorkflowNode<MediumPreviewResponse>> MediumPreviews
        {
            get => _mediumPreviews; 
            set
            {
                if (_mediumPreviews != value)
                {
                    _mediumPreviews = value;
                    if (value is not null)
                    {
                        foreach (var kvp in value)
                        {
                            OnObservablePropertyChanged(
                                propertyName: $"MediumPreviews[{kvp.Key}]",
                                value: kvp.Value);
                        }
                    }
                }
            }
        }
    }
}
