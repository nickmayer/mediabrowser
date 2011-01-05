namespace MediaBrowserService.Code
{
    public class ServiceGuiOptions
    {
        public bool IncludeImagesOption { get; set; }

        public bool IncludeGenresOption { get; set; }

        public bool IncludeStudiosOption { get; set; }

        public bool IncludePeopleOption { get; set; }

        public bool IncludeYearOption { get; set; }

        public bool ClearCacheOption { get; set; }

        public bool AnyImageOptionsSelected
        {
            get
            {
                return IncludeImagesOption || IncludeGenresOption || IncludePeopleOption || IncludeStudiosOption || IncludeYearOption;
            }
        }
    }
}
