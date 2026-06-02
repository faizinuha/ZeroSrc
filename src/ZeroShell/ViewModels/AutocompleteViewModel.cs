using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZeroMix.Utils;

namespace ZeroMix.ZeroShell.ViewModels
{
    public class AutocompleteViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SuggestionItem> _suggestions = new ObservableCollection<SuggestionItem>();
        private bool _isOpen = false;
        private SuggestionItem? _selectedSuggestion;

        public ObservableCollection<SuggestionItem> Suggestions
        {
            get => _suggestions;
            set
            {
                if (_suggestions != value)
                {
                    _suggestions = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public SuggestionItem? SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                if (_selectedSuggestion != value)
                {
                    _selectedSuggestion = value;
                    OnPropertyChanged();
                }
            }
        }

        public void UpdateSuggestions(System.Collections.Generic.List<SuggestionItem> newSuggestions)
        {
            Suggestions.Clear();
            foreach (var item in newSuggestions)
            {
                Suggestions.Add(item);
            }

            IsOpen = Suggestions.Count > 0;
            SelectedSuggestion = Suggestions.Count > 0 ? Suggestions[0] : null;
        }

        public void Clear()
        {
            Suggestions.Clear();
            IsOpen = false;
            SelectedSuggestion = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
