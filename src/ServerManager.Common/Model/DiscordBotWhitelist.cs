using System.Windows;

namespace ServerManagerTool.Common.Model
{
    public class DiscordBotWhitelist : DependencyObject
    {
        public static readonly DependencyProperty BotIdProperty = DependencyProperty.Register(nameof(BotId), typeof(string), typeof(DiscordBotWhitelist), new PropertyMetadata(""));

        public string BotId
        {
            get { return (string)GetValue(BotIdProperty); }
            set { SetValue(BotIdProperty, value); }
        }
    }
}
