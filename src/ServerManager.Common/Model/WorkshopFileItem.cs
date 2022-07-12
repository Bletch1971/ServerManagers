using ServerManagerTool.Common.Utils;
using System;
using System.Windows;

namespace ServerManagerTool.Common.Model
{
    public class WorkshopFileItem : DependencyObject
    {
        public static readonly DependencyProperty AppIdProperty = DependencyProperty.Register(nameof(AppId), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CreatedDateProperty = DependencyProperty.Register(nameof(CreatedDate), typeof(DateTime), typeof(WorkshopFileItem), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty FileSizeProperty = DependencyProperty.Register(nameof(FileSize), typeof(long), typeof(WorkshopFileItem), new PropertyMetadata(-1L));
        public static readonly DependencyProperty SubscriptionsProperty = DependencyProperty.Register(nameof(Subscriptions), typeof(int), typeof(WorkshopFileItem), new PropertyMetadata(0));
        public static readonly DependencyProperty TimeUpdatedProperty = DependencyProperty.Register(nameof(TimeUpdated), typeof(int), typeof(WorkshopFileItem), new PropertyMetadata(0));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty UpdatedDateProperty = DependencyProperty.Register(nameof(UpdatedDate), typeof(DateTime), typeof(WorkshopFileItem), new PropertyMetadata(DateTime.MinValue));
        public static readonly DependencyProperty WorkshopIdProperty = DependencyProperty.Register(nameof(WorkshopId), typeof(string), typeof(WorkshopFileItem), new PropertyMetadata(string.Empty));

        public string AppId
        {
            get { return (string)GetValue(AppIdProperty); }
            set { SetValue(AppIdProperty, value); }
        }

        public DateTime CreatedDate
        {
            get { return (DateTime)GetValue(CreatedDateProperty); }
            set { SetValue(CreatedDateProperty, value); }
        }

        public long FileSize
        {
            get { return (long)GetValue(FileSizeProperty); }
            set { SetValue(FileSizeProperty, value); }
        }

        public int Subscriptions
        {
            get { return (int)GetValue(SubscriptionsProperty); }
            set { SetValue(SubscriptionsProperty, value); }
        }

        public int TimeUpdated
        {
            get { return (int)GetValue(TimeUpdatedProperty); }
            set { SetValue(TimeUpdatedProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set
            {
                SetValue(TitleProperty, value);

                TitleFilterString = value?.ToLower();
            }
        }

        public DateTime UpdatedDate
        {
            get { return (DateTime)GetValue(UpdatedDateProperty); }
            set { SetValue(UpdatedDateProperty, value); }
        }

        public string WorkshopId
        {
            get { return (string)GetValue(WorkshopIdProperty); }
            set { SetValue(WorkshopIdProperty, value); }
        }


        public string TitleFilterString
        {
            get;
            private set;
        }

        public string WorkshopUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId}";

        public static WorkshopFileItem GetItem(WorkshopFileDetail item)
        {
            if (string.IsNullOrWhiteSpace(item.publishedfileid) || string.IsNullOrWhiteSpace(item.title))
                return null;

            var result = new WorkshopFileItem();
            result.AppId = item.creator_appid;
            result.CreatedDate = DateTimeUtils.UnixTimeStampToDateTime(item.time_created);
            result.FileSize = -1;
            result.Subscriptions = item.subscriptions;
            result.TimeUpdated = item.time_updated;
            result.Title = item.title ?? string.Empty;
            result.UpdatedDate = DateTimeUtils.UnixTimeStampToDateTime(item.time_updated);
            result.WorkshopId = item.publishedfileid ?? string.Empty;

            long fileSize;
            if (long.TryParse(item.file_size, out fileSize))
                result.FileSize = fileSize;

            return result;
        }

        public override string ToString()
        {
            return $"{WorkshopId} - {Title}";
        }
    }
}
