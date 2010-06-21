using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.Collections;

namespace Diamond
{
    public class AreaOfInterestHelper
    {
        private PrivateObjectReflector areaOfInterestWrapper;

        public void SetFocused(object Panel)
        {
            Type panelType = Panel.GetType();
            areaOfInterestWrapper = new PrivateObjectReflector("Microsoft.MediaCenter.UI.AreaOfInterestLayoutInput, Microsoft.MediaCenter.UI, Version=6.0.6000.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", new object[] { "Focus" });
            MethodInfo theMethod = panelType.GetMethod("SetLayoutInput", new Type[] { areaOfInterestWrapper.Instance.GetType() });
            theMethod.Invoke(Panel, new object[] { areaOfInterestWrapper.Instance });
        }

        public void SetSelected(object Panel)
        {
            Type panelType = Panel.GetType();
            areaOfInterestWrapper = new PrivateObjectReflector("Microsoft.MediaCenter.UI.AreaOfInterestLayoutInput, Microsoft.MediaCenter.UI, Version=6.0.6000.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", new object[] { "Selected" });
            MethodInfo theMethod = panelType.GetMethod("SetLayoutInput", new Type[] { areaOfInterestWrapper.Instance.GetType() });
            theMethod.Invoke(Panel, new object[] { areaOfInterestWrapper.Instance });
        }

        public void RemoveLayoutInput(object Panel)
        {
            object layoutData = areaOfInterestWrapper.Instance.GetType().GetProperty("Microsoft.MediaCenter.UI.ILayoutData.Data", BindingFlags.FlattenHierarchy |
                BindingFlags.IgnoreCase |
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public | BindingFlags.Static).GetValue(areaOfInterestWrapper.Instance, null);
            Type panelType = Panel.GetType();
            MethodInfo theMethod = panelType.GetMethod("SetLayoutInput", new Type[] { layoutData.GetType(), areaOfInterestWrapper.Instance.GetType() });
            theMethod.Invoke(Panel, new object[] { layoutData, null });
        }
    }
}
