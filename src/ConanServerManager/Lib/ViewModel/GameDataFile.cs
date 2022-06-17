using System;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class GameDataFile : DependencyObject
    {
        public static readonly DependencyProperty CreatedDateProperty = DependencyProperty.Register(nameof(CreatedDate), typeof(DateTime), typeof(GameDataFile), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register(nameof(File), typeof(string), typeof(GameDataFile), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(GameDataFile), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty IsUserDataProperty = DependencyProperty.Register(nameof(IsUserData), typeof(bool), typeof(GameDataFile), new PropertyMetadata(true));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(nameof(Version), typeof(string), typeof(GameDataFile), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty HasErrorProperty = DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(GameDataFile), new PropertyMetadata(false));

        public DateTime CreatedDate
        {
            get { return (DateTime)GetValue(CreatedDateProperty); }
            set { SetValue(CreatedDateProperty, value); }
        }

        public string File
        {
            get { return (string)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public bool IsUserData
        {
            get { return (bool)GetValue(IsUserDataProperty); }
            set { SetValue(IsUserDataProperty, value); }
        }

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        public bool HasError
        {
            get { return (bool)GetValue(HasErrorProperty); }
            set { SetValue(HasErrorProperty, value); }
        }
    }
}
