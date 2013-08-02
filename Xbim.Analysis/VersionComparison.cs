using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Analysis.Comparitors;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Analysis
{
    public delegate void MessageCallback(string message);
    public class VersionComparison
    {
        private XbimModel Baseline { get; set; }
        private XbimModel Revision { get; set; }

        public event MessageCallback OnMessage;
        private void Message(String message)
        {
            if (OnMessage != null) OnMessage(message);
        }

        public Dictionary<IfcProduct, ChangeType> EntityLabelChanges = new Dictionary<IfcProduct, ChangeType>();
        public Dictionary<Int32, Int32> EntityMapping = new Dictionary<Int32, Int32>();
        public List<IfcProduct> Deleted = new List<IfcProduct>();
        public List<IfcProduct> Added = new List<IfcProduct>();

        public Int32 StartComparison(XbimModel baseline, XbimModel revision)
        {
            Baseline = baseline;
            Revision = revision;

            WorkingCopyBaseline = new List<IfcProduct>(Baseline.Instances.OfType<IfcProduct>());
            WorkingCopyDelta = new List<IfcProduct>(Revision.Instances.OfType<IfcProduct>());

            CheckGuids();
            if (WorkingCopyDelta.Count > 0 && WorkingCopyBaseline.Count > 0)
                CheckNames();
            if (WorkingCopyDelta.Count > 0 && WorkingCopyBaseline.Count > 0)
                CheckRelationships();
            if (WorkingCopyDelta.Count > 0 && WorkingCopyBaseline.Count > 0)
                CheckProperties();
            if (WorkingCopyDelta.Count > 0 && WorkingCopyBaseline.Count > 0)
                CheckGeometry();


            Deleted.AddRange(WorkingCopyBaseline);
            Added.AddRange(WorkingCopyDelta);

            Message(String.Format("All Checks Complete. {0} items unresolved", WorkingCopyBaseline.Count+WorkingCopyDelta.Count));
            if (WorkingCopyBaseline.Count > 0)
            {
                Message("Cannot resolve Baseline item(s):");
                foreach (var i in WorkingCopyBaseline)
                    Message(String.Format("Cannot resolve Missing GUID: {0}", i.GlobalId));
            }
            if (WorkingCopyDelta.Count > 0)
            {
                Message("Cannot resolve Delta item(s):");
                foreach (var i in WorkingCopyDelta)
                {
                    Message(String.Format("Cannot resolve Added GUID: {0}", i.GlobalId));
                }
            }
            
            Message("Map from Entity Labels is as follows (Baseline -> Delta)");
            foreach (var key in EntityMapping)
            {
                Message(String.Format("{0} -> {1}", key.Key, EntityMapping[key.Key]));
            }

            return WorkingCopyBaseline.Count + WorkingCopyDelta.Count;
        }

        private void CheckNames()
        {
            Message("Starting Name Check");
            NameComparison n = new NameComparison();

            var results = n.Compare(WorkingCopyBaseline, WorkingCopyDelta);

            //update working copies as we go with those still yet to resolve
            WorkingCopyBaseline.Clear(); WorkingCopyDelta.Clear();

            foreach (var item in results.Where(x => x.Value == ChangeType.Matched))
            {
                Message(String.Format("Found a Match for type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Added))
            {
                WorkingCopyDelta.Add(item.Key);
                Message(String.Format("Found a new item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Deleted))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found a missing item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Unknown))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found duplicate possibilities for item of type {1} with Name: {0}", item.Key.Name, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }

            var m = n.GetMap();
            foreach (var key in m)
            {
                EntityMapping[key.Key] = m[key.Key];
            }

            Message("Name Check - Complete");
        }
        private void CheckGuids()
        {
            Message("Starting Guid Check");
            GuidComparison g = new GuidComparison();

            var results = g.Compare(WorkingCopyBaseline, WorkingCopyDelta);

            //update working copies as we go with those still yet to resolve
            WorkingCopyBaseline.Clear(); WorkingCopyDelta.Clear();

            foreach (var item in results.Where(x => x.Value == ChangeType.Matched))
            {
                Message(String.Format("Found a Match for type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Added))
            {
                WorkingCopyDelta.Add(item.Key);
                Message(String.Format("Found a new item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Deleted))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found a missing item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }
            foreach (var item in results.Where(x => x.Value == ChangeType.Unknown))
            {
                WorkingCopyBaseline.Add(item.Key);
                Message(String.Format("Found duplicate possibilities for item of type {1} with GUID: {0}", item.Key.GlobalId, item.Key.GetType().ToString()));
                EntityLabelChanges[item.Key] = item.Value;
            }

            var m = g.GetMap();
            foreach (var key in m)
            {
                EntityMapping[key.Key] = m[key.Key];
            }
            Message("Guid Check - Complete");
        }
        private List<IfcProduct> WorkingCopyBaseline = new List<IfcProduct>();
        private List<IfcProduct> WorkingCopyDelta = new List<IfcProduct>();
       
        private void CheckGeometry()
        {
            Message("Starting - Geometry Check");
            Message("Check Not Implemented Yet");
            Message("Geometry Check - complete");
        }
        private void CheckRelationships()
        {
            Message("Starting - Relationship Check");
            Message("Check Not Implemented Yet");
            Message("Relationship Check - complete");
        }
        private void CheckProperties()
        {
            Message("Starting - Property Check");
            Message("Check Not Implemented Yet");
            Message("Property Check - complete");
        }
    }
}
