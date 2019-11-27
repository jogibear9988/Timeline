namespace WpfTimelineExample
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using ColorWheel.Core;
    using System.Windows.Media;
    using TimelineLibrary;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.IO;

    public partial class Window1: 
        Window, 
        IWeakEventListener,
        INotifyPropertyChanged
    {
        public Palette                                  m_palette;
        public event PropertyChangedEventHandler        PropertyChanged;
        private ColorManager                            m_cm;
        private SingleDelayedInvoke                     m_delay = new SingleDelayedInvoke(300);

        private int                                     m_selectedColor = -1;
        private int                                     m_selectedColorEffect = 0;

        public Window1(
        )
        {
            InitializeComponent();

            timeline.Loaded += (s, e) =>
            {
                LoadColors();
            };
        }

        void OnColorChanged(
            object                                      sender, 
            PropertyChangedEventArgs                    e
        )
        {
            PaletteColor                                pc;
            TemplateColor                               tc;

            if (e.PropertyName == "Color")
            {
                pc = sender as PaletteColor;
                tc = ColorManager.GetTemplate(((TemplateColor) pc.Tag).Name);

                m_delay.Invoke(() =>
                {
                    tc.Color = pc.RgbColor;
                    ColorManager.Instance.ClearCache();
                });
            }
        }

        private void LoadColors(
        )
        {
            if (Palette == null)
            {
                List<TemplateColor>                      lst = new List<TemplateColor>();

                foreach (var kv in ColorManager.Templates)
                {
                    if (kv.Value.Color != null)
                    {
                        lst.Add(kv.Value);
                    }
                }

                m_cm          = (new ColorManager()).Global;
                Palette       = Palette.Create(new RGBColorWheel(), Colors.White, PaletteSchemaType.Custom, lst.Count);
                ColorNameList = new ObservableCollection<string>();

                for (int i = 0; i < lst.Count; ++i)
                {
                    var pc = lst[i];

                    Palette.Colors[i].Name = pc.Title;
                    Palette.Colors[i].Tag = pc;
                    Palette.Colors[i].RgbColor = pc.Color.Value;
                    Palette.Colors[i].PropertyChanged += OnColorChanged;

                    ColorNameList.Add(pc.Name);
                }
                FirePropertyChanged("ColorNameList");

                DataContext     = this;
                m_selectedColor = 0;

                UpdateSelectedColor();
            }
        }

        public ObservableCollection<string> ColorNameList
        {
            get;
            set;
        }

        public string SelectedColorName
        {
            get;
            set;
        }

        private void UpdateSelectedColor(
        )
        {
            var tc  = Palette.Colors[m_selectedColor].Tag as TemplateColor;
            var idx = selectedColorEffect.Items.IndexOf(tc.Effect);

            if (SelectedColorName != tc.Name)
            {
                SelectedColorName = tc.Name;
                FirePropertyChanged("SelectedColorName");
            }
            SelectedColorEffect = idx;
        }

        public int SelectedColorEffect
        {
            get
            {
                return m_selectedColorEffect;
            }

            set
            {
                if (value == -1)
                {
                    value = 0;
                }

                if (m_selectedColorEffect != value)
                {
                    var tc = (Palette.Colors[m_selectedColor].Tag as TemplateColor);
                    string effect = tc.Effect;

                    if (value == 0)
                    {
                        effect = "";
                    }
                    else
                    {
                        effect = selectedColorEffect.Items[value].ToString();
                    }

                    if (tc.Effect != effect)
                    {
                        tc.Effect = effect;    
                        ColorManager.Instance.ClearCache();
                    }

                    if (m_selectedColorEffect != value)
                    {
                        m_selectedColorEffect = value;
                        FirePropertyChanged("SelectedColorEffect");
                    }
                }
            }
        }

        private void Window_Activated(
            object                                      sender, 
            EventArgs                                   e
        )
        {
        }

        private void timeline_Loaded(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
        }

        private void slider1_ValueChanged(
            object                                      sender, 
            RoutedPropertyChangedEventArgs<double>      e
        )
        {
            timeline.CurrentDateTime = timeline.MinDateTime + new TimeSpan((int) e.NewValue, 0, 0, 0);
        }

        private void button1_Click(
            object                                      sender, 
            RoutedEventArgs                             e
        )
        {
            timeline.ClearEvents();

            var monetXml = this.GetResourceTextFile("Monet.xml");
            timeline.ResetEvents(monetXml);
        }

        private void timeline_TimelineReady(
            object                                      sender, 
            EventArgs                                   e
        )
        {
            var monetXml = this.GetResourceTextFile("Monet.xml");
            timeline.ResetEvents(monetXml);
            CollectionChangedEventManager.AddListener(timeline.SelectedTimelineEvents, this);
        }

		private void Button2Click(object sender, RoutedEventArgs e)
		{
		}

        private void FirePropertyChanged(
            string                                      name
        )
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public Palette Palette
        {
            get
            {
                return m_palette;
            }
            set
            {
                m_palette = value;
                FirePropertyChanged("Palette");
            }
        }

        #region IWeakEventListener Members

        public bool ReceiveWeakEvent(
            Type                                        managerType, 
            object                                      sender, 
            EventArgs                                   e
        )
        {
            selectedCount.GetBindingExpression(Label.ContentProperty).UpdateTarget();
            return true;
        }

        #endregion

        private void ColorWheelControl_ColorSelected(
            object                                      sender, 
            ColorWheel.Controls.EventArg<int>           e
        )
        {
            m_selectedColor = e.Value;
            UpdateSelectedColor();
        }

        private void BrightnessSaturationControl_SelectColored(
            object                                      sender, 
            ColorWheel.Controls.EventArg<int>           e
        )
        {
            m_selectedColor = e.Value;
            UpdateSelectedColor();
        }

        private void selectedColor_SelectionChanged(
            object                                      sender, 
            SelectionChangedEventArgs                   e
        )
        {
            if (e.AddedItems.Count > 0)
            {
                var val = e.AddedItems[0] as string;
                var idx = ColorNameList.IndexOf(val);

                m_selectedColor = idx;
                UpdateSelectedColor();
            }
        }

        /// <summary>
        /// Gets the resource text file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        private string GetResourceTextFile(string filename)
        {
            string result = string.Empty;            

            using (Stream stream = this.GetType().Assembly.
                       GetManifestResourceStream("WpfTimelineExample." + filename))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
    }
}
