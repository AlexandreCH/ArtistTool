using System;
using System.Collections.Generic;
using System.Text;

namespace ArtistTool.Domain.Agents
{
    public class ResearchResponse : BaseObservable 
    {
        private string medium = string.Empty;
        private string area = string.Empty;
        private string topic = string.Empty;
        private string content = string.Empty;

        public string Medium
        {
            get => medium;
            set
            {
                if (medium != value)
                {
                    medium = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Area
        {
            get => area;
            set
            {
                if (area != value)
                {
                    area = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Topic
        {
            get => topic;
            set
            {
                if (topic != value)
                {
                    topic = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Content
        {
            get => content;
            set
            {
                if (content != value)
                {
                    content = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
