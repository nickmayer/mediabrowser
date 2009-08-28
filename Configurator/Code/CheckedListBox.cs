using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Collections;
using System.Windows.Media;

namespace Configurator.Code
{
    public class CheckedListBox : ListBox
    {
        private const double CHECKBOX_WIDTH = 16.0;
        private Brush SelectedCheckboxColor = (Brush)(new BrushConverter()).ConvertFromString("Gray");        

        public CheckedListBox()
        {
            base.SelectionChanged += new SelectionChangedEventHandler(CheckedListBox_SelectionChanged);
            base.PreviewMouseUp += new System.Windows.Input.MouseButtonEventHandler(CheckedListBox_PreviewMouseUp);
        }
        
        public void Sort()
        {
            try
            {
                ArrayList SortedContents = new ArrayList();
                ArrayList OrignalCheckBoxes = new ArrayList();
                
                foreach (var item in base.Items)
                {
                    OrignalCheckBoxes.Add(item);
                    SortedContents.Add(((CheckBox)item).Content);
                }

                SortedContents.Sort();

                base.Items.Clear();

                foreach (var item in SortedContents)
                {
                    foreach (CheckBox cb in OrignalCheckBoxes)
                    {
                        if (cb.Content == item)
                        {
                            base.Items.Add(cb);
                            break;
                        }
                    }                    
                }
                
            }
            catch (Exception ex)
            {
                
            }
        }       

        public void Add(Object item)
        {
            CheckBox cb = new CheckBox();                      
            cb.Content = item;            
            cb.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(cb_PreviewMouseDown);            
            base.Items.Add(cb);
        }       

        public void CheckItem(Object Item)
        {
            int i = 0;
            foreach (var cb in this.Items)
            {
                try
                {
                    if (((MCEntryPointItem)cb).EntryPointUID == ((MCEntryPointItem)Item).EntryPointUID)
                    {
                        CheckItem(i);
                        break;
                    }
                    i++;
                }
                catch (Exception ex)
                { }
            }
        }

        public void CheckItem(int Index)
        {
            if (Index < base.Items.Count && Index >= 0)
            {
                ((CheckBox)base.Items[Index]).IsChecked = true;                
            }
        }

        public void UnCheckItem(Object Item)
        {
            int i = 0;
            foreach (var cb in this.Items)
            {
                try
                {
                    if (cb == Item)
                    {
                        UnCheckItem(i);
                        break;
                    }
                    i++;
                }
                catch (Exception ex)
                { }
            }
        }

        public void UnCheckItem(int Index)
        {
            if (Index < base.Items.Count && Index > 0)
            {
                ((CheckBox)base.Items[Index]).IsChecked = false;
            }
        }

        public bool isChecked(Object item)
        {
            foreach (var cb in base.Items)
            {
                try
                {
                    if (((CheckBox)cb).Content == item)
                    {
                        return (bool)((CheckBox)cb).IsChecked;                       
                    }                    
                }
                catch (Exception ex)
                {
                    return false;
                }                
            }
            return false;
        }

        public void Refresh()
        {
            this.UpdateBackgroundAndForegroundColors();
        }

        private void UpdateBackgroundAndForegroundColors()
        {
            this.UpdateBackgroundAndForegroundColors((CheckBox)((CheckedListBox)this).SelectedItem);
        }

        private void UpdateBackgroundAndForegroundColors(CheckBox cb)
        {
            int i = -1;
            foreach (CheckBox item in base.Items)
            {
                i++;
                //if (this.SelectedIndex == i)
                if(item == cb)
                {
                    //CheckBox cb = (CheckBox)((CheckedListBox)this).SelectedItem;
                    cb.Foreground = this.Background;
                    cb.Background = this.SelectedCheckboxColor;                    
                    continue;
                }
                item.Foreground = this.Foreground;
                item.Background = this.Background;
            }            
        }
        
        public void Clear()
        {
            base.Items.Clear();
        }

        public new ItemCollection Items
        {
            get
            {                
                ItemCollection ic = (new ListBox()).Items;                
                ic.Clear();

                foreach (var item in base.Items)
                {
                    ic.Add(((CheckBox)item).Content);
                }

                return (ItemCollection)ic;
            }
        }

        public new object SelectedValue
        {
            get
            {
                return ((CheckBox)base.SelectedValue).Content;
            }
            set
            {
                int i = 0;
                foreach (var item in this.Items)
                {
                    if (item.Equals(value))
                    {
                        base.SelectedIndex = i;
                        break;
                    }
                    i++;
                }
            }
        }        

        void CheckedListBox_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                CheckBox cb = (CheckBox)((CheckedListBox)this).SelectedItem;
                cb.IsChecked = !cb.IsChecked;
                this.UpdateBackgroundAndForegroundColors();
                cb.IsChecked = !cb.IsChecked;
            }
            catch (Exception ex)
            {

            }
        }

        void CheckedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IList RemovedItems = e.RemovedItems;
            IList AddedItems = e.AddedItems;

            for (int i = 0; i < AddedItems.Count; i++)
            {
                if (AddedItems[i].GetType() == typeof(CheckBox))
                {
                    AddedItems[i] = ((CheckBox)AddedItems[i]).Content;
                }
            }
            for (int i = 0; i < RemovedItems.Count; i++)
            {
                if (RemovedItems[i].GetType() == typeof(CheckBox))
                {
                    RemovedItems[i] = ((CheckBox)RemovedItems[i]).Content;
                }
            }


            try
            {
                SelectionChangedEventArgs new_e = new SelectionChangedEventArgs(e.RoutedEvent, RemovedItems, AddedItems);
                this.SelectionChanged(sender, new_e);
            }
            catch (Exception ex)
            { }


        }

        void cb_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UpdateBackgroundAndForegroundColors((CheckBox)sender);
            Point p = e.GetPosition(this);
            double X = p.X;
            CheckBox cb = (CheckBox)sender;

            if (X > CHECKBOX_WIDTH)
            {
                cb.IsChecked = !cb.IsChecked;
            }

            int itemindex = -1;
            int i = 0;
            foreach (CheckBox c in base.Items)
            {
                if (c == cb)
                {
                    itemindex = i;
                    break;
                }
                i++;
            }

            if (itemindex >= 0)
            {
                base.SelectedIndex = itemindex;
            }

            if (X <= CHECKBOX_WIDTH)
            {
                cb.IsChecked = !cb.IsChecked;
                CheckBoxCheckedChanged(((CheckBox)sender).Content, e);
                cb.IsChecked = !cb.IsChecked;
                //this.UpdateBackgroundAndForegroundColors();                
            }
        }

        public delegate void CheckBoxChangedHandler(object sender, System.Windows.RoutedEventArgs e);
        
        public event CheckBoxChangedHandler CheckBoxCheckedChanged;        

        public new event SelectionChangedEventHandler SelectionChanged;

    }
}
