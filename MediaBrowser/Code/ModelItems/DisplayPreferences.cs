﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;
using System.Collections;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library
{


    public class DisplayPreferences : BaseModelItem
    {
        private static readonly byte Version = 3;

        readonly Choice viewType = new Choice();
        readonly BooleanChoice showLabels;
        readonly BooleanChoice verticalScroll;
        readonly Choice sortOrders = new Choice();
        readonly Choice indexBy = new Choice();
        readonly BooleanChoice useBanner;
        readonly BooleanChoice useCoverflow;
        readonly BooleanChoice useBackdrop;
        private bool saveEnabled = true;
        SizeRef thumbConstraint = new SizeRef(Config.Instance.DefaultPosterSize);
        private Dictionary<string, IComparer<BaseItem>> sortDict;
        private Dictionary<string, string> indexDict;

        public Guid Id { get; set; }

        public DisplayPreferences(Guid id, Folder folder)
        {
            this.Id = id;

            ArrayList list = new ArrayList();
            foreach (ViewType v in Enum.GetValues(typeof(ViewType)))
                list.Add(ViewTypeNames.GetName(v));
            viewType.Options = list;

            this.viewType.Chosen = ViewTypeNames.GetName(Config.Instance.DefaultViewType);

            //set our dynamic choice options
            this.sortDict = folder.SortOrderOptions;
            this.sortOrders.Options = sortDict.Keys.ToArray();
            this.indexDict = folder.IndexByOptions;
            this.indexBy.Options = folder.IndexByOptions.Keys.ToArray();
 
            showLabels = new BooleanChoice();
            showLabels.Value = Config.Instance.DefaultShowLabels;

            verticalScroll = new BooleanChoice();
            verticalScroll.Value = Config.Instance.DefaultVerticalScroll;

            useBanner = new BooleanChoice();
            useBanner.Value = false;

            useCoverflow = new BooleanChoice();
            useCoverflow.Value = false;

            useBackdrop = new BooleanChoice();
            useBackdrop.Value = Config.Instance.ShowBackdrop;

            sortOrders.ChosenChanged += new EventHandler(sortOrders_ChosenChanged);
            indexBy.ChosenChanged += new EventHandler(indexBy_ChosenChanged);
            viewType.ChosenChanged += new EventHandler(viewType_ChosenChanged);
            showLabels.ChosenChanged += new EventHandler(showLabels_ChosenChanged);
            verticalScroll.ChosenChanged += new EventHandler(verticalScroll_ChosenChanged);
            useBanner.ChosenChanged += new EventHandler(useBanner_ChosenChanged);
            useCoverflow.ChosenChanged += new EventHandler(useCoverflow_ChosenChanged);
            useBackdrop.ChosenChanged += new EventHandler(useBackdrop_ChosenChanged);
            thumbConstraint.PropertyChanged += new PropertyChangedEventHandler(thumbConstraint_PropertyChanged);
        }


        void useCoverflow_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void useBanner_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void indexBy_ChosenChanged(object sender, EventArgs e)
        {
            FirePropertyChanged("IndexBy");
            Save();
        }

        void thumbConstraint_PropertyChanged(IPropertyObject sender, string property)
        {
            Save();
        }

        void showLabels_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void verticalScroll_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }

        void viewType_ChosenChanged(object sender, EventArgs e)
        {
            FirePropertyChanged("ViewTypeString");
            Save();
        }

        void sortOrders_ChosenChanged(object sender, EventArgs e)
        {
            FirePropertyChanged("SortOrder");
            Save();
        }

        void useBackdrop_ChosenChanged(object sender, EventArgs e)
        {
            Save();
        }



        public void WriteToStream(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.SafeWriteString(ViewTypeNames.GetEnum((string)this.viewType.Chosen).ToString());
            bw.Write(this.showLabels.Value);
            bw.Write(this.verticalScroll.Value);
            bw.SafeWriteString((string)this.SortOrder.ToString());
            bw.SafeWriteString((string)this.IndexByString);
            bw.Write(this.useBanner.Value);
            bw.Write(this.thumbConstraint.Value.Width);
            bw.Write(this.thumbConstraint.Value.Height);
            bw.Write(this.useCoverflow.Value);
            bw.Write(this.useBackdrop.Value);
        }

        public DisplayPreferences ReadFromStream(BinaryReader br)
        {
            this.saveEnabled = false;
            byte version = br.ReadByte();
            try
            {
                this.viewType.Chosen = ViewTypeNames.GetName((ViewType)Enum.Parse(typeof(ViewType), br.SafeReadString()));
            }
            catch
            {
                this.viewType.Chosen = ViewTypeNames.GetName(MediaBrowser.Library.ViewType.Poster);
            }
            this.showLabels.Value = br.ReadBoolean();
            this.verticalScroll.Value = br.ReadBoolean();
            try
            {
                this.SortOrder = br.SafeReadString();
            }
            catch { }
            try
            {
                this.IndexBy = br.SafeReadString();
            }
            catch { }
            if (!Config.Instance.RememberIndexing)
                this.IndexBy = Localization.LocalizedStrings.Instance.GetString("NoneDispPref");
            this.useBanner.Value = br.ReadBoolean();
            this.thumbConstraint.Value = new Size(br.ReadInt32(), br.ReadInt32());

            if (version >= 2)
                this.useCoverflow.Value = br.ReadBoolean();

            if (version >= 3)
                this.useBackdrop.Value = br.ReadBoolean();

            this.saveEnabled = true;
            return this;
        }

        public Choice SortOrders
        {
            get { return this.sortOrders; }
        }

        public IComparer<BaseItem> SortFunction
        {
            get
            {
                return sortDict[sortOrders.Chosen.ToString()];
            }
        }

        public string SortOrder
        {
            get { return sortOrders.Chosen.ToString(); }
            set
            {
                this.SortOrders.Chosen = value.ToString();
                this.SortOrders.Default = this.SortOrders.Chosen;
            }
        }

        public string IndexBy
        {
            get { return indexDict[indexBy.Chosen.ToString()]; }
            set
            {
                this.IndexByChoice.Chosen = value.ToString();
                this.IndexByChoice.Default = this.IndexByChoice.Chosen;
            }
        }

        public string IndexByString
        {
            get
            {
                return this.indexBy.Chosen.ToString();
            }
        }

        public Choice IndexByChoice
        {
            get { return this.indexBy; }
        }

        public Choice ViewType
        {
            get { return this.viewType; }
        }

        public string ViewTypeString
        {
            get
            {
                return ViewTypeNames.GetEnum((string)this.viewType.Chosen).ToString();
            }
        }

        public BooleanChoice ShowLabels
        {
            get { return this.showLabels; }
        }

        public BooleanChoice VerticalScroll
        {
            get { return this.verticalScroll; }
        }

        public BooleanChoice UseBanner
        {
            get { return this.useBanner; }
        }

        public BooleanChoice UseCoverflow
        {
            get { return this.useCoverflow; }
        }

        public SizeRef ThumbConstraint
        {
            get
            {
                return this.thumbConstraint;
            }
        }

        public void IncreaseThumbSize()
        {
            Size s = this.ThumbConstraint.Value;
            s.Height += 20;
            s.Width += 20;
            this.ThumbConstraint.Value = s;
        }

        public void DecreaseThumbSize()
        {
            Size s = this.ThumbConstraint.Value;
            s.Height -= 20;
            s.Width -= 20;
            if (s.Height < 60)
                s.Height = 60;
            if (s.Width < 60)
                s.Width = 60;
            this.ThumbConstraint.Value = s;
        }

        public BooleanChoice UseBackdrop
        {
            get { return this.useBackdrop; }
        }

        internal void LoadDefaults()
        {

        }

        private void Save()
        {
            if ((!saveEnabled) || (this.Id == Guid.Empty))
                return;
            Kernel.Instance.ItemRepository.SaveDisplayPreferences(this);
        }

        public void ToggleViewTypes()
        {
            this.ViewType.NextValue(true);
            Save();
            FirePropertyChanged("DisplayPrefs");
        }
    }

    public enum ViewType
    {
        CoverFlow,
        Detail,
        Poster,
        Thumb,
        ThumbStrip
    }

    public class ViewTypeNames
    {
        //private static readonly string[] Names = { "Cover Flow","Detail", "Poster", "Thumb", "Thumb Strip"};
        private static readonly string[] Names = { Kernel.Instance.StringData.GetString("CoverFlowDispPref"), 
                                                   Kernel.Instance.StringData.GetString("DetailDispPref"), 
                                                   Kernel.Instance.StringData.GetString("PosterDispPref"), 
                                                   Kernel.Instance.StringData.GetString("ThumbDispPref"), 
                                                   Kernel.Instance.StringData.GetString("ThumbStripDispPref") };

        public static string GetName(ViewType type)
        {
            return Names[(int)type];
        }

        public static ViewType GetEnum(string name)
        {
            return (ViewType)Array.IndexOf<string>(Names, name);
        }
    }

    public class SortOrderNames
    {
        private static readonly string[] Names = { Kernel.Instance.StringData.GetString("NameDispPref"), 
                                                   Kernel.Instance.StringData.GetString("DateDispPref"), 
                                                   Kernel.Instance.StringData.GetString("RatingDispPref"), 
                                                   Kernel.Instance.StringData.GetString("RuntimeDispPref"), 
                                                   Kernel.Instance.StringData.GetString("UnWatchedDispPref"), 
                                                   Kernel.Instance.StringData.GetString("YearDispPref") };

        public static string GetName(SortOrder order)
        {
            return Names[(int)order];
        }

        public static SortOrder GetEnum(string name)
        {
            return (SortOrder)Array.IndexOf<string>(Names, name);
        }
    }

    public class IndexTypeNames
    {
        private static readonly string[] Names = { Kernel.Instance.StringData.GetString("NoneDispPref"), 
                                                   Kernel.Instance.StringData.GetString("ActorDispPref"), 
                                                   Kernel.Instance.StringData.GetString("GenreDispPref"), 
                                                   Kernel.Instance.StringData.GetString("DirectorDispPref"),
                                                   Kernel.Instance.StringData.GetString("YearDispPref"), 
                                                   Kernel.Instance.StringData.GetString("StudioDispPref") };

        public static string GetName(IndexType order)
        {
            return Names[(int)order];
        }

        public static IndexType GetEnum(string name)
        {
            return (IndexType)Array.IndexOf<string>(Names, name);
        }
    }
}
