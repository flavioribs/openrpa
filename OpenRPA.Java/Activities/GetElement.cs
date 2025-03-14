﻿using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Java
{
    [System.ComponentModel.Designer(typeof(GetElementDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    [System.Drawing.ToolboxBitmap(typeof(GetElement), "Resources.toolbox.getjavaelement.png")]
    [System.Windows.Markup.ContentProperty("Body")]
    [LocalizedToolboxTooltip("activity_getelement_tooltip", typeof(Resources.strings))]
    [LocalizedDisplayName("activity_getelement", typeof(Resources.strings))]
    [LocalizedHelpURL("activity_getelement_helpurl", typeof(Resources.strings))]
    public class GetElement : BreakableLoop, System.Activities.Presentation.IActivityTemplateFactory
    {
        public GetElement()
        {
            MaxResults = 1;
            MinResults = 1;
        }
        [System.ComponentModel.Browsable(false)]
        public ActivityAction<JavaElement> Body { get; set; }
        public InArgument<int> MaxResults { get; set; }
        public InArgument<int> MinResults { get; set; }
        [RequiredArgument]
        public InArgument<string> Selector { get; set; }
        public InArgument<JavaElement> From { get; set; }
        public InArgument<TimeSpan> Timeout { get; set; }
        public OutArgument<JavaElement[]> Elements { get; set; }
        [Browsable(false)]
        public string Image { get; set; }
        private Variable<IEnumerator<JavaElement>> _elements = new Variable<IEnumerator<JavaElement>>("_elements");
        public Activity LoopAction { get; set; }
        protected override void StartLoop(NativeActivityContext context)
        {
            var SelectorString = Selector.Get(context);
            SelectorString = OpenRPA.Interfaces.Selector.Selector.ReplaceVariables(SelectorString, context.DataContext);
            var sel = new JavaSelector(SelectorString);
            var timeout = Timeout.Get(context);
            if (Timeout == null || Timeout.Expression == null) timeout = TimeSpan.FromSeconds(3);
            var maxresults = MaxResults.Get(context);
            var minresults = MinResults.Get(context);
            var from = From.Get(context);
            if (maxresults < 1) maxresults = 1;
            if (timeout.Minutes > 5 || timeout.Hours > 1)
            {
                Activity _Activity = null;
                try
                {
                    var strProperty = context.GetType().GetProperty("Activity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var strGetter = strProperty.GetGetMethod(nonPublic: true);
                    _Activity = (Activity)strGetter.Invoke(context, null);
                }
                catch (Exception)
                {
                }
                if (_Activity != null)
                {
                    Log.Warning("Timeout for Activity " + _Activity.Id + " is above 5 minutes, was this the intention ? calculated value " + timeout.ToString());
                }
                else
                {
                    Log.Warning("Timeout for on of your Java.GetElements is above 5 minutes, was this the intention ? calculated value " + timeout.ToString());
                }
            }

            JavaElement[] elements = { };
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var selector = new JavaSelector(SelectorString);
                elements = JavaSelector.GetElementsWithuiSelector(selector, from, maxresults);

            } while (elements.Count() == 0 && sw.Elapsed < timeout);
            Log.Debug(string.Format("OpenRPA.Java::GetElement::found {1} elements in {0:mm\\:ss\\.fff}", sw.Elapsed, elements.Count()));
            if (elements.Count() > maxresults) elements = elements.Take(maxresults).ToArray();
            if (elements.Count() < minresults)
            {
                Log.Selector(string.Format("Windows.GetElement::Failed locating " + minresults + " item(s) {0:mm\\:ss\\.fff}", sw.Elapsed));
                throw new ElementNotFoundException("Failed locating " + minresults + " item(s)");
            }
            context.SetValue(Elements, elements);
            IEnumerator<JavaElement> _enum = elements.ToList().GetEnumerator();
            context.SetValue(_elements, _enum);
            bool more = _enum.MoveNext();
            if (more)
            {
                IncIndex(context);
                SetTotal(context, elements.Length);
                context.ScheduleAction(Body, _enum.Current, OnBodyComplete);
            }
        }
        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            IEnumerator<JavaElement> _enum = _elements.Get(context);
            bool more = _enum.MoveNext();
            if (more && !breakRequested)
            {
                IncIndex(context);
                context.ScheduleAction<JavaElement>(Body, _enum.Current, OnBodyComplete);
            }
            else
            {
                if (LoopAction != null && !breakRequested)
                {
                    context.ScheduleActivity(LoopAction, LoopActionComplete);
                }
            }
        }
        private void LoopActionComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (!breakRequested) StartLoop(context);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            Interfaces.Extensions.AddCacheArgument(metadata, "Selector", Selector);
            Interfaces.Extensions.AddCacheArgument(metadata, "From", From);
            Interfaces.Extensions.AddCacheArgument(metadata, "Elements", Elements);
            Interfaces.Extensions.AddCacheArgument(metadata, "MaxResults", MaxResults);
            metadata.AddImplementationVariable(_elements);
            base.CacheMetadata(metadata);
        }
        public Activity Create(System.Windows.DependencyObject target)
        {
            var fef = new GetElement();
            fef.Variables.Add(new Variable<int>("Index", 0));
            fef.Variables.Add(new Variable<int>("Total", 0));
            var aa = new ActivityAction<JavaElement>();
            var da = new DelegateInArgument<JavaElement>();
            da.Name = "item";
            fef.Body = aa;
            aa.Argument = da;
            return fef;
        }
        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    var displayNameAttribute = this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAttribute != null) displayName = displayNameAttribute.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}