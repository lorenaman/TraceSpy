﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace TraceSpy
{
    public class WpfSettings : Serializable<WpfSettings>
    {
        private string _fontName;
        private Lazy<Typeface> _typeFace;
        private string _alternateColor;
        private Lazy<Brush> _alternateBrush;
        private List<string> _searches = new List<string>();
        private List<Filter> _filters = new List<Filter>();
        private List<EtwProvider> _etwProviders = new List<EtwProvider>();

        public WpfSettings()
        {
            _alternateBrush = new Lazy<Brush>(CreateAlternateBrush);
            _typeFace = new Lazy<Typeface>(CreateTypeFace);
            RemoveEmptyLines = true;
            AutoScroll = true;
            ResolveProcessName = true;
            Left = 50;
            Top = 50;
            FindLeft = 50;
            FindTop = 100;
            Width = 800;
            Height = 600;
            FontName = "Lucida Console";
            FontSize = 10;
            IndexColumnWidth = 75;
            TicksColumnWidth = 96;
            ProcessColumnWidth = 102;
            TextColumnWidth = 681;
            _filters.Add(new Filter(null, false));
        }

        public bool ResolveProcessName { get; set; }
        public bool ShowProcessId { get; set; }
        public bool ShowTooltips { get; set; }
        public bool ShowEtwDescription { get; set; }
        public bool AutoScroll { get; set; }
        public bool RemoveEmptyLines { get; set; }
        public bool WrapText { get; set; }
        public bool CaptureEtwTraces { get; set; }
        public bool CaptureOdsTraces { get; set; }
        public ShowTicksMode ShowTicksMode { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double FindLeft { get; set; }
        public double FindTop { get; set; }
        public double FontSize { get; set; }
        public double IndexColumnWidth { get; set; }
        public double TicksColumnWidth { get; set; }
        public double ProcessColumnWidth { get; set; }
        public double TextColumnWidth { get; set; }
        public bool DontAnimateCaptureMenuItem { get; set; }

        [XmlIgnore] // we don't persist this one
        public bool DisableAllFilters { get; set; }

        public string FontName
        {
            get => _fontName;
            set
            {
                if (_fontName == value)
                    return;

                _fontName = value;
                _typeFace = new Lazy<Typeface>(CreateTypeFace);
            }
        }

        [XmlIgnore]
        public Typeface TypeFace => _typeFace.Value;

        private Typeface CreateTypeFace()
        {
            if (!string.IsNullOrWhiteSpace(FontName))
            {
                try
                {
                    return new Typeface(FontName);
                }
                catch
                {
                    // continue
                }
            }
            return new Typeface("Lucida Console");
        }

        public string AlternateColor
        {
            get => _alternateColor;
            set
            {
                if (_alternateColor == value)
                    return;

                _alternateColor = value;
                _alternateBrush = new Lazy<Brush>(CreateAlternateBrush);
            }
        }

        [XmlIgnore]
        public Brush AlternateBrush => _alternateBrush.Value;

        private Brush CreateAlternateBrush()
        {
            if (string.IsNullOrWhiteSpace(AlternateColor))
                return null;

            try
            {
                var color = (Color)(new ColorConverter().ConvertFromInvariantString(AlternateColor));
                return new SolidColorBrush(color);
            }
            catch
            {
                return null;
            }
        }

        public string[] Searches
        {
            get => _searches.ToArray();
            set => _searches = value == null ? new List<string>() : new List<string>(value);
        }

        public void ClearSearches() => _searches = new List<string>();

        public void AddSearch(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return;

            _searches.RemoveAll(s => s == search);
            _searches.Insert(0, search);
        }

        public Filter[] Filters
        {
            get => _filters.ToArray();
            set => _filters = value == null ? new List<Filter>() : new List<Filter>(value);
        }

        public bool AddFilter(Filter old, Filter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var existing = _filters.FirstOrDefault(p => p.Equals(old));
            if (existing != null)
            {
                existing.IsActive = filter.IsActive;
                existing.Column = filter.Column;
                existing.Type = filter.Type;
                existing.Definition = filter.Definition;
                existing.IgnoreCase = filter.IgnoreCase;
                return false;
            }

            _filters.Add(filter);
            return true;
        }

        public bool RemoveFilter(Filter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return _filters.Remove(filter);
        }

        public EtwProvider[] EtwProviders
        {
            get => _etwProviders.ToArray();
            set => _etwProviders = value == null ? new List<EtwProvider>() : new List<EtwProvider>(value);
        }

        public bool AddEtwProvider(EtwProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var existing = _etwProviders.FirstOrDefault(p => p.Equals(provider));
            if (existing != null)
            {
                existing.IsActive = provider.IsActive;
                existing.StringMessageMode = provider.StringMessageMode;
                existing.Description = provider.Description;
                existing.TraceLevel = provider.TraceLevel;
                return false;
            }

            _etwProviders.Add(provider);
            return true;
        }

        public bool RemoveEtwProvider(Guid guid)
        {
            var existing = _etwProviders.FirstOrDefault(p => p.Guid == guid);
            if (existing == null)
                return false;

            return _etwProviders.Remove(existing);
        }

        public bool RemoveEtwProvider(EtwProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return _etwProviders.Remove(provider);
        }

        public bool ExcludeLine(string line, string processName)
        {
            if (RemoveEmptyLines && string.IsNullOrWhiteSpace(line))
                return true;

            if (App.Current.Settings.DisableAllFilters)
                return true;

            foreach (var filter in _filters.Where(f => f.IsActive))
            {
                if (filter.ExcludeLine(line, processName))
                    return true;
            }
            return false;
        }
    }
}
