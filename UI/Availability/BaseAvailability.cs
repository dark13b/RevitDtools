using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitDtools.UI.Availability
{
    /// <summary>
    /// Base class for context-sensitive button availability
    /// </summary>
    public abstract class BaseAvailability : IExternalCommandAvailability
    {
        protected abstract string[] AllowedViewTypes { get; }
        protected virtual bool RequireActiveDocument => true;
        protected virtual bool RequireSelection => false;

        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            try
            {
                // Check if we have an active document
                if (RequireActiveDocument && applicationData.ActiveUIDocument?.Document == null)
                {
                    return false;
                }

                var document = applicationData.ActiveUIDocument?.Document;
                if (document == null && RequireActiveDocument)
                {
                    return false;
                }

                // Check view type restrictions
                if (AllowedViewTypes?.Length > 0)
                {
                    var activeView = applicationData.ActiveUIDocument?.ActiveView;
                    if (activeView == null)
                    {
                        return false;
                    }

                    var viewTypeName = activeView.GetType().Name;
                    if (!AllowedViewTypes.Contains(viewTypeName))
                    {
                        return false;
                    }
                }

                // Check selection requirements
                if (RequireSelection)
                {
                    var selection = applicationData.ActiveUIDocument?.Selection;
                    if (selection == null || !selection.GetElementIds().Any())
                    {
                        return false;
                    }
                }

                // Additional custom checks
                return IsCommandAvailableCustom(applicationData, selectedCategories);
            }
            catch (Exception)
            {
                // If any error occurs, disable the command for safety
                return false;
            }
        }

        /// <summary>
        /// Override this method for custom availability logic
        /// </summary>
        protected virtual bool IsCommandAvailableCustom(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}