using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configurator
{
    public static class Constants
    {
        public const float MAX_ASPECT_RATIO_STRETCH = 10000;
        public const float MAX_ASPECT_RATIO_DEFAULT = 0.05F;

        public static readonly String START_MENU_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Start Menu\Applications";
        public static readonly String ENTRYPOINTS_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Extensibility\Entry Points";
        public static readonly String MB_CONFIG_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Extensibility\Categories\Settings\Other";
        public static readonly String CATEGORIES_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Extensibility\Categories";
        public static readonly String APPLICATION_ID = @"{ce32c570-4bec-4aeb-ad1d-cf47b91de0b2}";
        public static readonly String MB_MAIN_ENTRYPOINT_GUID = @"{fc9abccc-36cb-47ac-8bab-03e8ef5f6f22}";
        public static readonly String MB_CONFIG_ENTRYPOINT_GUID = @"{b8f02923-484e-483e-b227-e5a810c77724}";

        public static readonly String VISTA_TV_MOVIES_GUID = "{dde82191-ea23-4606-9d63-b92c809c31bb}";
        public static readonly String MC_INTERNAL_START_MENU_REGISTRY_PATH = @"Internal\" + VISTA_TV_MOVIES_GUID;
        public static readonly String ON_STARTMENU_KEY = "OnStartMenu";
        public static readonly String VISTA_TV_MOVIES_DISPLAY_NAME = "TV + Movies";
        public static readonly String VISTA_TV_MOVIES_START_MENU_CATEGORY = @"Services\TV";

        public const int MAX_ITEMS_IN_MENU_STRIP_VISTA = 4;
        public const int MAX_ITEMS_IN_MENU_STRIP_WIN7 = 5;
        public static readonly String HKEY_LOCAL_MACHINE = @"HKEY_LOCAL_MACHINE";
    }
}
