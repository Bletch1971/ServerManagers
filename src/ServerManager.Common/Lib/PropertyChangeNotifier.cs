using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace ServerManagerTool.Common.Lib
{
    public sealed class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        #region Member Variables

        private WeakReference _propertySource;

        #endregion // Member Variables

        #region Constructor

        public PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property))
        {
        }
        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property, DependencyPropertyChangedEventHandler handler)
            : this(propertySource, property)
        {
            this.ValueChanged += handler;
        }

        public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
        {
            if (null == propertySource)
                throw new ArgumentNullException("propertySource");
            if (null == property)
                throw new ArgumentNullException("property");
            this._propertySource = new WeakReference(propertySource);
            Binding binding = new Binding();
            binding.Path = property;
            binding.Mode = BindingMode.OneWay;
            binding.Source = propertySource;
            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        public static IEnumerable<PropertyChangeNotifier> GetNotifiers(DependencyObject propertySource, IEnumerable<DependencyProperty> properties, DependencyPropertyChangedEventHandler handler)
        {
            foreach(var property in properties)
            {
                var notifier = new PropertyChangeNotifier(propertySource, property, handler);
                yield return notifier;
            }
        }

        #endregion // Constructor

        #region PropertySource

        public DependencyObject PropertySource
        {
            get
            {
                try
                {
                    // note, it is possible that accessing the target property
                    // will result in an exception so i’ve wrapped this check
                    // in a try catch
                    return this._propertySource.IsAlive
                    ? this._propertySource.Target as DependencyObject
                    : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        #endregion // PropertySource

        #region Value

        /// <summary>
        /// Identifies the <see cref=”Value”/> dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value),
        typeof(object), typeof(PropertyChangeNotifier), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPropertyChanged)));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyChangeNotifier notifier = (PropertyChangeNotifier)d;
            if (null != notifier.ValueChanged)
            {
                notifier.ValueChanged(notifier.PropertySource, e);
            }
        }

        /// <summary>
        /// Returns/sets the value of the property
        /// </summary>
        /// <seealso cref=”ValueProperty”/>
        [Description("Returns/sets the value of the property")]
        [Category("Behavior")]
        [Bindable(true)]
        public object Value
        {
            get
            {
                return (object)this.GetValue(PropertyChangeNotifier.ValueProperty);
            }
            set
            {
                this.SetValue(PropertyChangeNotifier.ValueProperty, value);
            }
        }

        #endregion //Value

        #region Events

        public event DependencyPropertyChangedEventHandler ValueChanged;

        #endregion // Events

        #region IDisposable Members

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }

        #endregion
    }
}
