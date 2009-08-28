using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Windows;
using System.Collections;
using System.IO;

namespace Configurator
{
    #region Class StartMenuRegistryEditor
    public class StartMenuRegistryEditor
    { 
        #region Public Methods

        public void EnableMultipleEntry()
        {
            try
            {
                RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);
                RegistryItem regKey_Enabled = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, true.ToString());
                RegistryItem regKey_Disabled = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, false.ToString());

                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(Constants.MB_MAIN_ENTRYPOINT_GUID, true);
                this.WriteValue(EntryPointsSubKey, regKey_Disabled);

                EntryPointsSubKey = EntryPointsTree.OpenSubKey(Constants.MB_CONFIG_ENTRYPOINT_GUID, true);
                this.WriteValue(EntryPointsSubKey, regKey_Enabled);


                foreach (var Key in EntryPointsTree.GetSubKeyNames())
                {
                    try
                    {
                        if (Key == Constants.MB_MAIN_ENTRYPOINT_GUID || Key == Constants.MB_CONFIG_ENTRYPOINT_GUID)
                        {
                            continue;
                        }

                        if (this.FetchAppID(Key).ToLower() == Constants.APPLICATION_ID.ToLower())
                        {
                            EntryPointsSubKey = EntryPointsTree.OpenSubKey(Key, true);

                            if (EntryPointsSubKey != null)
                            {
                                RegistryItem SavedEnabledStatus;
                                try
                                {
                                    SavedEnabledStatus = this.ReadValue(EntryPointsSubKey, EntryPointItem.SavedEnabledName);
                                }
                                catch (Exception ex)
                                {
                                    SavedEnabledStatus = new RegistryItem(EntryPointItem.SavedEnabledName, RegistryValueKind.String, false.ToString());
                                    try
                                    {
                                        this.WriteValue(EntryPointsSubKey, SavedEnabledStatus);
                                    }
                                    catch (Exception e)
                                    { }
                                }
                                RegistryItem EnabledStatus = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, SavedEnabledStatus.Value);
                                this.WriteValue(EntryPointsSubKey, EnabledStatus);
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                }               
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void DisableMultipleEntry()
        {
            try
            {
                RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);
                RegistryItem regKey_Enabled = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, true.ToString());
                RegistryItem regKey_Disabled = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, false.ToString());

                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(Constants.MB_MAIN_ENTRYPOINT_GUID, true);
                this.WriteValue(EntryPointsSubKey, regKey_Enabled);

                EntryPointsSubKey = EntryPointsTree.OpenSubKey(Constants.MB_CONFIG_ENTRYPOINT_GUID, true);
                this.WriteValue(EntryPointsSubKey, regKey_Disabled);

                foreach (var Key in EntryPointsTree.GetSubKeyNames())
                {
                    try
                    {
                        if (Key == Constants.MB_MAIN_ENTRYPOINT_GUID || Key == Constants.MB_CONFIG_ENTRYPOINT_GUID)
                        {
                            continue;
                        }

                        if (this.FetchAppID(Key).ToLower() == Constants.APPLICATION_ID.ToLower())
                        {
                            EntryPointsSubKey = EntryPointsTree.OpenSubKey(Key, true);

                            if (EntryPointsSubKey != null)
                            {
                                RegistryItem EnabledStatus;
                                try
                                {
                                    EnabledStatus = this.ReadValue(EntryPointsSubKey, EntryPointItem.EnabledName);
                                }
                                catch (Exception ex)
                                {
                                    EnabledStatus = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, false.ToString());
                                }
                                RegistryItem SavedEnabledStatus = new RegistryItem(EntryPointItem.SavedEnabledName, RegistryValueKind.String, EnabledStatus.Value);
                                this.WriteValue(EntryPointsSubKey, SavedEnabledStatus);

                                this.WriteValue(EntryPointsSubKey, regKey_Disabled);
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                }
               
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SwapTimeStamp(MCEntryPointItem ep1, MCEntryPointItem ep2)
        {
            try
            {
                String timeStampTemp = ep1.Values.TimeStamp.Value;

                ep1.Values.TimeStamp.Value = ep2.Values.TimeStamp.Value;

                ep2.Values.TimeStamp.Value = timeStampTemp;
            }
            catch (Exception ex)
            {
                throw new Exception("Error swapping timestamps. Error: " + ex.Message);
            }
        }
        
        public String FetchAppID(String EntrypointGUID)
        {
            String AppID = String.Empty;            

            try
            {
                RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);
                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(EntrypointGUID);
                AppID = this.ReadValue(EntryPointsSubKey, EntryPointItem.AppIdName).Value.ToString();
            }
            catch (Exception ex)
            {
                AppID = String.Empty;
            }
            return AppID;
        }

        public bool IsMultipleEntryPointsEnabled()
        {
            try
            {
                RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH + @"\" + Constants.MB_MAIN_ENTRYPOINT_GUID );
                bool isEnabled = Convert.ToBoolean(this.ReadValue(EntryPointsTree, EntryPointItem.EnabledName).Value);
                return isEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error detecting if Multiple EntryPoints are enabled. " + ex.Message);
                return true;// Default to true
            }
        }
        public MCEntryPointItem FetchEntryPoint(String EntryPointUID)
        {
            RegistryKey StartMenuCategory = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH);
            RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);

            MCEntryPointItem EntryPoint = new MCEntryPointItem();
            try
            {
                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(EntryPointUID);
                if (EntryPointsSubKey != null)
                {
                    EntryPoint.EntryPointUID = EntryPointUID;
                    
                    try
                    {
                        EntryPoint.Values.Enabled = this.ReadValue(EntryPointsSubKey, EntryPointItem.EnabledName);
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.Enabled = new RegistryItem(EntryPointItem.EnabledName, RegistryValueKind.String, "false");
                    }

                    try
                    {
                        EntryPoint.Values.AppID = this.ReadValue(EntryPointsSubKey, EntryPointItem.AppIdName);                        
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.AppID = new RegistryItem(EntryPointItem.AppIdName, RegistryValueKind.String, String.Empty);
                    }
                    try
                    {
                        EntryPoint.Values.AddIn = this.ReadValue(EntryPointsSubKey, EntryPointItem.AddInName);                        
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.AddIn = new RegistryItem(EntryPointItem.AppIdName, RegistryValueKind.String, String.Empty);
                    }
                    try
                    {
                        EntryPoint.Values.Context = this.ReadValue(EntryPointsSubKey, EntryPointItem.ContextName);
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.Context = new RegistryItem(EntryPointItem.ContextName, RegistryValueKind.ExpandString, String.Empty);                        
                    }

                    try
                    {
                        EntryPoint.Values.Description = this.ReadValue(EntryPointsSubKey, EntryPointItem.DescriptionName);
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.Description = new RegistryItem(EntryPointItem.DescriptionName, RegistryValueKind.ExpandString, String.Empty);
                        
                    }

                    try
                    {
                        EntryPoint.Values.ImageUrl = this.ReadValue(EntryPointsSubKey, EntryPointItem.ImageUrlName);
                        if (!new FileInfo(new Uri(EntryPoint.Values.ImageUrl.Value).LocalPath).Exists)
                        {
                            EntryPoint.Values.ImageUrl.Value = String.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.ImageUrl = new RegistryItem(EntryPointItem.ImageUrlName, RegistryValueKind.ExpandString, String.Empty);
                    }

                    try
                    {
                        EntryPoint.Values.InactiveImageUrl = this.ReadValue(EntryPointsSubKey, EntryPointItem.InactiveImageUrlName);
                        if (!new FileInfo(new Uri(EntryPoint.Values.InactiveImageUrl.Value).LocalPath).Exists)
                        {
                            EntryPoint.Values.InactiveImageUrl = new RegistryItem(EntryPointItem.InactiveImageUrlName, RegistryValueKind.ExpandString, String.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.InactiveImageUrl = new RegistryItem(EntryPointItem.InactiveImageUrlName, RegistryValueKind.ExpandString, String.Empty);
                    }

                    

                    try
                    {
                        EntryPoint.Values.Title = this.ReadValue(EntryPointsSubKey, EntryPointItem.TitleName);
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.Values.Title = new RegistryItem(EntryPointItem.TitleName, RegistryValueKind.ExpandString, String.Empty);
                        EntryPoint.Values.Enabled.Value = false.ToString();
                    }

                    try
                    {
                        String[] StartMenuCategories = GetStartMenuCategory(EntryPointUID);

                        if (StartMenuCategories.Length == 1)
                        {
                            EntryPoint.StartMenuCategory = StartMenuCategories[0];
                        }
                        else if (StartMenuCategories.Length > 1)
                        {
                            String locations = String.Empty;
                            foreach (String s in StartMenuCategories)
                            {
                                locations += "\"" + s + "\"" + ", ";
                            }
                            locations = locations.TrimEnd(',').Trim();
                            MessageBox.Show("Warning, EntryPoint " + EntryPointUID + " is in multiple Start menu locations. Located in: " + locations + ". Will be using entrypoint from the menu strip \"" + StartMenuCategories[0] + "\".");
                            EntryPoint.StartMenuCategory = StartMenuCategories[0];
                        }
                        else if (StartMenuCategories.Length == 0)
                        {
                            EntryPoint.StartMenuCategory = String.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.StartMenuCategory = String.Empty;
                        EntryPoint.Values.Enabled.Value = false.ToString();
                    }

                    try
                    {
                        RegistryKey StartMenuSubKey = StartMenuCategory.OpenSubKey(EntryPoint.StartMenuCategory + @"\" + EntryPoint.EntryPointUID);

                        if (StartMenuSubKey != null)
                        {
                            EntryPoint.StartMenuCategoryAppId = this.ReadValue(StartMenuSubKey, EntryPointItem.AppIdName).Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        EntryPoint.StartMenuCategoryAppId = new RegistryItem(EntryPointItem.AppIdName, RegistryValueKind.String, String.Empty).Value;
                        EntryPoint.StartMenuCategory = String.Empty;
                        EntryPoint.Values.Enabled.Value = false.ToString();
                    }

                    try
                    {
                        RegistryKey StartMenuSubKey = StartMenuCategory.OpenSubKey(EntryPoint.StartMenuCategory + @"\" + EntryPoint.EntryPointUID);

                        if (StartMenuSubKey != null)
                        {
                            EntryPoint.Values.TimeStamp = this.ReadValue(StartMenuSubKey, EntryPointItem.TimeStampName);
                        }
                        else
                        {
                            throw new Exception("Create value for timestamp");
                        }
                    }
                    catch (Exception ex)
                    {
                        String timeStamp = "0";
                        try
                        {
                            timeStamp = (new Random()).Next(1,100).ToString();
                        }
                        catch(Exception e){}
                        EntryPoint.Values.TimeStamp = new RegistryItem(EntryPointItem.TimeStampName, RegistryValueKind.DWord, timeStamp);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching entry point for " + EntryPoint.EntryPointUID + ". " + ex.Message);
            }

            return EntryPoint;
        }       
        
        public List<MCEntryPointItem> FetchMediaBrowserEntryPoints()
        {  
            List<MCEntryPointItem> EntryPointItems = new List<MCEntryPointItem>();

            RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);

            foreach (var Key in EntryPointsTree.GetSubKeyNames())
            {
                try
                {
                    if (Key == Constants.MB_MAIN_ENTRYPOINT_GUID || Key == Constants.MB_CONFIG_ENTRYPOINT_GUID)
                    {
                        continue;
                    }

                    if (this.FetchAppID(Key).ToLower() == Constants.APPLICATION_ID.ToLower())
                    {
                        MCEntryPointItem entryPoint = FetchEntryPoint(Key);

                        if (entryPoint != null)
                        {
                            EntryPointItems.Add(entryPoint);
                        }
                    }
                }
                catch (Exception ex)
                { }
            }
            
            return EntryPointItems;
        }

        public void SaveEntryPoint(MCEntryPointItem entryPoint)
        {
            RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH);

            try
            {
                if (!this.TestRegistryAccess())
                {
                    throw new Exception("This account does not have suffisant privileges to write to the registry.");
                }

                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(entryPoint.EntryPointUID, true);

                if (EntryPointsSubKey != null)
                {
                    //if (entryPoint.StartMenuCategory != String.Empty)
                    {
                        try
                        {
                            MCEntryPointItem prevValues = FetchEntryPoint(entryPoint.EntryPointUID);
                            RegistryKey newStartMenuLocation = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH + @"\" + entryPoint.StartMenuCategory + @"\" + entryPoint.EntryPointUID, true);
                            RegistryKey oldStartMenuLocation = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH + @"\" + prevValues.StartMenuCategory, true);
                            if (prevValues.StartMenuCategory.ToUpper() != entryPoint.StartMenuCategory.ToUpper())
                            {
                                try
                                {                                    
                                    oldStartMenuLocation.DeleteSubKey(entryPoint.EntryPointUID);
                                }
                                catch (Exception ex)
                                { }

                                if (newStartMenuLocation == null && entryPoint.StartMenuCategory != String.Empty)// key doesn't already exist
                                {
                                    newStartMenuLocation = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH + @"\" + entryPoint.StartMenuCategory, true);
                                    newStartMenuLocation = newStartMenuLocation.CreateSubKey(entryPoint.EntryPointUID);
                                    this.WriteValue(newStartMenuLocation, new RegistryItem(EntryPointItem.AppIdName, RegistryValueKind.String, entryPoint.StartMenuCategoryAppId));

                                    //if (prevValues.Values.TimeStamp == null || prevValues.Values.TimeStamp.Value == "0")
                                    //{
                                    //    entryPoint.Values.TimeStamp = new RegistryItem(EntryPointItem.TimeStampName, RegistryValueKind.DWord, "0");
                                    //    entryPoint.Values.TimeStamp.Value = (new Random()).Next(1, 100).ToString();
                                    //}
                                    //else
                                    {
                                        entryPoint.Values.TimeStamp = prevValues.Values.TimeStamp;
                                    }
                                }
                            }
                            if (newStartMenuLocation != null)
                            {
                                this.WriteValue(newStartMenuLocation, entryPoint.Values.TimeStamp);                                
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error saving start menu location. Entrypoint not saved. " + ex.Message);
                            return;
                        }
                    }

                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.Context);
                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.Description);
                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.ImageUrl);
                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.InactiveImageUrl);
                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.Title);
                    this.WriteValue(EntryPointsSubKey, entryPoint.Values.Enabled);                   
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving entry point for " + entryPoint.EntryPointUID + ". " + ex.Message);
            }
        }
        
        public MCEntryPointItem CreateNewEntryPoint(String Title, String Context)
        {            
            MCEntryPointItem mcp = new MCEntryPointItem();
            mcp.EntryPointUID = "{" + this.CreateGuid().ToString() + "}";
            mcp.Values.Context.Value = Context;

            RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH, true);
            RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(mcp.EntryPointUID, true);

            if (EntryPointsSubKey == null)
            {
                EntryPointsSubKey = EntryPointsTree.CreateSubKey(mcp.EntryPointUID);
            }

            MCEntryPointItem MainEP = this.FetchEntryPoint(Constants.MB_MAIN_ENTRYPOINT_GUID);

            this.WriteValue(EntryPointsSubKey, MainEP.Values.AppID);
            this.WriteValue(EntryPointsSubKey, MainEP.Values.AddIn);

            
            mcp.Values.Title.Value = Title;
            mcp.Values.Description.Value = MainEP.Values.Description.Value;
            mcp.Values.ImageUrl.Value = MainEP.Values.ImageUrl.Value;
            mcp.Values.InactiveImageUrl.Value = MainEP.Values.InactiveImageUrl.Value;
            this.SaveEntryPoint(mcp);

           

            return this.FetchEntryPoint(mcp.EntryPointUID);
        }

        public void DeleteEntryPointKey(String EntryPointGuid)
        {
            StartMenuRegistryEditor smre = new StartMenuRegistryEditor();
            MCEntryPointItem entryPoint = smre.FetchEntryPoint(EntryPointGuid);

            try
            {
                RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH, true);
                RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(EntryPointGuid);

                if (EntryPointsSubKey != null)
                {
                    EntryPointsTree.DeleteSubKey(EntryPointGuid);
                }
            }
            catch (Exception ex)
            { }
            try
            {
                if (entryPoint.StartMenuCategory != String.Empty)
                {
                    String StartMenuSubCategory = @"\" + entryPoint.StartMenuCategory;
                    RegistryKey EntryPointsTree = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH +  StartMenuSubCategory, true);
                    if (EntryPointsTree != null)
                    {
                        RegistryKey EntryPointsSubKey = EntryPointsTree.OpenSubKey(EntryPointGuid);

                        if (EntryPointsSubKey != null)
                        {
                            EntryPointsTree.DeleteSubKey(EntryPointGuid);
                        }
                    }
                }
            }
            catch (Exception ex)
            { }
            
        }

        public List<MediaCenterStartMenuItem> GetStartMenuItems()
        {
            List<MediaCenterStartMenuItem> menuItems = new List<MediaCenterStartMenuItem>();

            try
            {
                RegistryKey keys = Registry.LocalMachine.OpenSubKey(Constants.START_MENU_REGISTRY_PATH, false);

                foreach (String key in keys.GetSubKeyNames())
                {
                    try
                    {
                        Guid guid = new Guid(key.Trim().TrimStart('{').TrimEnd('}'));// Will throw exception if is not a GUID                                                

                        RegistryKey startMenuKey = keys.OpenSubKey(key);
                        MediaCenterStartMenuItem MenuItem = new MediaCenterStartMenuItem();

                        RegistryItem OnStartMenu = this.ReadValue(startMenuKey, Constants.ON_STARTMENU_KEY);
                        if (Convert.ToBoolean(OnStartMenu.Value.Trim()))
                        {
                            RegistryItem Title = this.ReadValue(startMenuKey, "Title");
                            RegistryItem Category = this.ReadValue(startMenuKey, "Category");

                            MenuItem.Name = Title.Value;
                            MenuItem.OnStartMenu = true;
                            MenuItem.StartMenuCategory = Category.Value;
                            MenuItem.Guid = "{" + guid.ToString() + "}";

                            RegistryKey StartMenuCategory = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH + @"\" + Category.Value);

                            if (StartMenuCategory != null)
                            {
                                menuItems.Add(MenuItem);
                            }
                        }

                    }
                    catch (Exception ex)
                    { }
                }

                try
                {
                    if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0)// Add tv + Movies. Only for Vista
                    {
                        RegistryKey TV_MoviesCategory = keys.OpenSubKey(Constants.MC_INTERNAL_START_MENU_REGISTRY_PATH);

                        if (TV_MoviesCategory != null)
                        {
                            RegistryItem OnStartMenu = this.ReadValue(TV_MoviesCategory, Constants.ON_STARTMENU_KEY);
                            if (OnStartMenu != null && Convert.ToBoolean(OnStartMenu.Value.Trim()))
                            {
                                MediaCenterStartMenuItem MenuItem = new MediaCenterStartMenuItem();

                                MenuItem.Name = Constants.VISTA_TV_MOVIES_DISPLAY_NAME;
                                MenuItem.OnStartMenu = true;
                                MenuItem.StartMenuCategory = Constants.VISTA_TV_MOVIES_START_MENU_CATEGORY;
                                MenuItem.Guid = Constants.VISTA_TV_MOVIES_GUID;
                                menuItems.Add(MenuItem);
                            }
                        }
                        else // If not in the registry, then TV + movies is enabled
                        {
                            MediaCenterStartMenuItem MenuItem = new MediaCenterStartMenuItem();

                            MenuItem.Name = Constants.VISTA_TV_MOVIES_DISPLAY_NAME;
                            MenuItem.OnStartMenu = true;
                            MenuItem.StartMenuCategory = Constants.VISTA_TV_MOVIES_START_MENU_CATEGORY;
                            MenuItem.Guid = Constants.VISTA_TV_MOVIES_GUID;
                            menuItems.Add(MenuItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                return menuItems;
            }
            catch (Exception ex)
            {
                return new List<MediaCenterStartMenuItem>();
            }
        }

        public bool TestRegistryAccess()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(Constants.ENTRYPOINTS_REGISTRY_PATH + @"\" + Constants.MB_MAIN_ENTRYPOINT_GUID, true);

                RegistryItem items = this.ReadValue(key, EntryPointItem.TimeStampName);
                this.WriteValue(key, items);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
        
        #endregion

        #region Private Methods
        private RegistryItem ReadValue(RegistryKey regKeyPath, String ValueKey)
        {
            try
            {
                if (regKeyPath.ValueCount == 0)
                {
                    throw new Exception("There are no value keys in the path " + regKeyPath.Name);
                }
                String Value = regKeyPath.GetValue(ValueKey).ToString();
                
                if (Value == null)
                {
                    throw new Exception("Reg key " + ValueKey + " does not exist in " + regKeyPath.Name);
                }

                RegistryValueKind KeyType = regKeyPath.GetValueKind(ValueKey);

                return new RegistryItem(ValueKey, KeyType, Value);
            }
            catch (Exception ex)
            {
                throw new Exception("Error recieving registry key. " + ex.Message);
            }
        }
        
        private void WriteValue(RegistryKey regKeyPath, RegistryItem regItem)
        {
            try
            {
                if (regKeyPath == null)
                {
                    throw new Exception("RegKeyPath is null for item " + regItem.Name + ".");
                }
                if (regItem.Name == String.Empty || regItem.type == null)
                {
                    throw new Exception("One or more of the values in " + regItem.Name + " are incomplete.");
                }
                
                try
                {
                    regKeyPath.SetValue(regItem.Name, regItem.Value, regItem.type);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error writing " + regItem.Value + " to " + regItem.Name + " of type " + regItem.type.ToString() + " in the path of " + regKeyPath.Name + ". " + ex.Message);
                }                 
            }
            catch (Exception ex)
            {
                throw new Exception("Error writing registry key. " + ex.Message);
            }
        }
        
        private String[] GetStartMenuCategory(String EntryPointGUID)
        {
            try
            {
                RegistryKey CategoryKey = Registry.LocalMachine.OpenSubKey(Constants.CATEGORIES_REGISTRY_PATH, false);
                List<String> cat = GetStartMenuCategoryRecursive(CategoryKey, EntryPointGUID);
               
                return cat.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<String> GetStartMenuCategoryRecursive(RegistryKey key, String GUID)
        {
            try
            {
                List<String> matchesFound = new List<String>();

                String[] KeyNameSplit = key.Name.Split('\\');
                String KeyName = KeyNameSplit[KeyNameSplit.Length - 1];
                if (KeyName.ToUpper() == GUID.ToUpper())
                {
                    String CatPath = key.Name.Replace(Constants.HKEY_LOCAL_MACHINE + @"\" + Constants.CATEGORIES_REGISTRY_PATH, String.Empty);
                    CatPath = CatPath.Replace(GUID, String.Empty);
                    CatPath = CatPath.TrimStart('\\').TrimEnd('\\');
                    matchesFound.Add(CatPath);
                    return matchesFound;
                }

                foreach (String item in key.GetSubKeyNames())
                {
                    List<String> s = GetStartMenuCategoryRecursive(key.OpenSubKey(item), GUID);
                    matchesFound.AddRange(s);
                }

                return matchesFound;
            }
            catch (Exception ex)
            {
                return new List<String>();
            }
        }

        private Guid CreateGuid()
        {
            Guid guid = System.Guid.NewGuid();
            return guid;
        }

        #endregion
    }
    #endregion

    #region Class MediaCenterStartMenuItem
    public class MediaCenterStartMenuItem
    {
        public String Name = String.Empty;
        public String StartMenuCategory = String.Empty;
        public bool OnStartMenu = false;
        public String Guid = String.Empty;

        public override string ToString()
        {
            return Name;
        }
    }
    #endregion

    #region Class MCEntryPointItem
    public class MCEntryPointItem : IComparable
    {        
        public String EntryPointUID = String.Empty;
        public String StartMenuCategory = String.Empty;
        public String StartMenuCategoryAppId = String.Empty;
        public EntryPointItem Values = new EntryPointItem();        

        int IComparable.CompareTo(Object other)
        {
            return Convert.ToInt32(this.Values.TimeStamp.Value).CompareTo(Convert.ToInt32(((MCEntryPointItem)other).Values.TimeStamp.Value));           
        }

        public override string ToString()
        {
            return this.Values.Title.Value;
        }
    }
    #endregion

    #region Class EntryPointItem
    public class EntryPointItem
    {
        public static readonly String AppIdName = "AppID";
        public static readonly String AddInName = "AddIn";
        public static readonly String DescriptionName = "Description";
        public static readonly String ContextName = "Context";
        public static readonly String ImageUrlName = "ImageUrl";
        public static readonly String InactiveImageUrlName = "InactiveImageUrl";
        public static readonly String TimeStampName = "TimeStamp";
        public static readonly String TitleName = "Title";
        public static readonly String EnabledName = "Enabled";
        public static readonly String SavedEnabledName = "SavedEnabledStatus";

        public RegistryItem AppID = new RegistryItem(AppIdName, RegistryValueKind.String, Constants.APPLICATION_ID);
        public RegistryItem AddIn = new RegistryItem(AddInName, RegistryValueKind.ExpandString, String.Empty);        
        public RegistryItem Description = new RegistryItem(DescriptionName, RegistryValueKind.ExpandString, String.Empty);
        public RegistryItem Context = new RegistryItem(ContextName, RegistryValueKind.ExpandString, String.Empty);
        public RegistryItem ImageUrl = new RegistryItem(ImageUrlName, RegistryValueKind.ExpandString, String.Empty);
        public RegistryItem InactiveImageUrl = new RegistryItem(InactiveImageUrlName, RegistryValueKind.ExpandString, String.Empty);
        public RegistryItem TimeStamp = new RegistryItem(TimeStampName, RegistryValueKind.DWord, "0");
        public RegistryItem Title = new RegistryItem(TitleName, RegistryValueKind.ExpandString, String.Empty);
        public RegistryItem Enabled = new RegistryItem(EnabledName, RegistryValueKind.String, "false");

        public override string ToString()
        {
            return this.Title.Name;
        }        
    }
    #endregion

    #region Class RegistryItem
    public class RegistryItem
    {
        private String _Name = String.Empty;
        private RegistryValueKind _type;
        private String _Value = String.Empty;

        public String Name { get { return this._Name; } }
        public RegistryValueKind type { get { return this._type; } }
        public String Value { set { this._Value = value; } get { return this._Value; } }
        
        public RegistryItem(String Name, RegistryValueKind type, String Value)
        {            
            this._Name = Name;
            this._type = type;
            this._Value = Value;

            if (this._Value.ToLower() == "null".ToLower())
            {
                this._Value = String.Empty;
            }
        }        
    }
    #endregion
}
