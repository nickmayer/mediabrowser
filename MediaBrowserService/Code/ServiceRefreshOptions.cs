namespace MediaBrowserService.Code
{
    public class ServiceRefreshOptions
    {
        public bool IncludeImagesOption { get; set; }

        public bool IncludeGenresOption { get; set; }

        public bool IncludeStudiosOption { get; set; }

        public bool IncludePeopleOption { get; set; }

        public bool IncludeYearOption { get; set; }

        public bool ClearCacheOption { get; set; }

        public bool ClearImageCacheOption { get; set; }

        public bool MigrateOption { get; set; }

        public bool AllowCancel = true;

        public bool AnyImageOptionsSelected
        {
            get
            {
                return IncludeImagesOption || IncludeGenresOption || IncludePeopleOption || IncludeStudiosOption || IncludeYearOption;
            }
        }
    }
}
